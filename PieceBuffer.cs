using System;
using System.Collections.Generic;
using System.Diagnostics;


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
		public Piece StartPiece;
		public long StartOffset;
		public Piece EndPiece;
		public long EndOffset;
		public long Length;
	}
	
	
	
	public class Piece
	{
		public readonly Block Block;
		public readonly long Start;
		public readonly long End;
		
		public Piece Next;
		public Piece Prev;
		
		public Piece()
		{
			Next = this;
			Prev = this;
			Block = null;
			Start = Int64.MaxValue;
			End = Int64.MaxValue;
		}
		
		public Piece(Piece src)
		{
			Next = src.Next;
			Prev = src.Prev;
			Block = src.Block;
			Start = src.Start;
			End = src.End;
		}
		
		public Piece(Block block, long start, long end)
		{
			Next = this;
			Prev = this;
			Block = block;
			Start = start;
			End = end;
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
		Replace
	}
	
	public class HistoryItem
	{
		public bool Active;
		public DateTime Date;
		public HistoryOperation Operation;
		public long StartPosition;
		public long Length;
		
		public Piece Head;
		public Piece Tail;
		public int GroupLevel;
		
		public HistoryItem Parent;
		public HistoryItem FirstChild;
		public HistoryItem NextSibling;
		
		public HistoryItem(DateTime date, HistoryOperation op, long startPosition, long length, Piece head, Piece tail, int groupLevel)
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
	
	
	
	
	public Piece		Pieces = new Piece();
	public MarkCollection	Marks;
	public Block		CurrentBlock;
	public HistoryItem	History;
	public HistoryItem	HistoryRoot;
	public int			HistoryGroupLevel;
	
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
	
	public byte this[long index]
	{
		get
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
		
		// TODO: Implement set
	}

	public long	Length
	{
		get { return Marks.End.Position; }
	}
	
	public PieceBuffer()
	{
		Marks = new MarkCollection(this, Pieces, Pieces, 0);
		
		CurrentBlock = new MemoryBlock(4096);
		
		HistoryRoot = History = new HistoryItem(DateTime.Now, HistoryOperation.New, 0, 0, null, null, 0);
		HistoryGroupLevel = 0;
		
		IndexCacheBytes = new byte[IndexCacheSize];
		IndexCacheStartOffset = Int64.MaxValue;
	}
	
	public PieceBuffer(string filename)
	{
		Block block = FileBlock.Create(filename);
		
		Piece piece = new Piece(block, 0, block.Length);
		Piece.ListInsert(Pieces, piece);

		Marks = new MarkCollection(this, Pieces, piece, block.Length);
		
		CurrentBlock = new MemoryBlock(4096);
		
		HistoryRoot = History = new HistoryItem(DateTime.Now, HistoryOperation.Open, 0, block.Length, null, null, 0);
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
			while(m != Marks.Sentinel && m.Piece == old)
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
				while(m != Marks.Sentinel && m.Piece == old)
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
				while(m != Marks.Sentinel && m.Piece == old)
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
		
		Marks.UpdateAfterReplace(curStart, curEnd, removedLength, newLength, firstInsertedPiece);
		
		OnChanged(change);
		
		Debug.Assert(firstRemovedPiece == null || lastRemovedPiece == null || 
		             Marks.DebugMarkChainDoesntReferenceRemovePieces(firstRemovedPiece, lastRemovedPiece), "Replace: Leave: Mark chain references removed piece");
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Replace: Leave: Invalid mark chain");
	}
	
	//
	// Insert
	//
	public void Insert(Mark destStart, Mark destEnd, byte[] text, long length)
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

	public void Insert(Mark dest, byte[] text, long length)
	{
		Insert((InternalMark)dest, (InternalMark)dest, text, length);
	}
	
	public void Insert(Mark destStart, Mark destEnd, byte c)
	{
		Insert((InternalMark)destStart, (InternalMark)destEnd, new byte[] {c}, 1);
	}
	
	public void Insert(Mark dest, byte c)
	{
		Insert((InternalMark)dest, new byte[] {c}, 1);
	}
	
	public void Insert(byte[] text, long length)
	{
		Insert((InternalMark)Marks.Insert, text, length);
	}
	
	public void Insert(byte c)
	{
		Insert((InternalMark)Marks.Insert, new byte[] {c}, 1);
	}
	
	public void Insert(string text)
	{
		Insert(System.Text.Encoding.ASCII.GetBytes(text), text.Length);
	}
	
	public void Insert(Mark destStart, Mark destEnd, string text)
	{
		Insert((InternalMark)destStart, (InternalMark)destEnd, System.Text.Encoding.ASCII.GetBytes(text), text.Length);
	}
	
	public void Insert(Mark dest, string text)
	{
		Insert((InternalMark)dest, System.Text.Encoding.ASCII.GetBytes(text), text.Length);
	}

	
	//
	// Insert File
	//
	
	public void InsertFile(Mark destStart, Mark destEnd, string filename, long offset, long length)
	{
		Block block = FileBlock.Create(filename);
		Piece piece = new Piece(block, offset, offset + length);
		Replace(HistoryOperation.InsertFile, (InternalMark)destStart, (InternalMark)destEnd, piece, piece, length);
	}
	
	//
	// Fill Constant
	//
	public void FillConstant(Mark destStart, Mark destEnd, byte constant, long length)
	{
		Piece piece = null;
		Block block = ConstantBlock.Create(constant);
		
		piece = new Piece(block, 0, length);
		
		Replace(HistoryOperation.FillConstant, (InternalMark)destStart, (InternalMark)destEnd, piece, piece, length);
	}
	
	

	//
	// Remove
	//
	public void Remove(Mark start, Mark end)
	{
		Replace(HistoryOperation.Remove, (InternalMark)start, (InternalMark)end, null, null, 0);
	}
	
	public void Remove(long length)
	{
		Mark end = Marks.Add(Marks.Insert.Position + length); 
		Replace(HistoryOperation.Remove, (InternalMark)Marks.Insert, (InternalMark)end, null, null, 0); 
		Marks.Remove(end);
	}
	
	public void Remove(Range range)
	{
		Replace(HistoryOperation.Remove, (InternalMark)range.Start, (InternalMark)range.End, null, null, 0);
	}
	
	public void Remove(long start, long end)
	{
		Mark s = Marks.Add(start);
		Mark e = Marks.Add(end);
		Replace(HistoryOperation.Remove, (InternalMark)s, (InternalMark)e, null, null, 0);
		Marks.Remove(s);
		Marks.Remove(e);
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
		Copy((InternalMark)destStart, (InternalMark)destEnd, 
		     ((InternalMark)srcStart).Piece, ((InternalMark)srcStart).Offset, 
		     ((InternalMark)srcEnd).Piece, ((InternalMark)srcEnd).Offset, srcEnd.Position - srcStart.Position);
	}
	
	public void Copy(Mark start, Mark end)
	{
		Copy(Marks.Insert, Marks.Insert, start, end);
	}
	
	public void Copy(Mark dest, Range range)
	{
		Copy(dest, dest, range.Start, range.End);
	}
	
	public void Copy(Range range)
	{
		Copy(Marks.Insert, Marks.Insert, range.Start, range.End);
	}
	
	public void Copy(Mark dest, long start, long end)
	{
		Mark s = Marks.Add(start);
		Mark e = Marks.Add(end);
		Copy(dest, dest, s, e);
		Marks.Remove(s);
		Marks.Remove(e);
	}
	
	public void Copy(long start, long end)
	{
		Copy(Marks.Insert, start, end);
	}

	
	//
	// Move
	//	
	public void Move(Mark destStart, Mark destEnd, Mark srcStart, Mark srcEnd)
	{
		BeginHistoryGroup();
		Copy(destStart, destEnd, srcStart, srcEnd);
		Remove(srcStart, srcEnd);
		EndHistoryGroup();
	}
	
	public void Move(Mark dest, Range range)
	{
		BeginHistoryGroup();
		Copy(dest, dest, range.Start, range.End);
		Remove(range.Start, range.End);
		EndHistoryGroup();
	}
	
	public void Move(Mark dest, long start, long end)
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
	
	public void Move(Mark start, Mark end)
	{
		BeginHistoryGroup();
		Copy(Marks.Insert, Marks.Insert, start, end);
		Remove(start, end);
		EndHistoryGroup();
	}
	
	public void Move(Range range)
	{
		BeginHistoryGroup();
		Copy(Marks.Insert, Marks.Insert, range.Start, range.End);
		Remove(range.Start, range.End);
		EndHistoryGroup();
	}
	
	public void Move(long start, long end)
	{
		Move(Marks.Insert, start, end);
	}
	
	
	//
	// Clipboard Operations
	//
	public ClipboardRange ClipboardCopy(Mark start, Mark end)
	{
		ClipboardRange range = new ClipboardRange();
		
		range.StartPiece = ((InternalMark)start).Piece;
		range.StartOffset = ((InternalMark)start).Offset;
		range.EndPiece = ((InternalMark)end).Piece;
		range.EndOffset = ((InternalMark)end).Offset;
		range.Length = end.Position - start.Position;
		
		return range;
	}
	
	public ClipboardRange ClipboardCut(Mark start, Mark end)
	{
		ClipboardRange range = ClipboardCopy(start, end);
		Remove(start, end);
		return range;
	}
	
	public void ClipboardPaste(Mark dstStart, Mark dstEnd, ClipboardRange range)
	{
		Copy((InternalMark)dstStart, (InternalMark)dstEnd, range.StartPiece, range.StartOffset, range.EndPiece, range.EndOffset, range.Length);
	}
	
	
	
	//
	// History
	//
	
	public void BeginHistoryGroup() { ++HistoryGroupLevel; }
	public void EndHistoryGroup() { --HistoryGroupLevel; }
	
	public void AddHistory(HistoryOperation operation, long startPosition, long length, Piece start, Piece end)
	{
		HistoryItem oldItem = History;
		HistoryItem newItem = new HistoryItem(DateTime.Now, operation, startPosition, length, start, end, HistoryGroupLevel);
		
		newItem.NextSibling = History.FirstChild;
		History.FirstChild = newItem;
		newItem.Parent = History;
		History = newItem;
		
		if(HistoryAdded != null)
			HistoryAdded(this, new HistoryEventArgs(oldItem, newItem));
		
		DebugDumpHistory("");
	}
	
	protected void UndoRedo()
	{
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Undo: Enter: Invalid mark chain");

		if(History != null)
		{
			Piece removeHead = History.Head.Prev.Next;
			Piece removeTail = History.Tail.Next.Prev;
			Piece insertHead = History.Head;
			Piece insertTail = History.Tail;
			Piece insertAfter = History.Head.Prev;
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
				History.Head = removeHead;
				History.Tail = removeTail;
				
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
				History.Head = empty;
				History.Tail = empty;
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
			
			Marks.UpdateAfterReplace(editStartMark, editEndMark, 0, lengthChange, null);
			Marks.Remove(editStartMark);
			Marks.Remove(editEndMark);
			
			OnChanged(change);
		}

		DebugDumpHistory("");
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Undo: Leave: Invalid mark chain");
	}
	
	public void Undo()
	{
		if(History.Parent != null)
		{
			HistoryItem oldItem = History;
			HistoryItem newItem = History.Parent;
			
			UndoRedo();
			History = History.Parent;
			oldItem.Active = false;
			
			if(HistoryUndone != null)
				HistoryUndone(this, new HistoryEventArgs(oldItem, newItem));
		}
	}
	
	public void Redo()
	{
		if(History.FirstChild != null)
		{
			HistoryItem oldItem = History;
			HistoryItem newItem = History.FirstChild;
			
			History = newItem;
			UndoRedo();
			newItem.Active = true;
			
			if(HistoryRedone != null)
				HistoryRedone(this, new HistoryEventArgs(oldItem, newItem));
		}
	}
	
	public void HistoryJump(HistoryItem destination)
	{
		HistoryItem oldItem = History;
		Stack<HistoryItem> redoPath = new Stack<HistoryItem>();
		
		// Find where the destination branch joins the current branch
		// and record the path from the join point to the destination
		HistoryItem item = destination;
		while(!item.Active)
		{
			redoPath.Push(item);
			item = item.Parent;
		}
		HistoryItem commonParent = item;
		
		// Undo back to the point where the branches meet
		while(History != commonParent)
		{
			UndoRedo();
			History.Active = false;
			History = History.Parent;
		}
		
		// Redo to the destination
		while(History != destination)
		{
			HistoryItem next = redoPath.Pop();
			HistoryItem prev = History.FirstChild;
			
			item = History.FirstChild;
			while(item != next)
			{
				prev = item;
				item = item.NextSibling;
			}

			HistoryItem tmp = next.NextSibling;
			next.NextSibling = History.FirstChild;
			History.FirstChild = next;
			prev.NextSibling = tmp;
			
			History = item;
			UndoRedo();
			History.Active = true;
		}
		
		if(HistoryJumped != null)
			HistoryJumped(this, new HistoryEventArgs(oldItem, destination));
	}
	
	// TODO: Why does this take a length as well as start/end?
	public void GetBytes(Mark start, Mark end, byte[] dest, long length)
	{
		InternalMark s = (InternalMark)start;
		InternalMark e = (InternalMark)end;
		
		if(s.Piece == e.Piece)
		{
			if(length > e.Position - s.Position)
				length = e.Position - s.Position;
				
			if(s.Piece != Pieces)
				s.Piece.Block.GetBytes(s.Piece.Start + s.Offset, length, dest, 0);
		}
		else
		{
			long destOffset = 0;
			long len = s.Piece.End - s.Piece.Start - s.Offset;
			if(len > length)
				len = length;
			s.Piece.Block.GetBytes(s.Piece.Start + s.Offset, len, dest, destOffset);
			destOffset += len;
			length -= len;
			
			Piece p = s.Piece;
			while(length > 0 && (p = p.Next) != e.Piece)
			{
				len = p.End - p.Start;
				if(len > length)
					len = length;
				p.Block.GetBytes(p.Start, len, dest, destOffset);
				destOffset += len;
				length -= len;
			}
				
			if(length > 0 && p != Pieces)
				p.Block.GetBytes(p.Start, e.Offset > length ? length : e.Offset, dest, destOffset);
		}
	}
	
	public void GetBytes(long offset, byte[] dest, long length)
	{
		Mark start = Marks.Add(offset);
		Mark end = Marks.Add(offset + length);
		GetBytes(start, end, dest, length);
		Marks.Remove(start);
		Marks.Remove(end);
	}
		
	public void DebugDumpPieceText(string label, Piece head, Piece tail, bool between)
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
				p.Block.GetBytes(p.Start, p.End - p.Start, tmp, 0);
						
				Console.Write("\"" + System.Text.ASCIIEncoding.ASCII.GetString(tmp) + "\" ");
			}
			
			if(!between && p == tail)
				break;
			p = p.Next;
			++c;
		}
		Console.Write("\n");
	}
	
	public void DebugDumpHistory(HistoryItem item, int indent)
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
				
			DebugDumpHistory(item.FirstChild, indent + 1);
			item = item.NextSibling;
		}
	}
	public void DebugDumpHistory(string msg)
	{
		return;
		
		Console.WriteLine("\n" + msg + "\n========\n");
	
		HistoryItem i = History;
		while(i.Parent != null)
			i = i.Parent;
		DebugDumpHistory(i, 1);
	}
}
