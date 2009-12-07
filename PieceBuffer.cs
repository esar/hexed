using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;


public partial class PieceBuffer : IDisposable
{
	public class BufferChangedEventArgs : EventArgs
	{
		public long StartOffset;
		public long EndOffset;

		public BufferChangedEventArgs(long start, long end)
		{
			StartOffset = start;
			EndOffset = end;
		}
	}
	
	public class HistoryEventArgs : EventArgs
	{
		public HistoryItem OldItem;
		public HistoryItem NewItem;
		
		public HistoryEventArgs(HistoryItem oldItem, HistoryItem newItem)
		{
			OldItem = oldItem;
			NewItem = newItem;
		}
	}
	
	public class SavePlan
	{
		protected long _TotalLength;
		public long TotalLength
		{
			get { return _TotalLength; }
		}

		protected long _WriteLength;
		public long WriteLength
		{
			get { return _WriteLength; }
		}

		protected long _BlockCount;
		public long BlockCount
		{
			get { return _BlockCount; }
		}

		protected bool _IsInPlace;
		public bool IsInPlace
		{
			get { return _IsInPlace; }
		}
	}

	protected class InternalSavePlan : SavePlan
	{
		public Piece Pieces;
		public List< KeyValuePair<long, Piece> > InPlacePieces;
		public new long TotalLength
		{
			get { return _TotalLength; }
			set { _TotalLength = value; }
		}
		public new long WriteLength
		{
			get { return _WriteLength; }
			set { _WriteLength = value; }
		}
		public new long BlockCount
		{
			get { return _BlockCount; }
			set { _BlockCount = value; }
		}
		public new bool IsInPlace
		{
			get { return _IsInPlace; }
			set { _IsInPlace = value; }
		}

		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.Append("SavePlan:\n");
			sb.Append("    InPlace: " + IsInPlace + "\n");
			sb.Append("    TotalLength: " + TotalLength + "\n");
			sb.Append("    WriteLength: " + WriteLength + "\n");
			sb.Append("    Blocks: " + BlockCount + "\n");
			if(IsInPlace)
			{
				foreach(KeyValuePair<long, Piece> kvp in InPlacePieces)
					sb.Append("        " + kvp.Value.Length + "@" + kvp.Key + "\n");
			}
			else
			{
				long offset = 0;
				Piece p = Pieces;
				while((p = p.Next) != Pieces)
				{
					sb.Append("        " + p.Length + "@" + offset + "\n");
					offset += p.Length;
				}
			}

			return sb.ToString();
		}

