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
	
	public class ClipboardRange
	{
		protected long _Length;
		public long Length
		{
			get { return _Length; }
		}

		public virtual bool IsValid 
		{ 
			get { return true; }
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

		public override bool IsValid
		{
			get  { return StartPiece.Next != null && EndPiece.Next != null; }
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

		public Piece(List<Piece> allocatedPieces)
		{
			allocatedPieces.Add(this);

			Next = this;
			Prev = this;
			Block = null;
			Start = Int64.MaxValue;
			End = Int64.MaxValue;
		}

		public Piece(List<Piece> allocatedPieces, IBlock block, long start, long end)
		{
			allocatedPieces.Add(this);

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
		
		public virtual void Write(InternalSavePlan dest, long start, long length)
		{
			Block.Write(dest, Start, length);
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
	


	protected List<Piece>            AllocatedPieces;
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
	public bool                     CanUndo { get { return _History != null && _History.Parent != null; } }
	public bool                     CanRedo { get { return _History != null && _History.FirstChild != null; } }
	public bool                     IsModified { get { return _History != null && _History.Parent != null; } }
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

		AllocatedPieces = new List<Piece>();
		OpenBlocks  = new Dictionary<string,Block>();

		Pieces = new Piece(AllocatedPieces);

		_Marks = new InternalMarkCollection(this, Pieces, Pieces, 0);

		CurrentBlock = MemoryBlock.Create(OpenBlocks, 4096);
		
		_HistoryRoot = _History = new InternalHistoryItem(DateTime.Now, HistoryOperation.New, 0, 0, 0, 0, 0, 0, null, null, 0);
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

		AllocatedPieces = new List<Piece>();
		OpenBlocks  = new Dictionary<string,Block>();

		_FileName = filename;

		OriginalFileBlock = FileBlock.Create(OpenBlocks, filename);
		Block block = OriginalFileBlock;

		Pieces = new Piece(AllocatedPieces);
		Piece piece = new Piece(AllocatedPieces, block, 0, block.Length);
		Piece.ListInsert(Pieces, piece);

		_Marks = new InternalMarkCollection(this, Pieces, piece, block.Length);

		CurrentBlock = MemoryBlock.Create(OpenBlocks, 4096);

		_HistoryRoot = _History = new InternalHistoryItem(DateTime.Now, HistoryOperation.Open, 
		                                                  0, 0, block.Length, 0, 0, block.Length, 
		                                                  null, null, 0);
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
		// Some pieces may still be referenced by clipboard ranges.
		// Unlink all the pieces so that all pieces not referenced by
		// a clipboard range are inaccessible and eligible for garbage 
		// collection. 
		// Setting prev/next to null also marks the piece as invalid so
		// if an attempt is made to perform a paste operation with an
		// old piece, it will fail.
		foreach(Piece p in AllocatedPieces)
		{
			p.Prev = null;
			p.Next = null;
		}
		AllocatedPieces.Clear();

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
		Console.WriteLine("Buffer Changed: " + e.StartOffset + " => " + e.EndOffset);

		// Invalidate any cached save plan
		CachedSavePlan = null;

		// Invalidate the index cache if the change covers all or part of it
		long indexCacheEndOffset = IndexCacheStartOffset + IndexCacheSize;
		if((e.StartOffset >= IndexCacheStartOffset && e.StartOffset < indexCacheEndOffset) ||
		   (e.EndOffset >= IndexCacheStartOffset && e.EndOffset < indexCacheEndOffset) ||
		   (e.StartOffset < IndexCacheStartOffset && e.EndOffset >= indexCacheEndOffset))
		{
			IndexCacheStartOffset = Int64.MaxValue;
		}

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
			A = new Piece(AllocatedPieces, start.Piece.Block, start.Piece.Start, start.Piece.Start + start.Offset);
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
			C = new Piece(AllocatedPieces, end.Piece.Block, end.Piece.Start + end.Offset, end.Piece.End);
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

		BufferChangedEventArgs change = new BufferChangedEventArgs(curStart.Position, 
		                                                           curEnd.Position - curStart.Position == newLength ?
		                                                           curStart.Position + newLength : Length);

		// Flip the marks if they're the wrong way round
		if(curEnd.Position < curStart.Position)
		{
			InternalMark tmp = curStart;
			curStart = curEnd;
			curEnd = tmp;
		}

		if(curStart.Position == curEnd.Position && curStart.Offset == 0)
		{
			Piece empty = new Piece(AllocatedPieces);
			empty.Prev = curStart.Piece.Prev;
			empty.Next = curStart.Piece;
			AddHistory(operation, curStart.Position, 0, newLength,
			           curStart.Position, 0, newLength,
			           empty, empty);
		}
		else
		{
			long oldLength = curEnd.Position - curStart.Position;
			if(curEnd.Piece == curStart.Piece || curEnd.Offset > 0)
			{
				long oldPieceLength = (curEnd.Position - curEnd.Offset + curEnd.Piece.Length) - 
				                      (curStart.Position - curStart.Offset);
				long newPieceLength = oldPieceLength + (newLength - oldLength);
				AddHistory(operation, curStart.Position, oldLength, newLength,
				           curStart.Position - curStart.Offset,
				           oldPieceLength, newPieceLength,
				           curStart.Piece, curEnd.Piece);
			}
			else
			{
				long oldPieceLength = curEnd.Position;
				long newPieceLength = oldPieceLength + (newLength - oldLength);
				AddHistory(operation, curStart.Position, oldLength, newLength,
				           curStart.Position - curStart.Offset,
				           oldPieceLength, newPieceLength,
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
				Piece piece = new Piece(AllocatedPieces, CurrentBlock, CurrentBlock.Used, CurrentBlock.Used + len);
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
			Piece piece = new Piece(AllocatedPieces, block, offset, offset + length);
			Replace(HistoryOperation.InsertFile, (InternalMark)destStart, (InternalMark)destEnd, piece, piece, length);
		}
	}

	//
	// Fill Constant
	//
	public void FillConstant(Mark destStart, Mark destEnd, byte[] constant, long length)
	{
		lock(Lock)
		{
			Piece piece = null;
			Block block = ConstantBlock.Create(OpenBlocks, constant);
			piece = new Piece(AllocatedPieces, block, 0, length);
			Replace(HistoryOperation.FillConstant, (InternalMark)destStart, (InternalMark)destEnd, 
			        piece, piece, length);
		}
	}

	//public void FillConstant(Mark destStart, Mark destEnd, byte constant, long length)
	//{
	//	byte[] c = new byte[] { constant };
	//	FillConstant(destStart, destEnd, c, length);
	//}
	
	

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
			head = new Piece(AllocatedPieces, srcStartPiece.Block, srcStartPiece.Start + srcStartOffset, srcStartPiece.End);
			tail = head;
			
			Piece p = srcStartPiece.Next;
			while(p != srcEndPiece)
			{
				newPiece = new Piece(AllocatedPieces, p.Block, p.Start, p.End);
				Piece.ListInsert(tail, newPiece);
				tail = newPiece;
				p = p.Next;
			}
			
			if(srcEndPiece != Pieces)
			{
				newPiece = new Piece(AllocatedPieces, srcEndPiece.Block, srcEndPiece.Start, srcEndPiece.Start + srcEndOffset);
				Piece.ListInsert(tail, newPiece);
				tail = newPiece;
			}
		}
		else
		{
			head = new Piece(AllocatedPieces, srcStartPiece.Block, srcStartPiece.Start + srcStartOffset, 
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
		Piece piece = new TransformPiece(AllocatedPieces, new TransformOperationOr(constant), src);
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
		Piece piece = new TransformPiece(AllocatedPieces, new TransformOperationAnd(constant), src);
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
		Piece piece = new TransformPiece(AllocatedPieces, new TransformOperationXor(constant), src);
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
		Piece piece = new TransformPiece(AllocatedPieces, new TransformOperationInvert(), src);
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
		Piece piece = new TransformPiece(AllocatedPieces, new TransformOperationReverse(), src);
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
		Piece piece = new TransformPiece(AllocatedPieces, new TransformOperationShift(distance), src);
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

			// Check is the piece is invalid because it's from before
			// a save operation and therefore a different piece chain
			if(!r.IsValid)
				return;

			Copy((InternalMark)dstStart, (InternalMark)dstEnd, r.StartPiece, r.StartOffset, 
			     r.EndPiece, r.EndOffset, r.Length);
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
}

