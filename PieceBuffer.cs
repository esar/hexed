using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


public partial class PieceBuffer
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
		public long Length
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
		
		
		public TransformOperationDataSource(Piece startPiece, long startOffset, Piece endPiece, long endOffset, long length)
		{
			StartPiece = startPiece;
			StartOffset = startOffset;
			EndPiece = endPiece;
			EndOffset = endOffset;
			_Length = length;
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
	}
	
	public interface ITransformOperation
	{
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
		
		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] ^= Constant[(start++) % Constant.Length];
		}
	}

	public class TransformOperationInvert : ITransformOperation
	{
		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] ^= 0xFF;
		}
	}

	public class TransformOperationReverse : ITransformOperation
	{
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

		public virtual void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't set data in Piece");
		}
		
		public virtual void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			Block.GetBytes(Start + start, length, dst, dstOffset);
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
		public bool Active 
		{
			get { return _Active; }
			set { _Active = value; } 
		}
		public DateTime Date 
		{
			get { return _Date; }
			set { _Date = value; } 
		}
		public HistoryOperation Operation 
		{
			get { return _Operation; }
			set { _Operation = value; } 
		}
		public long StartPosition 
		{
			get { return _StartPosition; }
			set { _StartPosition = value; } 
		}
		public long Length 
		{
			get { return _Length; }
			set { _Length = value; } 
		}
		
		public HistoryItem Parent 
		{
			get { return _Parent; }
			set { _Parent = value; } 
		}
		public HistoryItem FirstChild 
		{
			get { return _FirstChild; }
			set { _FirstChild = value; } 
		}
		public HistoryItem NextSibling 
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
		
		
		public InternalHistoryItem(DateTime date, HistoryOperation op, long startPosition, long length, Piece head, Piece tail, int groupLevel)
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
	
	
	
	
	protected Piece		Pieces = new Piece();
	protected InternalMarkCollection	_Marks;
	public MarkCollection Marks { get { return _Marks; } }
	protected Block		CurrentBlock;
	protected InternalHistoryItem	_History;
	public HistoryItem History { get { return _History; } }
	protected InternalHistoryItem	_HistoryRoot;
	public HistoryItem HistoryRoot { get { return _HistoryRoot; } }
	public bool CanUndo { get { return _History.Parent != null; } }
	public bool CanRedo { get { return _History.FirstChild != null; } }
	public bool IsModified { get { return _History.Parent != null; } }
	protected int			HistoryGroupLevel;
	
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

	public long	Length
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
		_Marks = new InternalMarkCollection(this, Pieces, Pieces, 0);
		
		CurrentBlock = new MemoryBlock(4096);
		
		_HistoryRoot = _History = new InternalHistoryItem(DateTime.Now, HistoryOperation.New, 0, 0, null, null, 0);
		HistoryGroupLevel = 0;
		
		IndexCacheBytes = new byte[IndexCacheSize];
		IndexCacheStartOffset = Int64.MaxValue;
	}
	
	public PieceBuffer(string filename)
	{
		_FileName = filename;
		
		Block block = FileBlock.Create(filename);
		
		Piece piece = new Piece(block, 0, block.Length);
		Piece.ListInsert(Pieces, piece);

		_Marks = new InternalMarkCollection(this, Pieces, piece, block.Length);
		
		CurrentBlock = new MemoryBlock(4096);
		
		_HistoryRoot = _History = new InternalHistoryItem(DateTime.Now, HistoryOperation.Open, 0, block.Length, null, null, 0);
		HistoryGroupLevel = 0;
		
		IndexCacheBytes = new byte[IndexCacheSize];
		IndexCacheStartOffset = Int64.MaxValue;
	}
	
	protected void OnChanged(BufferChangedEventArgs e)
	{
		Console.WriteLine("Buffer Changed: " + e.StartOffset + " => " + e.EndOffset);
		
		if((e.StartOffset >= IndexCacheStartOffset && e.StartOffset < IndexCacheStartOffset + IndexCacheSize) ||
		   (e.EndOffset >= IndexCacheStartOffset && e.EndOffset < IndexCacheStartOffset + IndexCacheSize))
			IndexCacheStartOffset = Int64.MaxValue;
		if(Changed != null)
			Changed(this, e);
	}
	
	
	protected void SplitPiece(InternalMark start, InternalMark end)
	{
		if(start.Offset == 0 && end.Offset == 0)
			return;
		
		// If the marks are both splitting the same piece then
		// the piece will be replaced with three new pieces
		//
		//              start            end
		// +--------------+---------------+---------------+
		// | A            | B             | C             |
		// +--------------+---------------+---------------+
		if(start.Piece == end.Piece && start.Offset != end.Offset && start.Offset != 0 && end.Offset != 0)
		{
			Piece old = start.Piece;
			Piece A = new Piece(old.Block, old.Start, old.Start + start.Offset);
			Piece B = new Piece(old.Block, old.Start + start.Offset, old.Start + end.Offset);
			Piece C = new Piece(old.Block, old.Start + end.Offset, old.End);
			Piece.ListRemove(old);
			Piece.ListInsert(old.Prev, A);
			Piece.ListInsert(A, B);
			Piece.ListInsert(B, C);
			
			InternalMark m = start;
			while(m.Prev.Piece == old)
				m = m.Prev;
			while(m.Position < start.Position)
			{
				m.Piece = A;
				m = m.Next;
			}
			while(m.Position < end.Position)
			{
				m.Piece = B;
				m.Offset -= A.End - A.Start;
				m = m.Next;
			}
			while(m != _Marks.Sentinel && m.Piece == old)
			{
				m.Piece = C;
				m.Offset -= (A.End - A.Start) + (B.End - B.Start);
				m = m.Next;
			}
		}
		else
		{
			// Split start's piece into two new pieces
			//
			//                start
			// +----------------+-----------------+
			// | A              | B               |
			// +----------------+-----------------+
			if(start.Offset != 0)
			{
				Piece old = start.Piece;
				Piece A = new Piece(old.Block, old.Start, old.Start + start.Offset);
				Piece B = new Piece(old.Block, old.Start + start.Offset, old.End);
				Piece.ListRemove(old);
				Piece.ListInsert(old.Prev, A);
				Piece.ListInsert(A, B);
				
				InternalMark m = start;
				while(m.Prev.Piece == old)
					m = m.Prev;
				while(m.Position < start.Position)
				{
					m.Piece = A;
					m = m.Next;
				}
				while(m != _Marks.Sentinel && m.Piece == old)
				{
					m.Piece = B;
					m.Offset -= (A.End - A.Start);
					m = m.Next;
				}
			}
			
			// Split end's piece into two new pieces
			//
			//                 end
			// +----------------+-----------------+
			// | A              | B               |
			// +----------------+-----------------+
			if(end.Offset != 0)
			{
				Piece old = end.Piece;
				Piece A = new Piece(old.Block, old.Start, old.Start + end.Offset);
				Piece B = new Piece(old.Block, old.Start + end.Offset, old.End);
				Piece.ListRemove(old);
				Piece.ListInsert(old.Prev, A);
				Piece.ListInsert(A, B);
				
				InternalMark m = end;
				while(m.Prev.Piece == old)
					m = m.Prev;
				while(m.Position < end.Position)
				{
					m.Piece = A;
					m = m.Next;
				}
				while(m != _Marks.Sentinel && m.Piece == old)
				{
					m.Piece = B;
					m.Offset -= (A.End - A.Start);
					m = m.Next;
				}
			}
		}		
		
		// Marks must now be immediately to the right of the splits
		Debug.Assert(start.Offset == 0, "SplitPiece: Leave: Bad start mark offset");
		Debug.Assert(end.Offset == 0, "SplitPiece: Leave: Bad end mark offset");
		// Mark chain must still be in order
		Debug.Assert(Marks.DebugMarkChainIsValid(), "SplitPiece: Leave: Invalid mark chain");
	}
	
	protected void Replace(HistoryOperation operation, InternalMark curStart, InternalMark curEnd, Piece newStart, Piece newEnd, long newLength)
	{
		Debug.Assert(curStart != null && curEnd != null, "Replace: Enter: Invalid curStart/curEnd");
		Debug.Assert((newStart == null && newEnd == null && newLength == 0) || (newStart != null && newEnd != null), "Replace: Enter: Invalid newStart/newEnd/newLength");
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
			if(curEnd.Piece == curStart.Piece)
				AddHistory(operation, curStart.Position, curEnd.Position - curStart.Position, curStart.Piece, curEnd.Piece);
			else
				AddHistory(operation, curStart.Position, curEnd.Position - curStart.Position, curStart.Piece, curEnd.Piece.Prev);
		}
		
		// Ensure the marks are on a piece boundaries
		SplitPiece(curStart, curEnd);
		
		// If the range curStart to curEnd is not empty, remove the pieces.
		long removedLength = 0;
		if(curStart.Position != curEnd.Position)
		{
			removedLength = curEnd.Position - curStart.Position;
			
			firstRemovedPiece = curStart.Piece;
			lastRemovedPiece = curEnd.Piece.Prev;
			Piece.ListRemoveRange(curStart.Piece, curEnd.Piece.Prev);
		}
		
		// If the new range of pieces is empty then delete them, 
		// they're probably place holders from the history.
		// Otherwise splice the new pieces into the piece chain
		Piece firstInsertedPiece = null;
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
			}
			else
			{
				firstInsertedPiece = newStart;
				Piece.ListInsertRange(curStart.Piece.Prev, newStart, newEnd);
			}
		}
		
		_Marks.UpdateAfterReplace(curStart, curEnd, removedLength, newLength, firstInsertedPiece);
		
		OnChanged(change);
		
		Debug.Assert(firstRemovedPiece == null || lastRemovedPiece == null || 
		             _Marks.DebugMarkChainDoesntReferenceRemovePieces(firstRemovedPiece, lastRemovedPiece), "Replace: Leave: Mark chain references removed piece");
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
					Block block = new MemoryBlock(4096);
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
			Insert((InternalMark)destStart, (InternalMark)destEnd, System.Text.Encoding.ASCII.GetBytes(text), text.Length);
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
			Block block = FileBlock.Create(filename);
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
			Block block = ConstantBlock.Create(constant);
			piece = new Piece(block, 0, length);
			Replace(HistoryOperation.FillConstant, (InternalMark)destStart, (InternalMark)destEnd, piece, piece, length);
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
	protected void Copy(InternalMark destStart, InternalMark destEnd, Piece srcStartPiece, long srcStartOffset, Piece srcEndPiece, long srcEndOffset, long length)
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
			head = new Piece(srcStartPiece.Block, srcStartPiece.Start + srcStartOffset, srcStartPiece.Start + srcEndOffset);
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
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, e.Piece, e.Offset, e.Position - s.Position);
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
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, e.Piece, e.Offset, e.Position - s.Position);
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
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, e.Piece, e.Offset, e.Position - s.Position);
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
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, e.Piece, e.Offset, e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationInvert(), src);
		Replace(HistoryOperation.Invert, s, e, piece, piece, e.Position - s.Position);
	}		

	public void Reverse(Mark start, Mark end)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, e.Piece, e.Offset, e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationReverse(), src);
		Replace(HistoryOperation.Reverse, s, e, piece, piece, e.Position - s.Position);
	}		
	
	public void Shift(Mark start, Mark end, int distance)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(e.Position - s.Position == 0)
			return;
		
		TransformOperationDataSource src = new TransformOperationDataSource(s.Piece, s.Offset, e.Piece, e.Offset, e.Position - s.Position);
		Piece piece = new TransformPiece(new TransformOperationShift(distance), src);
		Replace(distance > 0 ? HistoryOperation.ShiftRight : HistoryOperation.ShiftLeft, s, e, piece, piece, e.Position - s.Position);
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
			Copy((InternalMark)dstStart, (InternalMark)dstEnd, r.StartPiece, r.StartOffset, r.EndPiece, r.EndOffset, r.Length);
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
		InternalHistoryItem newItem = new InternalHistoryItem(DateTime.Now, operation, startPosition, length, start, end, HistoryGroupLevel);
		
		newItem.NextSibling = History.FirstChild;
		_History.FirstChild = newItem;
		newItem.Parent = History;
		_History = newItem;
		
		if(HistoryAdded != null)
			HistoryAdded(this, new HistoryEventArgs(oldItem, newItem));
		
		DebugDumpHistory("");
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
				
			} while(p != removeTail);
			editPosition += p.End - p.Start;
			editEndMark = (InternalMark)Marks.Add(editPosition);

			BufferChangedEventArgs change = new BufferChangedEventArgs(editStartMark.Position, editEndMark.Position);

			if(removeHead != insertTail.Next && removeTail != insertHead.Prev)
			{
				_History.Head = removeHead;
				_History.Tail = removeTail;
				
				p = removeHead;
				while(p != removeTail)
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
				while(p != insertTail)
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

		DebugDumpHistory("");
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
		return;
		
		Console.WriteLine("\n" + msg + "\n========\n");
	
		InternalHistoryItem i = _History;
		while(i.InternalParent != null)
			i = i.InternalParent;
		DebugDumpHistory(i, 1);
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