		public void Execute(FileStream stream)
		{
			if(IsInPlace)
			{
				foreach(KeyValuePair<long, Piece> kvp in InPlacePieces)
				{
					stream.Seek(kvp.Key, SeekOrigin.Begin);
					kvp.Value.Write(stream, 0, kvp.Value.Length);
				}
			}
			else
			{
				Piece p = Pieces;
				while((p = p.Next) != Pieces)
					p.Write(stream, 0, p.Length);
			}
		}
	}
	
	public class ClipboardRange
	{
		protected long _Length;
		public long Length
		{
			get { return _Length; }
		}
		
		protected ClipboardRange() {}
	}
	
	protected class InternalClipboardRange : ClipboardRange
	{
		public Piece StartPiece;
		public long StartOffset;
		public Piece EndPiece;
		public long EndOffset;
		public new long Length
		{
			get { return _Length; }
			set { _Length = value; }
		}
	}
	
	protected class TransformOperationDataSource : IBlock
	{
		protected Piece StartPiece;
		protected long StartOffset;
		protected Piece EndPiece;
		protected long EndOffset;
		protected long _Length;
		
		public byte this[long index]
		{
			get { return 0; }
			set {}
		}
		
		public long Length { get { return _Length; } }
		public long Used { get { return 0; } set {} }

		public bool CanSaveInPlace
		{
			get 
			{
				Piece p = StartPiece;
				while(true)
				{
					if(!p.CanSaveInPlace)
						return false;
					if(p == EndPiece)
						break;
					p = p.Next;
				}

				return true;
			}
		}		
		
		public TransformOperationDataSource(Piece startPiece, long startOffset, Piece endPiece, long endOffset, long length)
		{
			StartPiece = startPiece;
			StartOffset = startOffset;
			EndPiece = endPiece;
			EndOffset = endOffset;
			_Length = length;
		}
		
		public  bool GetOffsetsRelativeToBlock(IBlock block, out long start, out long end)
		{
			if(block == this)
			{
				start = 0;
				end = _Length;
			}
			
			long s, e;
			if(StartPiece.GetOffsetsRelativeToBlock(block, out s, out e))
			{
				start = s + StartOffset;
				if(EndPiece.GetOffsetsRelativeToBlock(block, out s, out e))
				{
					end = e + EndOffset;
					return true;
				}
			}

			start = StartOffset;
			end = EndOffset;
			return false; // Either start or end piece (or both) isn't related to block
		}

		public void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			Piece p = StartPiece;
			
			if(length > _Length)
				throw new ArgumentOutOfRangeException("length", "The requested length is longer than the available data");
			
			start += StartOffset;
			while(start >= p.Length)
			{
				start -= p.Length;
				p = p.Next;
			}
			
			while(length > 0)
			{
				long len = length > p.Length ? p.Length : length;
				p.GetBytes(start, len, dst, dstOffset);
				length -= len;
				dstOffset += len;
				p = p.Next;
			}
		}
		
		public void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
		}
		
		public virtual void Write(FileStream stream, long start, long length)
		{
			byte[] data = new byte[4096];

			while(length > 0)
			{
				int len = length > 4096 ? 4096 : (int)length;
				GetBytes(start, len, data, 0);
				stream.Write(data, 0, len);
				start += len;
				length -= len;
			}
		}
	}
	
	public interface ITransformOperation
	{
		bool CanSaveInPlace { get; }
		void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset);
	}
	
	public class TransformOperationOr : ITransformOperation
	{
		byte[] Constant;
		
		public TransformOperationOr(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
		}
		
		public bool CanSaveInPlace
		{
			get { return true; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] |= Constant[(start++) % Constant.Length];
		}
	}
	
	public class TransformOperationAnd : ITransformOperation
	{
		byte[] Constant;
		
		public TransformOperationAnd(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
		}
		
		public bool CanSaveInPlace
		{
			get { return true; }
		}
		
		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] &= Constant[(start++) % Constant.Length];
		}
	}
	
	public class TransformOperationXor : ITransformOperation
	{
		byte[] Constant;
		
		public TransformOperationXor(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
		}
		
		public bool CanSaveInPlace
		{
			get { return true; }
		}
		
		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] ^= Constant[(start++) % Constant.Length];
		}
	}

	public class TransformOperationInvert : ITransformOperation
	{
		public bool CanSaveInPlace
		{
			get { return true; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] ^= 0xFF;
		}
	}

	public class TransformOperationReverse : ITransformOperation
	{
		public bool CanSaveInPlace
		{
			get { return false; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(source.Length - start - length, length, dest, destOffset);
			Array.Reverse(dest, (int)destOffset, (int)length);
		}
	}
	
	public class TransformOperationShift : ITransformOperation
	{
		protected int Distance;
		
		public TransformOperationShift(int distance)
		{
			Distance = distance;
		}
		
		public bool CanSaveInPlace
		{
			get { return Distance < 0; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			int distance = Distance / 8;
			
			// Read into buffer, shifting by whole bytes
			start -= distance;
			if(distance > 0)
			{
				int off = (int)destOffset;
				int len = (int)length;
				
				if(start < 0)
				{
					Array.Clear(dest, off, (int)(0 - start));
					off -= (int)start;
					len += (int)start;
					start = 0;
				}
				
				source.GetBytes(start, len, dest, off);
			}
			else if(distance < 0)
			{
				int len = (int)length;
				if(start + len > source.Length)
					len = (int)(source.Length - start);
				
				source.GetBytes(start, len, dest, destOffset);
				int off = (int)destOffset + len;
				len = (int)length - len;
				
				if(len > 0)
					Array.Clear(dest, (int)off, (int)len);
			}
			
			// Adjust buffer, shifting by partial bytes
			distance = Distance % 8;
			if(distance < 0)
			{
				Console.WriteLine("distance: " + distance);
				Console.WriteLine("dstOff: {0}, len: {1}", destOffset, length);
				distance = 0 - distance;
				for(int i = (int)destOffset; i < (int)(destOffset + length) - 1; ++i)
				{
					Console.WriteLine("Shifting: " + i + ", by: " + distance);
					dest[i] = (byte)((dest[i] << distance) | (dest[i + 1] >> (8 - distance)));
				}
				dest[destOffset + length - 1] <<= distance;
			}
			else if(distance > 0)
			{
				dest[destOffset] >>= distance;
				for(int i = (int)destOffset + 1; i < (int)(destOffset + length); ++i)
					dest[i] = (byte)((dest[i - 1] << (8 - distance)) | (dest[i] >> distance));
			}
		}
	}

	protected class TransformPiece : Piece
	{
		protected ITransformOperation Op;
		
		public override bool CanSaveInPlace
		{
			get { return Block.CanSaveInPlace && Op.CanSaveInPlace; }
		}

		public override byte this[long index]
		{
			get { return 0; }
			set { throw new Exception("Can't set data in TransformPiece"); }
		}
		
		public TransformPiece(ITransformOperation op, IBlock source) : base(source, 0, source.Length) 
		{
			Op = op;
		}
		
		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't set data in TransformPiece");
		}
		
		public override void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			Op.GetTransformedBytes(Block, start, length, dst, dstOffset);
		}
		
		public override void Write(FileStream stream, long start, long length)
		{
			byte[] data = new byte[4096];

			while(length > 0)
			{
				int len = length > 4096 ? 4096 : (int)length;
				Op.GetTransformedBytes(Block, start, len, data, 0);
				stream.Write(data, 0, len);
				start += len;
				length -= len;
			}
		}
	}

	protected class Piece : IBlock
	{
		public readonly IBlock Block;
		public readonly long Start;
		public readonly long End;

		public Piece Next;
		public Piece Prev;

		public virtual byte this[long index]
		{
			get { return Block[index]; }
			set { throw new Exception("Can't set data in Piece"); }
		}
		
		public long Length { get { return End - Start; } }
		public long Used
		{
			get { return End - Start; }
			set { throw new Exception("Can't set Used on Piece"); }
		}

		public virtual bool CanSaveInPlace
		{
			get { return Block.CanSaveInPlace; }
		}

		public Piece()
		{
			Next = this;
			Prev = this;
			Block = null;
			Start = Int64.MaxValue;
			End = Int64.MaxValue;
		}

		public Piece(IBlock block, long start, long end)
		{
			Next = this;
			Prev = this;
			Block = block;
			Start = start;
			End = end;
		}
		
		public virtual bool GetOffsetsRelativeToBlock(IBlock block, out long start, out long end)
		{
			if(block == this)
			{
				start = 0;
				end = End - Start;
			}
			
			long s, e;
			if(Block.GetOffsetsRelativeToBlock(block, out s, out e))
			{
				start = s + Start;
				end = start + (End - Start);
				return true;
			}

			start = Start;
			end = End;
			return false; // isn't related to block
		}

		public virtual void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't set data in Piece");
		}

		public virtual void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			Block.GetBytes(Start + start, length, dst, dstOffset);
		}
		
		public virtual void Write(FileStream stream, long start, long length)
		{
			Block.Write(stream, Start, length);
		}

		public static void ListInsert(Piece list, Piece item)
		{
			item.Next = list.Next;
			item.Prev = list;
			list.Next.Prev = item;
			list.Next = item;
		}

		public static void ListInsertRange(Piece list, Piece first, Piece last)
		{
			last.Next = list.Next;
			first.Prev = list;
			last.Next.Prev = last;
			first.Prev.Next = first;
		}

		public static void ListRemove(Piece item)
		{
			item.Prev.Next = item.Next;
			item.Next.Prev = item.Prev;
		}

		public static void ListRemoveRange(Piece first, Piece last)
		{
			first.Prev.Next = last.Next;
			last.Next.Prev = first.Prev;
		}

		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("Piece{");
			sb.Append(Start.ToString());
			sb.Append(",");
			sb.Append(End.ToString());
			if(Block != null)
			{
				byte[] data = new byte[End-Start];
				GetBytes(0, End-Start, data, 0);
				sb.Append(",\"");
				sb.Append(System.Text.ASCIIEncoding.ASCII.GetString(data));
				sb.Append("\"}");
			}
			else
				sb.Append(",(null)}");
			return sb.ToString();
		}
	}
	
	public enum HistoryOperation
	{
		None,
		New,
		Open,
		Insert,
		InsertFile,
		FillConstant,
		Copy,
		Remove,
		Replace,
		And,
		Or,
		Xor,
		Invert,
		ShiftLeft,
		ShiftRight,
		RotateLeft,
		RotateRight,
		Reverse
	}
	
	public class HistoryItem
	{
		protected bool _Active;
		public bool Active { get { return _Active; } }
		
		protected DateTime _Date;
		public DateTime Date { get { return _Date; } }
		
		protected HistoryOperation _Operation;
		public HistoryOperation Operation { get { return _Operation; } }
		
		protected long _StartPosition;
		public long StartPosition { get { return _StartPosition; } }
		
		protected long _Length;
		public long Length { get { return _Length; } }
		
		protected HistoryItem _Parent;
		public HistoryItem Parent { get { return _Parent; } }
		
		protected HistoryItem _FirstChild;
		public HistoryItem FirstChild { get { return _FirstChild; } }
		
		protected HistoryItem _NextSibling;
		public HistoryItem NextSibling { get { return _NextSibling; } }
		
		protected HistoryItem() {}
	}

	protected class InternalHistoryItem : HistoryItem
	{
		public new bool Active 
		{
			get { return _Active; }
			set { _Active = value; } 
		}
		public new DateTime Date 
		{
			get { return _Date; }
			set { _Date = value; } 
		}
		public new HistoryOperation Operation 
		{
			get { return _Operation; }
			set { _Operation = value; } 
		}
		public new long StartPosition 
		{
			get { return _StartPosition; }
			set { _StartPosition = value; } 
		}
		public new long Length 
		{
			get { return _Length; }
			set { _Length = value; } 
		}
		
		public new HistoryItem Parent 
		{
			get { return _Parent; }
			set { _Parent = value; } 
		}
		public new HistoryItem FirstChild 
		{
			get { return _FirstChild; }
			set { _FirstChild = value; } 
		}
		public new HistoryItem NextSibling 
		{
			get { return _NextSibling; }
			set { _NextSibling = value; } 
		}
		
		public InternalHistoryItem InternalParent { get { return (InternalHistoryItem)_Parent; } }
		public InternalHistoryItem InternalFirstChild { get { return (InternalHistoryItem)_FirstChild; } }
		public InternalHistoryItem InternalNextSibling { get { return (InternalHistoryItem)_NextSibling; } }

		protected Piece _Head;
		public Piece Head
		{
			get { return _Head; }
			set { _Head = value; }
		}

		protected Piece _Tail;
		public Piece Tail
		{
			get { return _Tail; }
			set { _Tail = value; }
		}

		protected int _GroupLevel;
		public int GroupLevel
		{
			get { return _GroupLevel; }
			set { _GroupLevel = value; }
		}


		public InternalHistoryItem(DateTime date, HistoryOperation op, long startPosition, long length, 
		                           Piece head, Piece tail, int groupLevel)
		{
			Active = true;
			Date = date;
			Operation = op;
			StartPosition = startPosition;
			Length = length;
			Head = head;
			Tail = tail;
			GroupLevel = groupLevel;
		}
	}




	protected Piece                  Pieces;
	protected InternalMarkCollection _Marks;
	public MarkCollection            Marks { get { return _Marks; } }
	protected Dictionary<string, Block> OpenBlocks; 
	protected FileBlock              OriginalFileBlock;
	protected Block                  CurrentBlock;
	protected InternalHistoryItem    _History;
	public HistoryItem               History { get { return _History; } }
	protected InternalHistoryItem   _HistoryRoot;
	public HistoryItem              HistoryRoot { get { return _HistoryRoot; } }
	public bool                     CanUndo { get { return _History.Parent != null; } }
	public bool                     CanRedo { get { return _History.FirstChild != null; } }
	public bool                     IsModified { get { return _History.Parent != null; } }
	protected int                   HistoryGroupLevel;
	protected InternalSavePlan      CachedSavePlan;

	const int IndexCacheSize = 4096;
	long IndexCacheStartOffset;
	byte[] IndexCacheBytes;

	public delegate void BufferChangedEventHandler(object sender, BufferChangedEventArgs e);
	public event BufferChangedEventHandler Changed;
	
	public delegate void HistoryEventHandler(object sender, HistoryEventArgs e);
	public event HistoryEventHandler HistoryAdded;
	public event HistoryEventHandler HistoryUndone;
	public event HistoryEventHandler HistoryRedone;
	public event HistoryEventHandler HistoryJumped;
	public event EventHandler HistoryCleared;
	
	protected object Lock = new object();

	public byte this[long index]
	{
		get
		{
			lock(Lock)
			{
				if(index < IndexCacheStartOffset ||
				   index >= IndexCacheStartOffset + IndexCacheSize)
				{
					if(index < 0)
						index = 0;
					IndexCacheStartOffset = index;
					GetBytes(index, IndexCacheBytes, IndexCacheSize);
				}
				return IndexCacheBytes[index - IndexCacheStartOffset];
			}
		}

		set
		{
			Mark m1 = Marks.Add(index);
			Mark m2 = Marks.Add(index + 1);
			Insert(m1, m2, new byte[] { value }, 1);
			Marks.Remove(m1);
			Marks.Remove(m2);
		}
	}

	public long Length
	{
		get 
		{ 
			lock(Lock)
			{
				return Marks.End.Position; 
			}
		}
	}
	
	protected string _FileName;
	public string FileName
	{
		get { return _FileName; }
	}
	

	public PieceBuffer()
	{
		New();
	}

	public PieceBuffer(string filename)
	{
		Open(filename);
	}

	public void New()
	{
		if(CurrentBlock != null)
			throw new Exception("already open");

		OpenBlocks  = new Dictionary<string,Block>();

		Pieces = new Piece();

		_Marks = new InternalMarkCollection(this, Pieces, Pieces, 0);

		CurrentBlock = MemoryBlock.Create(OpenBlocks, 4096);
		
		_HistoryRoot = _History = new InternalHistoryItem(DateTime.Now, HistoryOperation.New, 0, 0, null, null, 0);
		HistoryGroupLevel = 0;

		IndexCacheBytes = new byte[IndexCacheSize];
		IndexCacheStartOffset = Int64.MaxValue;

		if(HistoryAdded != null)
			HistoryAdded(this, new HistoryEventArgs(null, _History));
	}

	public void Open(string filename)
	{
		if(CurrentBlock != null)
			throw new Exception("already open");

		OpenBlocks  = new Dictionary<string,Block>();

		_FileName = filename;

		OriginalFileBlock = FileBlock.Create(OpenBlocks, filename);
		Block block = OriginalFileBlock;

		Pieces = new Piece();
		Piece piece = new Piece(block, 0, block.Length);
		Piece.ListInsert(Pieces, piece);

		_Marks = new InternalMarkCollection(this, Pieces, piece, block.Length);

		CurrentBlock = MemoryBlock.Create(OpenBlocks, 4096);

		_HistoryRoot = _History = new InternalHistoryItem(DateTime.Now, HistoryOperation.Open, 0, 
		                                                  block.Length, null, null, 0);
		HistoryGroupLevel = 0;

		IndexCacheBytes = new byte[IndexCacheSize];
		IndexCacheStartOffset = Int64.MaxValue;

		if(HistoryAdded != null)
			HistoryAdded(this, new HistoryEventArgs(null, _History));
	}

	public void Reopen()
	{
		if(OriginalFileBlock == null)
			throw new Exception("can't reopen, not backed by a file");

		string filename = _FileName;
		InternalMarkCollection marks = _Marks;
		Close();
		Open(filename);
		_Marks = marks;
		_Marks.UpdateAfterReopen(Pieces);
	}

	public void Close()
	{
		Pieces = null;
		CurrentBlock = null;
		OriginalFileBlock = null;
		_Marks = null;
		_HistoryRoot = _History = null;
		IndexCacheBytes = null;
		CachedSavePlan = null;

		if(HistoryCleared != null)
			HistoryCleared(this, EventArgs.Empty);

		// Cleanup any open blocks
		if(OpenBlocks != null)
		{
			foreach(KeyValuePair<string,Block> kvp in OpenBlocks)
				kvp.Value.Close();
			OpenBlocks.Clear();
			OpenBlocks = null;
		}
	}

	protected void OnChanged(BufferChangedEventArgs e)
	{
		//Console.WriteLine("Buffer Changed: " + e.StartOffset + " => " + e.EndOffset);

		CachedSavePlan = null;

		if((e.StartOffset >= IndexCacheStartOffset && e.StartOffset < IndexCacheStartOffset + IndexCacheSize) ||
		   (e.EndOffset >= IndexCacheStartOffset && e.EndOffset < IndexCacheStartOffset + IndexCacheSize))
			IndexCacheStartOffset = Int64.MaxValue;
		if(Changed != null)
			Changed(this, e);
	}


	protected void Splice(InternalMark start, InternalMark end, Piece newStart, Piece newEnd, long newLength)
	{
		Piece A;
		Piece C;
		Piece APrev = start.Piece.Prev;
		Piece CNext;
		Piece oldStart;
		Piece oldEnd;
		if(end.Piece == start.Piece || end.Offset > 0)
			CNext = end.Piece.Next;
		else
			CNext = end.Piece;
		long oldLength = end.Position - start.Position;


		// Create new A piece from first half of start piece if start
		// offset isn't zero, otherwise A is piece immediately before
		// the start piece
		//
		//                   start
		//                     |
		// +-------+   +=======+--------+   +---+   +--------+-------+   +-------+
		// | APrev |<->|   A   |  B(0)  |<->|...|<->|  B(n)  |   C   |<->| CNext |
		// +-------+   +=======+--------+   +---+   +--------+-------+   +-------+
		if(start.Offset != 0)
			A = new Piece(start.Piece.Block, start.Piece.Start, start.Piece.Start + start.Offset);
		else
			A = APrev;

		// Create new C piece from second half of end piece if end
		// offset isn't zero, otherwise C is the end piece
		//
		//                                                  end
		//                                                   |
		// +-------+   +-------+--------+   +---+   +--------+=======+   +-------+
		// | APrev |<->|   A   |  B(0)  |<->|...|<->|  B(n)  |   C   |<->| CNext |
		// +-------+   +-------+--------+   +---+   +--------+=======+   +-------+
		if(end.Offset != 0)
			C = new Piece(end.Piece.Block, end.Piece.Start + end.Offset, end.Piece.End);
		else
			C = end.Piece;

		// Remove the old piece(s) (if they existed), leaving just APrev and CNext.
		//
		// +-------+   +-------+
		// | APrev |<->| CNext |
		// +-------+   +-------+
		oldStart = APrev.Next;
		oldEnd = CNext.Prev;
		if(start.Piece != end.Piece || start.Offset != 0 || end.Offset != 0)
		{
			Piece.ListRemoveRange(oldStart, oldEnd);
		}

		// Insert all the new pieces (all the ones that exist)
		//
		// +-------+   +=======+   +==========+   +===+   +========+   +=======+   +-------+
		// | APrev |<->|   A   |<->| newStart |<->|...|<->| newEnd |<->|   C   |<->| CNext |
		// +-------+   +=======+   +==========+   +===+   +========+   +=======+   +-------+
		//
		Piece X = APrev;
		// Insert our new A (if we made one)
		if(start.Offset != 0)
		{
			Piece.ListInsert(APrev, A);
			X = A;
		}
		// Insert the new pieces (if there are any)
		if(newStart != null && newEnd != null)
		{
			Piece.ListInsertRange(X, newStart, newEnd);
			X = newEnd;
		}
		// Insert our new C (if we made one)
		if(end.Offset != 0)
			Piece.ListInsert(X, C);



		// Find the left-most mark on the oldStart piece
		InternalMark m = start;
		while(m.Prev.Piece == oldStart && m.Prev != _Marks.Start)
			m = m.Prev;

		// Move all marks before the start position to the new A piece
		// with the same offset and position
		while(m.Position < start.Position)
		{
			m.Piece = A;
			m = m.Next;
		}

		// Move all marks before the end position to the beginning
		// of the newStart (B) piece
		if(newStart == null)
			newStart = C;
		while(m.Position < end.Position)
		{
			m.Piece = newStart;
			m.Offset = 0;
			m.Position = start.Position;
			m = m.Next;
		}

		// Move all marks after the end position that are still
		// pointing to oldEnd to piece C
		long cStartPos = end.Position + newLength - oldLength;
		while(m != _Marks.Sentinel && m.Piece == oldEnd)
		{
			m.Piece = C;
			m.Position += newLength - oldLength;
			m.Offset = m.Position - cStartPos;
			m = m.Next;
		}

		if(newLength - oldLength != 0)
		{
			while(m != _Marks.Sentinel)
			{
				m.Position += newLength - oldLength;
				m = m.Next;
			}
		}

		_Marks.UpdateAfterSplice(Pieces.Next, end);


		Debug.Assert(oldLength == 0 || 
		             _Marks.DebugMarkChainDoesntReferenceRemovePieces(oldStart, oldEnd), 
		             "Splice: Leave: Mark chain references removed piece");
		// Marks must now be immediately to the right of the splits
		Debug.Assert(start.Offset == 0, "Splice: Leave: Bad start mark offset");
		Debug.Assert(end.Offset == 0, "Splice: Leave: Bad end mark offset");
		// Mark chain must still be in order
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Splice: Leave: Invalid mark chain");
	}

	protected void Replace(HistoryOperation operation, InternalMark curStart, InternalMark curEnd, 
	                       Piece newStart, Piece newEnd, long newLength)
	{
		Debug.Assert(curStart != null && curEnd != null, "Replace: Enter: Invalid curStart/curEnd");
		Debug.Assert((newStart == null && newEnd == null && newLength == 0) || 
		             (newStart != null && newEnd != null), "Replace: Enter: Invalid newStart/newEnd/newLength");
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Replace: Enter: Invalid mark chain");

		BufferChangedEventArgs change = new BufferChangedEventArgs(curStart.Position, curEnd.Position);
		Piece firstRemovedPiece = null;
		Piece lastRemovedPiece = null;

		// Flip the marks if they're the wrong way round
		if(curEnd.Position < curStart.Position)
		{
			InternalMark tmp = curStart;
			curStart = curEnd;
			curEnd = tmp;
		}

		if(curStart.Position == curEnd.Position && curStart.Offset == 0)
		{
			Piece empty = new Piece();
			empty.Prev = curStart.Piece.Prev;
			empty.Next = curStart.Piece;
			AddHistory(operation, curStart.Position, curEnd.Position - curStart.Position, empty, empty);
		}
		else
		{
			if(curEnd.Piece == curStart.Piece || curEnd.Offset > 0)
			{
				AddHistory(operation, curStart.Position, curEnd.Position - curStart.Position, 
				           curStart.Piece, curEnd.Piece);
			}
			else
			{
				AddHistory(operation, curStart.Position, curEnd.Position - curStart.Position, 
				           curStart.Piece, curEnd.Piece.Prev);
			}
		}

		// If the new range of pieces is empty then delete them, 
		// they're probably place holders from the history.
		if(newStart != null && newEnd != null)
		{
			if((newStart.End - newStart.Start) + (newEnd.End - newEnd.Start) == 0)
			{
				Piece tmp;
				do
				{
					tmp = newStart;
					newStart = newStart.Next;
					//delete tmp;

				} while(tmp != newEnd);

				newStart = null;
				newEnd = null;
			}
		}

		// Ensure the marks are on a piece boundaries
		Splice(curStart, curEnd, newStart, newEnd, newLength);

		OnChanged(change);

		Debug.Assert(_Marks.DebugMarkChainIsValid(), "Replace: Leave: Invalid mark chain");
	}

	//
	// Insert
	//
	public void Insert(Mark destStart, Mark destEnd, byte[] text, long length)
	{
		lock(Lock)
		{
			long textOffset = 0;
			long origLength = length;
			Piece head = null;
			Piece tail = null;

			// Add the new text to the current block, create new blocks as necessary
			// to contain all of the text
			while(length != 0)
			{
				// Work out how much will fit in the current block
				long len = Math.Min(length, CurrentBlock.Length - CurrentBlock.Used);

				// Create a new piece covering the inserted text, chaining it
				// to any we've already created
				Piece piece = new Piece(CurrentBlock, CurrentBlock.Used, CurrentBlock.Used + len);
				if(head == null)
					head = piece;
				else
					Piece.ListInsert(tail, piece);
				tail = piece;

				// Copy the text into the block and account for it
				CurrentBlock.SetBytes(CurrentBlock.Used, len, text, textOffset);
				length -= len;
				textOffset += len;
				CurrentBlock.Used += len;

				// Make a new block if we've used all of the current one
				if(CurrentBlock.Used == CurrentBlock.Length)
				{
					Block block = MemoryBlock.Create(OpenBlocks, 4096);
					CurrentBlock = block;
				}
			}

			Replace(HistoryOperation.Insert, (InternalMark)destStart, (InternalMark)destEnd, head, tail, origLength);
		}
	}

	public void Insert(Mark dest, byte[] text, long length)
	{
		lock(Lock)
		{
			Insert((InternalMark)dest, (InternalMark)dest, text, length);
		}
	}

	public void Insert(Mark destStart, Mark destEnd, byte c)
	{
		lock(Lock)
		{
			Insert((InternalMark)destStart, (InternalMark)destEnd, new byte[] {c}, 1);
		}
	}

	public void Insert(Mark dest, byte c)
	{
		lock(Lock)
		{
			Insert((InternalMark)dest, new byte[] {c}, 1);
		}
	}

	public void Insert(byte[] text, long length)
	{
		lock(Lock)
		{
			Insert((InternalMark)Marks.Insert, text, length);
		}
	}

	public void Insert(byte c)
	{
		lock(Lock)
		{
			Insert((InternalMark)Marks.Insert, new byte[] {c}, 1);
		}
	}

	public void Insert(string text)
	{
		lock(Lock)
		{
			Insert(System.Text.Encoding.ASCII.GetBytes(text), text.Length);
		}
	}

	public void Insert(Mark destStart, Mark destEnd, string text)
	{
		lock(Lock)
		{
			Insert((InternalMark)destStart, (InternalMark)destEnd, 
			       System.Text.Encoding.ASCII.GetBytes(text), text.Length);
		}
	}

	public void Insert(Mark dest, string text)
	{
		lock(Lock)
		{
			Insert((InternalMark)dest, System.Text.Encoding.ASCII.GetBytes(text), text.Length);
		}
	}

	
	//
	// Insert File
	//
	
	public void InsertFile(Mark destStart, Mark destEnd, string filename, long offset, long length)
	{
		lock(Lock)
		{
			Block block = FileBlock.Create(OpenBlocks, filename);
			Piece piece = new Piece(block, offset, offset + length);
			Replace(HistoryOperation.InsertFile, (InternalMark)destStart, (InternalMark)destEnd, piece, piece, length);
		}
	}

	//
	// Fill Constant
	//
	public void FillConstant(Mark destStart, Mark destEnd, byte constant, long length)
	{
		lock(Lock)
		{
			Piece piece = null;
			Block block = ConstantBlock.Create(OpenBlocks, constant);
			piece = new Piece(block, 0, length);
			Replace(HistoryOperation.FillConstant, (InternalMark)destStart, (InternalMark)destEnd, 
			        piece, piece, length);
		}
	}
	
	

	//
	// Remove
	//
	public void Remove(Mark start, Mark end)
	{
		lock(Lock)
		{
			Replace(HistoryOperation.Remove, (InternalMark)start, (InternalMark)end, null, null, 0);
		}
	}

	public void Remove(long length)
	{
		lock(Lock)
		{
			Mark end = Marks.Add(Marks.Insert.Position + length);
			Replace(HistoryOperation.Remove, (InternalMark)Marks.Insert, (InternalMark)end, null, null, 0);
			Marks.Remove(end);
		}
	}

	public void Remove(Range range)
	{
		lock(Lock)
		{
			Replace(HistoryOperation.Remove, (InternalMark)range.Start, (InternalMark)range.End, null, null, 0);
		}
	}

	public void Remove(long start, long end)
	{
		lock(Lock)
		{
			Mark s = Marks.Add(start);
			Mark e = Marks.Add(end);
			Replace(HistoryOperation.Remove, (InternalMark)s, (InternalMark)e, null, null, 0);
			Marks.Remove(s);
			Marks.Remove(e);
		}
	}



	//
	// Copy
	//
	protected void Copy(InternalMark destStart, InternalMark destEnd, Piece srcStartPiece, long srcStartOffset, 
	                    Piece srcEndPiece, long srcEndOffset, long length)
	{
		Piece head = null;
		Piece tail = null;
		Piece newPiece;
			
		if(srcEndPiece != srcStartPiece)
		{
			head = new Piece(srcStartPiece.Block, srcStartPiece.Start + srcStartOffset, srcStartPiece.End);
			tail = head;
			
			Piece p = srcStartPiece.Next;
			while(p != srcEndPiece)
			{
				newPiece = new Piece(p.Block, p.Start, p.End);
				Piece.ListInsert(tail, newPiece);
				tail = newPiece;
				p = p.Next;
			}
			
			if(srcEndPiece != Pieces)
			{
				newPiece = new Piece(srcEndPiece.Block, srcEndPiece.Start, srcEndPiece.Start + srcEndOffset);
				Piece.ListInsert(tail, newPiece);
				tail = newPiece;
			}
		}
		else
		{
			head = new Piece(srcStartPiece.Block, srcStartPiece.Start + srcStartOffset, 
			                 srcStartPiece.Start + srcEndOffset);
			tail = head;
		}

		Replace(HistoryOperation.Copy, destStart, destEnd, head, tail, length);
	}
	
	public void Copy(Mark destStart, Mark destEnd, Mark srcStart, Mark srcEnd)
	{
		lock(Lock)
		{
			Copy((InternalMark)destStart, (InternalMark)destEnd, 
			     ((InternalMark)srcStart).Piece, ((InternalMark)srcStart).Offset, 
			     ((InternalMark)srcEnd).Piece, ((InternalMark)srcEnd).Offset, srcEnd.Position - srcStart.Position);
		}
	}

	public void Copy(Mark start, Mark end)
	{
		lock(Lock)
		{
			Copy(Marks.Insert, Marks.Insert, start, end);
		}
	}

	public void Copy(Mark dest, Range range)
	{
		lock(Lock)
		{
			Copy(dest, dest, range.Start, range.End);
		}
	}

	public void Copy(Range range)
	{
		lock(Lock)
		{
			Copy(Marks.Insert, Marks.Insert, range.Start, range.End);
		}
	}

	public void Copy(Mark dest, long start, long end)
	{
		lock(Lock)
		{
			Mark s = Marks.Add(start);
			Mark e = Marks.Add(end);
			Copy(dest, dest, s, e);
			Marks.Remove(s);
			Marks.Remove(e);
		}
	}

	public void Copy(long start, long end)
	{
		lock(Lock)
		{
			Copy(Marks.Insert, start, end);
		}
	}


	//
	// Move
	//
	public void Move(Mark destStart, Mark destEnd, Mark srcStart, Mark srcEnd)
	{
		lock(Lock)
		{
			BeginHistoryGroup();
			Copy(destStart, destEnd, srcStart, srcEnd);
			Remove(srcStart, srcEnd);
			EndHistoryGroup();
		}
	}

	public void Move(Mark dest, Range range)
	{
		lock(Lock)
		{
			BeginHistoryGroup();
			Copy(dest, dest, range.Start, range.End);
			Remove(range.Start, range.End);
			EndHistoryGroup();
		}
	}

	public void Move(Mark dest, long start, long end)
	{
		lock(Lock)
		{
			Mark s = Marks.Add(start);
			Mark e = Marks.Add(end);
			BeginHistoryGroup();
			Copy(dest, dest, s, e);
			Remove(s, e);
			EndHistoryGroup();
			Marks.Remove(s);
			Marks.Remove(e);
		}
	}

	public void Move(Mark start, Mark end)
	{
		lock(Lock)
		{
			BeginHistoryGroup();
			Copy(Marks.Insert, Marks.Insert, start, end);
			Remove(start, end);
			EndHistoryGroup();
		}
	}

	public void Move(Range range)
	{
		lock(Lock)
		{
			BeginHistoryGroup();
			Copy(Marks.Insert, Marks.Insert, range.Start, range.End);
			Remove(range.Start, range.End);
			EndHistoryGroup();
		}
	}

	public void Move(long start, long end)
	{
		lock(Lock)
		{
			Move(Marks.Insert, start, end);
		}
	}

	
	//
	// Logical Operations
	//
	
	public void Or(Mark start, Mark end, byte[] constant)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, 
		                                                                    e.Piece, e.Offset, 
		                                                                    e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationOr(constant), src);
		Replace(HistoryOperation.Or, s, e, piece, piece, e.Position - s.Position);
	}
	
	public void Or(Mark start, Mark end, byte constant)
	{
		Or(start, end, new byte[] { constant });
	}
	
	
	public void And(Mark start, Mark end, byte[] constant)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, 
		                                                                    e.Piece, e.Offset, 
		                                                                    e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationAnd(constant), src);
		Replace(HistoryOperation.And, s, e, piece, piece, e.Position - s.Position);
	}		
	
	public void And(Mark start, Mark end, byte constant)
	{
		And(start, end, new byte[] { constant });
	}
	
	public void Xor(Mark start, Mark end, byte[] constant)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, 
		                                                                    e.Piece, e.Offset, 
		                                                                    e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationXor(constant), src);
		Replace(HistoryOperation.Xor, s, e, piece, piece, e.Position - s.Position);
	}		
	
	public void Xor(Mark start, Mark end, byte constant)
	{
		Xor(start, end, new byte[] { constant });
	}

	public void Invert(Mark start, Mark end)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, 
		                                                                    e.Piece, e.Offset, 
		                                                                    e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationInvert(), src);
		Replace(HistoryOperation.Invert, s, e, piece, piece, e.Position - s.Position);
	}		

	public void Reverse(Mark start, Mark end)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, 
		                                                                    e.Piece, e.Offset, 
		                                                                    e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationReverse(), src);
		Replace(HistoryOperation.Reverse, s, e, piece, piece, e.Position - s.Position);
	}		
	
	public void Shift(Mark start, Mark end, int distance)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, 
		                                                                    e.Piece, e.Offset, 
		                                                                    e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationShift(distance), src);
		Replace(distance > 0 ? HistoryOperation.ShiftRight : HistoryOperation.ShiftLeft, s, e, 
		        piece, piece, e.Position - s.Position);
	}
	
	
	//
	// Clipboard Operations
	//
	public ClipboardRange ClipboardCopy(Mark start, Mark end)
	{
		lock(Lock)
		{
			InternalClipboardRange range = new InternalClipboardRange();
			
			range.StartPiece = ((InternalMark)start).Piece;
			range.StartOffset = ((InternalMark)start).Offset;
			range.EndPiece = ((InternalMark)end).Piece;
			range.EndOffset = ((InternalMark)end).Offset;
			range.Length = end.Position - start.Position;
			
			return range;
		}
	}
	
	public ClipboardRange ClipboardCut(Mark start, Mark end)
	{
		lock(Lock)
		{
			ClipboardRange range = ClipboardCopy(start, end);
			Remove(start, end);
			return range;
		}
	}
	
	public void ClipboardPaste(Mark dstStart, Mark dstEnd, ClipboardRange range)
	{
		lock(Lock)
		{
			InternalClipboardRange r = (InternalClipboardRange)range;
			Copy((InternalMark)dstStart, (InternalMark)dstEnd, r.StartPiece, r.StartOffset, 
			     r.EndPiece, r.EndOffset, r.Length);
		}
	}
	
	
	//
	// Save
	//

	protected InternalSavePlan BuildInPlaceSavePlan()
	{
		if(OriginalFileBlock == null)
		{
			//System.Console.WriteLine("CanSaveInPlace: no original file block, can't save in-place");
			return null;
		}

		InternalSavePlan plan = new InternalSavePlan();
		plan.InPlacePieces = new List< KeyValuePair<long, Piece> >();
		plan.IsInPlace = true;
		plan.TotalLength = Length;
		plan.WriteLength = 0;
		plan.BlockCount = 0;

		bool isContiguous = true;
		long offset = 0;
		Piece p = Pieces;
		while((p = p.Next) != Pieces)
		{
			if(!p.CanSaveInPlace)
			{
				//System.Console.WriteLine("CanSaveInPlace: piece can't be saved in place");
				return null;
			}

			long start, end = 0;
			if(p.GetOffsetsRelativeToBlock(OriginalFileBlock, out start, out end))
			{
				//System.Console.WriteLine("CanSaveInPlace: orig piece: offset: " + offset + 
				//			 ", start: " + start + 
				//			 ", end: " + end);

				if(start < offset || end != start + p.Length)
				{
					// Piece references an earlier part of original file so
					// we can't do an in-place save as we'll have overwritten
					// it by the time we need it.
					//System.Console.WriteLine("Save: earlier reference, can't in-place save");
					return null;
				}

				if(start != offset)
					isContiguous = false;
			}
			else
			{
				//System.Console.WriteLine("CanSaveInPlace: new piece: offset: " + offset +
				//			 ", length: " + p.Length);
			}

			if(!isContiguous || p.Block != OriginalFileBlock)
			{
				plan.InPlacePieces.Add(new KeyValuePair<long, Piece>(offset, p));
				plan.BlockCount += 1;
				plan.WriteLength += p.Length;
			}

			offset += p.Length;
		}

		return plan;
	}

	public SavePlan BuildSavePlan()
	{
		if(CachedSavePlan != null)
			return CachedSavePlan;

		CachedSavePlan = BuildInPlaceSavePlan();
		if(CachedSavePlan == null)
		{
			CachedSavePlan = new InternalSavePlan();
			CachedSavePlan.IsInPlace = false;
			CachedSavePlan.TotalLength = Length;
			CachedSavePlan.WriteLength = Length;
		}

		CachedSavePlan.Pieces = Pieces;
			
		return CachedSavePlan;
	}		

	public bool CanSaveInPlace
	{
		get { return BuildSavePlan().IsInPlace == true; }
	}

	public void SaveAs(string filename)
	{
		// TODO: Protect against overwritting the original file
		//
		FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
		Piece p = Pieces;
		while((p = p.Next) != Pieces)
			p.Write(fs, 0, p.Length);
		fs.Close();
	}

	public void SaveInPlace()
	{
		lock(Lock)
		{
			InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
			if(plan.IsInPlace == false)
				throw new Exception("Can't save in-place");
			System.Console.WriteLine(plan.ToString());

			FileStream stream = OriginalFileBlock.GetWriteStream();
			plan.Execute(stream);
			OriginalFileBlock.ReleaseWriteStream();

			Reopen();
		}
	}

	public void Save()
	{
		lock(Lock)
		{
			InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
			bool wasInPlace = plan.IsInPlace;
			plan.IsInPlace = false;
			System.Console.WriteLine(plan.ToString());

			// Make sure we have write access to the original file
			OriginalFileBlock.GetWriteStream();

			// open temp file
			string origFilename = _FileName;
			string tempFilename;
			do
			{
				tempFilename = origFilename + Path.GetRandomFileName();
			}
			while(File.Exists(tempFilename));

			System.Console.WriteLine("using tmp file: " + tempFilename);
			FileStream stream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write);

			// write to temp file
			plan.Execute(stream);
			stream.Close();

			OriginalFileBlock.ReleaseWriteStream();

			InternalMarkCollection oldMarks = _Marks;
			Close();
			File.Delete(origFilename);
			File.Move(tempFilename, origFilename);
			Open(origFilename);
			_Marks = oldMarks;
			_Marks.UpdateAfterReopen(Pieces);

			plan.IsInPlace = wasInPlace;
		}
	}


	//
	// History
	//

	protected void BeginHistoryGroup() { ++HistoryGroupLevel; }
	protected void EndHistoryGroup() { --HistoryGroupLevel; }

	protected void AddHistory(HistoryOperation operation, long startPosition, long length, Piece start, Piece end)
	{
		InternalHistoryItem oldItem = _History;
		InternalHistoryItem newItem = new InternalHistoryItem(DateTime.Now, operation, startPosition, length, 
		                                                      start, end, HistoryGroupLevel);
		
		newItem.NextSibling = History.FirstChild;
		_History.FirstChild = newItem;
		newItem.Parent = History;
		_History = newItem;
		
		if(HistoryAdded != null)
			HistoryAdded(this, new HistoryEventArgs(oldItem, newItem));

		DebugDumpHistory(String.Empty);
	}

	protected void UndoRedo()
	{
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Undo: Enter: Invalid mark chain");

		if(History != null)
		{
			Piece removeHead = _History.Head.Prev.Next;
			Piece removeTail = _History.Tail.Next.Prev;
			Piece insertHead = _History.Head;
			Piece insertTail = _History.Tail;
			Piece insertAfter = _History.Head.Prev;
			Piece p;
			long lengthChange = 0;
			long editPosition = 0;
			InternalMark editStartMark;
			InternalMark editEndMark;

			// Find the position of the change and create a mark there
			p = Pieces;
			while((p = p.Next) != Pieces && p != removeHead)
				editPosition += p.End - p.Start;
			editStartMark = (InternalMark)Marks.Add(editPosition);

			do
			{
				editPosition += p.End - p.Start;
				p = p.Next;

			} while(p != Pieces && p != removeTail);
			editPosition += p.End - p.Start;
			editEndMark = (InternalMark)Marks.Add(editPosition);

			BufferChangedEventArgs change = new BufferChangedEventArgs(editStartMark.Position, 
			                                                           editEndMark.Position);

			if(removeHead != insertTail.Next && removeTail != insertHead.Prev)
			{
				_History.Head = removeHead;
				_History.Tail = removeTail;

				p = removeHead;
				while(p != Pieces && p != removeTail)
				{
					lengthChange -= p.End - p.Start;
					p = p.Next;
				}
				lengthChange -= p.End - p.Start;

				Piece.ListRemoveRange(removeHead, removeTail);
			}
			else
			{
				Piece empty = new Piece();
				empty.Prev = insertAfter;
				empty.Next = insertAfter.Next;
				_History.Head = empty;
				_History.Tail = empty;
			}

			if(insertHead != insertTail)
			{
				p = insertHead;
				while(p != Pieces && p != insertTail)
				{
					lengthChange += p.End - p.Start;
					p = p.Next;
				}
				lengthChange += p.End - p.Start;

				Piece.ListInsertRange(insertAfter, insertHead, insertTail);
			}
			else
			{
				if(insertHead.Block != null)
				{
					lengthChange += insertHead.End - insertHead.Start;
					Piece.ListInsert(insertAfter, insertHead);
				}
			}
			
			_Marks.UpdateAfterReplace(editStartMark, editEndMark, 0, lengthChange, null);
			_Marks.Remove(editStartMark);
			_Marks.Remove(editEndMark);

			OnChanged(change);
		}

		DebugDumpHistory(String.Empty);
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Undo: Leave: Invalid mark chain");
	}

	public void Undo()
	{
		lock(Lock)
		{
			if(History.Parent != null)
			{
				InternalHistoryItem oldItem = _History;
				InternalHistoryItem newItem = _History.InternalParent;

				UndoRedo();
				_History = _History.InternalParent;
				oldItem.Active = false;
				
				if(HistoryUndone != null)
					HistoryUndone(this, new HistoryEventArgs(oldItem, newItem));
			}
		}
	}

	public void Redo()
	{
		lock(Lock)
		{
			if(History.FirstChild != null)
			{
				InternalHistoryItem oldItem = _History;
				InternalHistoryItem newItem = _History.InternalFirstChild;

				_History = newItem;
				UndoRedo();
				newItem.Active = true;
				
				if(HistoryRedone != null)
					HistoryRedone(this, new HistoryEventArgs(oldItem, newItem));
			}
		}
	}
	
	public void HistoryJump(HistoryItem destination)
	{
		lock(Lock)
		{
			InternalHistoryItem oldItem = _History;
			Stack<InternalHistoryItem> redoPath = new Stack<InternalHistoryItem>();
			
			// Find where the destination branch joins the current branch
			// and record the path from the join point to the destination
			InternalHistoryItem item = (InternalHistoryItem)destination;
			while(!item.Active)
			{
				redoPath.Push(item);
				item = item.InternalParent;
			}
			HistoryItem commonParent = item;
			
			// Undo back to the point where the branches meet
			while(History != commonParent)
			{
				UndoRedo();
				_History.Active = false;
				_History = _History.InternalParent;
			}
			
			// Redo to the destination
			while(History != destination)
			{
				InternalHistoryItem next = redoPath.Pop();
				InternalHistoryItem prev = _History.InternalFirstChild;
				
				item = _History.InternalFirstChild;
				while(item != next)
				{
					prev = item;
					item = item.InternalNextSibling;
				}

				HistoryItem tmp = next.NextSibling;
				next.NextSibling = History.FirstChild;
				_History.FirstChild = next;
				prev.NextSibling = tmp;
				
				_History = item;
				UndoRedo();
				_History.Active = true;
			}
			
			if(HistoryJumped != null)
				HistoryJumped(this, new HistoryEventArgs(oldItem, destination));
		}
	}

	// TODO: Why does this take a length as well as start/end?
	public void GetBytes(Mark start, Mark end, byte[] dest, long length)
	{
		lock(Lock)
		{
			InternalMark s = (InternalMark)start;
			InternalMark e = (InternalMark)end;

			if(s.Piece == e.Piece)
			{
				if(length > e.Position - s.Position)
					length = e.Position - s.Position;

				if(s.Piece != Pieces)
					s.Piece.GetBytes(s.Offset, length, dest, 0);
			}
			else
			{
				long destOffset = 0;
				long len = s.Piece.End - s.Piece.Start - s.Offset;
				if(len > length)
					len = length;
				s.Piece.GetBytes(s.Offset, len, dest, destOffset);
				destOffset += len;
				length -= len;

				Piece p = s.Piece;
				while(length > 0 && (p = p.Next) != e.Piece)
				{
					len = p.End - p.Start;
					if(len > length)
						len = length;
					p.GetBytes(0, len, dest, destOffset);
					destOffset += len;
					length -= len;
				}

				if(length > 0 && p != Pieces)
					p.GetBytes(0, e.Offset > length ? length : e.Offset, dest, destOffset);
			}
		}
	}

	public void GetBytes(long offset, byte[] dest, long length)
	{
		lock(Lock)
		{
			Mark start = Marks.Add(offset);
			Mark end = Marks.Add(offset + length);
			GetBytes(start, end, dest, length);
			Marks.Remove(start);
			Marks.Remove(end);
		}
	}
		
	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
			Close();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);			
	}

	protected void DebugDumpPieceText(string label, Piece head, Piece tail, bool between)
	{
		Console.Write(label);
		Piece p = head;
		int c = 0;
		
		if(between)
			p = p.Next;
		
		while((!between || p == tail) && c < 10)
		{
			if(p == Pieces)
				Console.Write("* ");
			else if(p.Start == Int64.MaxValue || p.End == Int64.MaxValue)
				Console.Write("E ");
			else
			{
				byte[] tmp = new byte[p.End - p.Start];
				p.GetBytes(0, p.End - p.Start, tmp, 0);
						
				Console.Write("\"" + System.Text.ASCIIEncoding.ASCII.GetString(tmp) + "\" ");
			}
			
			if(!between && p == tail)
				break;
			p = p.Next;
			++c;
		}
		Console.Write("\n");
	}

	protected void DebugDumpHistory(InternalHistoryItem item, int indent)
	{
		const string padding = "                                                                                ";
		string pad = padding.Substring(0, indent * 4);

		while(item != null)
		{
			if(item == History)
				Console.Write("=>" + pad.Substring(2));
			else
				Console.Write(pad);

			if(item.Parent != null)
			{
				Console.Write("Head: " + (item.Head.Start == Int64.MaxValue ? -1 : item.Head.Start));
				Console.Write(" => " + (item.Head.End == Int64.MaxValue ? -1 : item.Head.End));
				Console.Write(", Tail: " + (item.Tail.Start == Int64.MaxValue ? -1 : item.Tail.Start));
				Console.Write(" => " + (item.Tail.End == Int64.MaxValue ? -1 : item.Tail.End));
				Console.Write(", RHead: " + (item.Head.Prev.Start == Int64.MaxValue ? -1 : item.Head.Prev.Start));
				Console.Write(" => " + (item.Head.Prev.End == Int64.MaxValue ? -1 : item.Head.Prev.End));
				Console.Write(", RTail: " + (item.Tail.Next.Start == Int64.MaxValue ? -1 : item.Tail.Next.Start));
				Console.Write(" => " + (item.Tail.Next.End == Int64.MaxValue ? -1 : item.Tail.Next.End));
				Console.Write("\n");

				DebugDumpPieceText(pad, item.Head, item.Tail, false);
				DebugDumpPieceText(pad, item.Head.Prev, item.Tail.Next, false);				
			}
			else
				Console.Write("HistoryHead\n");
				
			DebugDumpHistory(item.InternalFirstChild, indent + 1);
			item = item.InternalNextSibling;
		}
	}
	protected void DebugDumpHistory(string msg)
	{
		/*
		Console.WriteLine("\n" + msg + "\n========\n");

		InternalHistoryItem i = _History;
		while(i.InternalParent != null)
			i = i.InternalParent;
		DebugDumpHistory(i, 1); */
	}
	
	public string DebugGetPieces()
	{
		System.Text.StringBuilder tmp = new System.Text.StringBuilder();

		Piece p = Pieces;
		while((p = p.Next) != Pieces)
			tmp.AppendFormat("{{{0},{1}}}", p.Start, p.End);

		return tmp.ToString();
	}
}

