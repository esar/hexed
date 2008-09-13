using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


public class PieceBuffer
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
	
	public class Mark
	{
		public Piece	Piece;
		public long	Offset;
		public long	Position;
		public bool		Modified;
		
		public Mark Next;
		public Mark Prev;
		
		public Mark()
		{
			Piece = null;
			Offset = Int64.MaxValue;
			Position = Int64.MaxValue;
			Modified = false;
			Next = this;
			Prev = this;
		}
		
		public Mark(Piece piece, long offset, long position)
		{
			Piece = piece;
			Offset = offset;
			Position = position;
			Modified = false;
			Next = this;
			Prev = this;
		}
		
		public void CopyTo(Mark dest)
		{
			dest.Next = Next;
			dest.Prev = Prev;
			dest.Piece = Piece;
			dest.Offset = Offset;
			dest.Position = Position;
			dest.Modified = Modified;
		}

		public static void ListInsert(Mark list, Mark item)
		{
			item.Next = list.Next;
			item.Prev = list;
			list.Next.Prev = item;
			list.Next = item;
		}

		public static void ListInsertRange(Mark list, Mark first, Mark last)
		{
			last.Next = list.Next;
			first.Prev = list;
			last.Next.Prev = last;
			first.Prev.Next = first;
		}

		public static void ListRemove(Mark item)
		{
			item.Prev.Next = item.Next;
			item.Next.Prev = item.Prev;
		}
												
		public static void ListRemoveRange(Mark first, Mark last)
		{
			first.Prev.Next = last.Next;
			last.Next.Prev = first.Prev;
		}		
	}
	
	public class Range
	{
		public Mark Start;
		public Mark End;

		public Range(Mark start, Mark end)
		{
			Start = start; 
			End = end;
		}
	}
	
	public abstract class Block
	{
		public long Length;
		public long Used;
		public Block Prev;

		public abstract byte this[long index] { get; set; }
		public abstract void GetBytes(long start, long length, byte[] src, long srcOffset);
		public abstract void SetBytes(long start, long length, byte[] dst, long dstOffset);
	}
	
	public class FileBlock : Block
	{
		private FileStream	FS;
		private byte[]		Buffer = new byte[4096];
		private uint		BufferedLength = 0;
		private uint		MaxLength = 4096;
		private long		StartAddress = 0;
		
		public override byte this[long i]
		{
			get	
			{
				if(i < StartAddress || i >= StartAddress + BufferedLength)
				{
					StartAddress = i - MaxLength / 2;
					if(StartAddress < 0)
						StartAddress = 0;
					FS.Seek(StartAddress, SeekOrigin.Begin);
					BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
				}

				if(i < StartAddress || i >= StartAddress + BufferedLength)
					return 0;
				else
					return Buffer[i - StartAddress];				
			}
			set	{ }
		}
		
		public FileBlock(string filename)
		{
			FS = new FileStream(filename, FileMode.Open, FileAccess.Read);
			Length = FS.Length;
			Used = Length;
		}
		
		public override void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			while(length > 0)
			{
				if(start < StartAddress || start >= StartAddress + BufferedLength)
				{
					StartAddress = start;
					FS.Seek(StartAddress, SeekOrigin.Begin);
					BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
				}

				if(start < StartAddress || start >= StartAddress + BufferedLength)
					throw new Exception("Failed to read from stream");

				long offset = start - StartAddress;
				long len = length > BufferedLength - offset ? BufferedLength - offset : length;
				Array.Copy(Buffer, offset, dst, dstOffset, len);
				dstOffset += len;
				length -= len;
				start += len;
			}
		}
		
		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't SetBytes() on a FileBlock");
		}
	}
	
	public class MemoryBlock : Block
	{
		public byte[] Buffer;
		
		public override byte this[long index]
		{
			get { return Buffer[index]; }
			set { Buffer[index] = value; }
		}
		
		public MemoryBlock(long size)
		{
			Buffer = new byte[size];
			Length = size;
			Used = 0;
		}
		
		public override void GetBytes(long start, long length, byte[] dest, long destOffset)
		{
			Array.Copy(Buffer, start, dest, destOffset, length);
		}
		
		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			Array.Copy(src, srcOffset, Buffer, start, length);
		}
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
	
	public class HistoryItem
	{
		public Piece Head;
		public Piece Tail;
		public int GroupLevel;
		
		public HistoryItem Parent;
		public HistoryItem FirstChild;
		public HistoryItem NextSibling;
		
		public HistoryItem(Piece head, Piece tail, int groupLevel)
		{
			Head = head;
			Tail = tail;
			GroupLevel = groupLevel;
		}
	}
	
	
	
	
	public Piece		Pieces = new Piece();
	public Mark			Marks = new Mark();
	public Block		CurrentBlock;
	public HistoryItem	History = new HistoryItem(null, null, 0);
	public int			HistoryGroupLevel;
	public Mark			InsertMark;
	public Mark			StartMark;
	public Mark			EndMark;
	
	const int IndexCacheSize = 4096;
	long IndexCacheStartOffset;
	byte[] IndexCacheBytes;
	
	public delegate void BufferChangedEventHandler(object sender, BufferChangedEventArgs e);
	public event BufferChangedEventHandler Changed;
	
	public byte this[long index]
	{
		get
		{
			if(index < IndexCacheStartOffset ||
			   index >= IndexCacheStartOffset + IndexCacheSize)
			{
				IndexCacheStartOffset = index;
				GetBytes(index, IndexCacheBytes, IndexCacheSize);
			}
			return IndexCacheBytes[index - IndexCacheStartOffset];
		}
		
		// TODO: Implement set
	}

	public long	Length
	{
		get { return EndMark.Position; }
	}
	
	public PieceBuffer()
	{
		EndMark = new Mark(Pieces, 0, 0);
		Mark.ListInsert(Marks, EndMark);
		InsertMark = new Mark(Pieces, 0, 0);
		Mark.ListInsert(Marks, InsertMark);
		StartMark = new Mark(Pieces, 0, 0);
		Mark.ListInsert(Marks, StartMark);
	
		CurrentBlock = new MemoryBlock(4096);
		
		HistoryGroupLevel = 0;
		
		IndexCacheBytes = new byte[IndexCacheSize];
		IndexCacheStartOffset = Int64.MaxValue;
	}
	
	public PieceBuffer(string filename)
	{
		Block block = new FileBlock(filename);
		
		Piece piece = new Piece(block, 0, block.Length);
		Piece.ListInsert(Pieces, piece);

		EndMark = new Mark(Pieces, 0, block.Length);
		Mark.ListInsert(Marks, EndMark);
		InsertMark = new Mark(piece, 0, 0);
		Mark.ListInsert(Marks, InsertMark);
		StartMark = new Mark(piece, 0, 0);
		Mark.ListInsert(Marks, StartMark);

		CurrentBlock = new MemoryBlock(4096);
		CurrentBlock.Prev = block;
		
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
	
	public void MoveMarkRight(Mark mark, long distance)
	{
		Debug.Assert(DebugMarkChainIsValid(), "MoveMarkRight: Enter: Invalid mark chain");
		
		// If the desired position isn't contained in the current piece or the next
		// mark is before the desired position
		if(mark.Offset + distance >= mark.Piece.End - mark.Piece.Start ||
		   mark.Next.Position < mark.Position + distance)
		{
			// Find nearest mark before the destination position and move the
			// specified mark to that position (must be to the right in mark chain)
			Mark m = mark;
			while((m = m.Next) != Marks && m.Position <= mark.Position + distance)
				;
			if(m.Prev != mark)
			{
				m = m.Prev;
		
				distance -= m.Position - mark.Position;
				
				Mark.ListRemove(mark);
				m.CopyTo(mark);
				Mark.ListInsert(m, mark);
			}
		

			// Move the mark to the beginning of the current piece
			distance += mark.Offset;
			mark.Position -= mark.Offset;
			mark.Offset = 0;
			
			// Move the specified mark by whole pieces
			while(distance >= mark.Piece.End - mark.Piece.Start)
			{
				// Stop if we're at the end of the buffer
				if(mark.Piece == Pieces)
				{
					Debug.Assert(DebugMarkChainIsValid(), "MoveMarkRight: Leave: Invalid mark chain");
					return;
				}
		
				distance -= mark.Piece.End - mark.Piece.Start;
				mark.Position += mark.Piece.End - mark.Piece.Start;
				mark.Piece = mark.Piece.Next;			
			}
		}
			
		// Move the specified mark to the correct location in the piece
		mark.Offset += distance;
		mark.Position += distance;
		
		
		Debug.Assert(DebugMarkChainIsValid(), "MoveMarkRight: Leave: Invalid mark chain");
	}
	
	public void MoveMarkLeft(Mark mark, long distance)
	{
		Debug.Assert(DebugMarkChainIsValid(), "MoveMarkLeft: Enter: Invalid mark chain");
		
		if(distance > mark.Position)
			distance = mark.Position;
		
		if(	distance - mark.Offset > 0 ||
		    mark.Prev.Position < mark.Position - distance ||
			mark.Prev == Marks)
		{
			// Find nearest mark after the destination position and move the
			// specified mark to that position (must be to left in mark chain)
			Mark m = mark;
			while((m = m.Prev) != Marks && m.Position >= mark.Position - distance)
				;
			if(m.Next != mark)
			{
				distance -= mark.Position - m.Next.Position;
				Mark.ListRemove(mark);
				m.Next.CopyTo(mark);
				Mark.ListInsert(m, mark);
			}
				
			// Move the mark to the beginning of the current piece
			distance -= mark.Offset;
			mark.Position -= mark.Offset;
			mark.Offset = 0;
				
			// Move the specified mark by whole pieces
			while(distance > 0) // mark->piece->end - mark->piece->start)
			{
				if(mark.Piece.Prev == Pieces)
				{
					Debug.Assert(DebugMarkChainIsValid(), "MoveMarkLeft: Leave: Invalid mark chain");
					return;
				}
			
				mark.Piece = mark.Piece.Prev;
				mark.Position -= mark.Piece.End - mark.Piece.Start;
				distance -= mark.Piece.End - mark.Piece.Start;
			}
		}
		
		// Move the specified mark to the correct location in the piece		
		mark.Offset -= distance;
		mark.Position -= distance;
		
		
		Debug.Assert(DebugMarkChainIsValid(), "MoveMarkLeft: Leave: Invalid mark chain");
	}
	
	public Mark CreateMark(long distance)
	{
		Debug.Assert(DebugMarkChainIsValid(), "CreateMark: Enter: Invalid mark chain");
		
		Mark mark = new Mark();
		InsertMark.CopyTo(mark);
		Mark.ListInsert(InsertMark, mark);
		MoveMark(mark, distance);
		
		Debug.Assert(DebugMarkChainIsValid(), "CreateMark: Leave: Invalid mark chain");
		
		return mark;
	}
	
	public void DestroyMark(Mark mark)
	{
		if(mark == StartMark || mark == InsertMark || mark == EndMark)
			return;
		
		Mark.ListRemove(mark);
		
		Debug.Assert(DebugMarkChainIsValid(), "DestroyMark: Invalid mark chain");
	}
	
	//
	// Marks
	//
	public Mark CreateMarkAbsolute(long position)
	{
		return CreateMark(position - InsertMark.Position);
	}

	public void MoveMark(Mark mark, long distance)
	{
		if(distance > 0) MoveMarkRight(mark, distance);
		else if(distance < 0) MoveMarkLeft(mark, 0 - distance);
	}
	
	public void MoveMarkAbsolute(Mark mark, long position)
	{
		MoveMark(mark, position - mark.Position);
	}
	
	public void MoveMark(long distance)
	{
		MoveMark(InsertMark, distance);
	}
	
	public void MoveMarkAbsolute(long position)
	{
		MoveMark(InsertMark, position - InsertMark.Position);
	}
	
	
	protected void SplitPiece(Mark start, Mark end)
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
			
			Mark m = start;
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
			while(m != Marks && m.Piece == old)
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
				
				Mark m = start;
				while(m.Prev.Piece == old)
					m = m.Prev;
				while(m.Position < start.Position)
				{
					m.Piece = A;
					m = m.Next;
				}
				while(m != Marks && m.Piece == old)
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
				
				Mark m = end;
				while(m.Prev.Piece == old)
					m = m.Prev;
				while(m.Position < end.Position)
				{
					m.Piece = A;
					m = m.Next;
				}
				while(m != Marks && m.Piece == old)
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
		Debug.Assert(DebugMarkChainIsValid(), "SplitPiece: Leave: Invalid mark chain");
	}
	
	protected void Replace(Mark curStart, Mark curEnd, Piece newStart, Piece newEnd, long newLength)
	{
		Debug.Assert(curStart != null && curEnd != null, "Replace: Enter: Invalid curStart/curEnd");
		Debug.Assert((newStart == null && newEnd == null && newLength == 0) || (newStart != null && newEnd != null), "Replace: Enter: Invalid newStart/newEnd/newLength");
		Debug.Assert(DebugMarkChainIsValid(), "Replace: Enter: Invalid mark chain");
		
		BufferChangedEventArgs change = new BufferChangedEventArgs(curStart.Position, curEnd.Position);
		Piece firstRemovedPiece = null;
		Piece lastRemovedPiece = null;
		
		// Flip the marks if they're the wrong way round
		if(curEnd.Position < curStart.Position)
		{
			Mark tmp = curStart;
			curStart = curEnd;
			curEnd = tmp;
		}
		
		if(curStart.Position == curEnd.Position)
		{
			Piece empty = new Piece();
			empty.Prev = curStart.Piece.Prev;
			empty.Next = curStart.Piece;
			AddHistory(empty, empty);
		}
		else
			AddHistory(curStart.Piece, curEnd.Piece.Prev);
		
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
		
		
		// Remove/insert is done so now we need to update the marks.
		// First, find the left-most mark with the same position as cur_end.
		Mark m = curEnd;
		while(m.Position == m.Prev.Position)
			m = m.Prev;
		
		// Move any marks within the removed region to the point
		// immediately following the removed region (curEnd or m)
		if(curStart.Position != curEnd.Position)
		{
			// Make sure start mark is left-most of all marks with
			// the same position, otherwise we might miss some.
			while(curStart.Position == curStart.Prev.Position)
				curStart = curStart.Prev;
			
			// Loop through setting their position to match curEnd / m
			// until we get to m
			if(firstInsertedPiece == null)
			{
				while(curStart != m)
				{
					curStart.Piece = m.Piece;
					curStart.Offset = m.Offset;
					curStart.Position = m.Position + newLength - removedLength;
					curStart.Modified = true;
					curStart = curStart.Next;
				}
			}
			else
			{
				while(curStart != m)
				{
					curStart.Piece = firstInsertedPiece;
					curStart.Offset = 0;
					curStart.Position = m.Position - removedLength;
					curStart.Modified = true;
					curStart = curStart.Next;
				}
			}
		}
		
		// Flag first mark after insert/remove as modified
		m.Modified = true;
		
		// Update the position of all marks following the removed/inserted
		// pieces if the change is non zero
		if(newLength - removedLength != 0)
		{
			do
			{
				m.Position += newLength - removedLength;
				
			} while((m = m.Next) != Marks);
		}
		
		// Finally, position the insert mark immediately 
		// after the removed/inserted pieces (curEnd)
		// and make sure the start mark is still at the start
		if(InsertMark != curEnd)
		{
			Mark.ListRemove(InsertMark);
			curEnd.CopyTo(InsertMark);
			Mark.ListInsert(curEnd, InsertMark);
		
		}
		if(StartMark.Position != 0)
		{		
			Mark.ListRemove(StartMark);
			StartMark.Piece = Pieces.Next;
			StartMark.Offset = 0;
			StartMark.Position = 0;
			Mark.ListInsert(Marks, StartMark);
		}
		
		OnChanged(change);
		
		Debug.Assert(firstRemovedPiece == null || lastRemovedPiece == null || 
		             DebugMarkChainDoesntReferenceRemovePieces(firstRemovedPiece, lastRemovedPiece), "Replace: Leave: Mark chain references removed piece");
		Debug.Assert(DebugMarkChainIsValid(), "Replace: Leave: Invalid mark chain");
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
				block.Prev = CurrentBlock;
				CurrentBlock = block;
			}
		}
		
		Replace(destStart, destEnd, head, tail, origLength);
	}

	public void Insert(Mark dest, byte[] text, long length)
	{
		Insert(dest, dest, text, length);
	}
	
	public void Insert(Mark destStart, Mark destEnd, byte c)
	{
		Insert(destStart, destEnd, new byte[] {c}, 1);
	}
	
	public void Insert(Mark dest, byte c)
	{
		Insert(dest, new byte[] {c}, 1);
	}
	
	public void Insert(byte[] text, long length)
	{
		Insert(InsertMark, text, length);
	}
	
	public void Insert(byte c)
	{
		Insert(InsertMark, new byte[] {c}, 1);
	}
	
	public void Insert(string text)
	{
		Insert(System.Text.Encoding.ASCII.GetBytes(text), text.Length);
	}
	
	public void Insert(Mark destStart, Mark destEnd, string text)
	{
		Insert(destStart, destEnd, System.Text.Encoding.ASCII.GetBytes(text), text.Length);
	}
	
	public void Insert(Mark dest, string text)
	{
		Insert(dest, System.Text.Encoding.ASCII.GetBytes(text), text.Length);
	}

	

	//
	// Remove
	//
	public void Remove(Mark start, Mark end)
	{
		Replace(start, end, null, null, 0);
	}
	
	public void Remove(long length)
	{
		Mark end = CreateMark(length); 
		Replace(InsertMark, end, null, null, 0); 
		DestroyMark(end);
	}
	
	public void Remove(Range range)
	{
		Replace(range.Start, range.End, null, null, 0);
	}
	
	public void Remove(long start, long end)
	{
		Mark s = CreateMark(start - InsertMark.Position);
		Mark e = CreateMark(end - InsertMark.Position);
		Replace(s, e, null, null, 0);
		DestroyMark(s);
		DestroyMark(e);
	}
	
	
	
	//
	// Copy
	//	
	public void Copy(Mark destStart, Mark destEnd, Mark srcStart, Mark srcEnd)
	{
		
	}
	
	public void Copy(Mark start, Mark end)
	{
		Copy(InsertMark, InsertMark, start, end);
	}
	
	public void Copy(Mark dest, Range range)
	{
		Copy(dest, dest, range.Start, range.End);
	}
	
	public void Copy(Range range)
	{
		Copy(InsertMark, InsertMark, range.Start, range.End);
	}
	
	public void Copy(Mark dest, long start, long end)
	{
		Mark s = CreateMark(start);
		Mark e = CreateMark(end);
		Copy(dest, dest, s, e);
		DestroyMark(s);
		DestroyMark(e);
	}
	
	public void Copy(long start, long end)
	{
		Copy(InsertMark, start, end);
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
		Mark s = CreateMark(start);
		Mark e = CreateMark(end);
		BeginHistoryGroup();
		Copy(dest, dest, s, e);
		Remove(s, e);
		EndHistoryGroup();
		DestroyMark(s);
		DestroyMark(e);
	}
	
	public void Move(Mark start, Mark end)
	{
		BeginHistoryGroup();
		Copy(InsertMark, InsertMark, start, end);
		Remove(start, end);
		EndHistoryGroup();
	}
	
	public void Move(Range range)
	{
		BeginHistoryGroup();
		Copy(InsertMark, InsertMark, range.Start, range.End);
		Remove(range.Start, range.End);
		EndHistoryGroup();
	}
	
	public void Move(long start, long end)
	{
		Move(InsertMark, start, end);
	}
	
	
	
	
	public void BeginHistoryGroup() { ++HistoryGroupLevel; }
	public void EndHistoryGroup() { --HistoryGroupLevel; }
	
	public void AddHistory(Piece start, Piece end)
	{
		HistoryItem i = new HistoryItem(start, end, HistoryGroupLevel);
		
		History.FirstChild = i;
		i.Parent = History;
		History = i;
		
		DebugDumpHistory("");
	}
	
	public void UndoRedo()
	{
		Debug.Assert(DebugMarkChainIsValid(), "Undo: Enter: Invalid mark chain");

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
			Mark editStartMark;
			Mark editEndMark;
			
			// Find the position of the change and create a mark there
			p = Pieces;
			while((p = p.Next) != Pieces && p != removeHead)
				editPosition += p.End - p.Start;
			editStartMark = CreateMarkAbsolute(editPosition);
			
			do
			{
				editPosition += p.End - p.Start;
				p = p.Next;
				
			} while(p != removeTail);
			editEndMark = CreateMarkAbsolute(editPosition);

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
			
			// Make sure the mark is the left most mark with the same position
			// as the edit mark.
			Mark m = editStartMark;
			while(m.Position == m.Prev.Position)
				m = m.Prev;
			
			// Move all marks within the removed part to the first position following
			// the removed part
			while(m != editEndMark)
			{
				m.Offset = editEndMark.Offset;
				m.Piece = editEndMark.Piece;
				m.Position = editEndMark.Position + lengthChange;
				m = m.Next;
			}
			
			// Update the positions of all following marks.
			if(lengthChange != 0)
			{
				while(m != Marks)
				{
					m.Position += lengthChange;
					m = m.Next;
				}
			}
	
			if(StartMark.Position != 0)
			{		
				Mark.ListRemove(StartMark);
				StartMark.Piece = Pieces.Next;
				StartMark.Offset = 0;
				StartMark.Position = 0;
				Mark.ListInsert(Marks, StartMark);
			}
			
			DestroyMark(editStartMark);
			DestroyMark(editEndMark);
			
			OnChanged(change);
		}

		DebugDumpHistory("");
		Debug.Assert(DebugMarkChainIsValid(), "Undo: Leave: Invalid mark chain");
	}
	
	public void Undo()
	{
		if(History.Parent != null)
		{
			UndoRedo();
			History = History.Parent;
		}
	}
	
	public void Redo()
	{
		if(History.FirstChild != null)
		{
			HistoryItem i = History.FirstChild;
			while(i.NextSibling != null)
				i = i.NextSibling;
			History = i;
			UndoRedo();
		}
	}
	
	// TODO: Why does this take a length as well as start/end?
	public void GetBytes(Mark start, Mark end, byte[] dest, long length)
	{
		if(start.Piece == end.Piece)
		{
			if(length > end.Position - start.Position)
				length = end.Position - start.Position;
				
			if(start.Piece != Pieces)
				start.Piece.Block.GetBytes(start.Piece.Start + start.Offset, length, dest, 0);
		}
		else
		{
			long destOffset = 0;
			long len = start.Piece.End - start.Piece.Start - start.Offset;
			if(len > length)
				len = length;
			start.Piece.Block.GetBytes(start.Piece.Start + start.Offset, len, dest, destOffset);
			destOffset += len;
			length -= len;
			
			Piece p = start.Piece;
			while(length > 0 && (p = p.Next) != end.Piece)
			{
				len = p.End - p.Start;
				if(len > length)
					len = length;
				p.Block.GetBytes(p.Start, len, dest, destOffset);
				destOffset += len;
				length -= len;
			}
				
			if(length > 0 && p != Pieces)
				p.Block.GetBytes(p.Start, end.Offset > length ? length : end.Offset, dest, destOffset);
		}
	}
	
	public void GetBytes(long offset, byte[] dest, long length)
	{
		Mark start = CreateMarkAbsolute(offset);
		Mark end = CreateMarkAbsolute(offset + length);
		GetBytes(start, end, dest, length);
		DestroyMark(start);
		DestroyMark(end);
	}
	
	public bool DebugMarkChainIsValid()
	{
		Mark m = Marks;
		
		// List sentinal's values must never change
		Debug.Assert(m.Piece == null, "Mark chain sentinel's piece is not null");
		Debug.Assert(m.Offset == Int64.MaxValue, "Mark chain sentinel's offset wrong");
		Debug.Assert(m.Position == Int64.MaxValue, "Mark chain sentinel's position wrong");
		
		// Sentinal links must be valid
		Debug.Assert(m.Next.Prev == m, "Mark chain sentinel's next pointer is bad");
		Debug.Assert(m.Prev.Next == m, "Mark chain sentinel's prev pointer is bad");
		
		// List must never be empty, insert_mark always exists
		Debug.Assert(m.Prev != Marks, "Mark chain is empty");
		
		while((m = m.Prev) != Marks)
		{
			// List links must be valid
			Debug.Assert(m.Next.Prev == m, "Mark chain node's next pointer is bad");
			Debug.Assert(m.Prev.Next == m, "Mark chain node's prev pointer is bad");
		
			// Each mark should be in sorted order
			Debug.Assert(m.Position <= m.Next.Position, "Mark chain node's position is out of order");
			
			if(m.Piece == m.Next.Piece)
			{
				// Each mark that shares the same piece must have their
				// offsets in sorted order
				Debug.Assert(m.Offset <= m.Next.Offset, "Mark chain node's offset is out of order");
				
				// Each mark that shares the same piece must have the
				// same distance between offsets as positions
				Debug.Assert(m.Next.Offset - m.Offset == m.Next.Position - m.Position, "Mark chain node's position/offset don't match");
			}
		}
		
		return true;
	}
	
	public bool DebugMarkChainDoesntReferenceRemovePieces(Piece removedStart, Piece removedEnd)
	{
		Piece p = removedStart;
		while(true)
		{
			Mark m = Marks;
			while((m = m.Next) != Marks)
				Debug.Assert(m.Piece != p, "Mark references removed piece");
			
			if(p == removedEnd)
				break;
			p = p.Next;
		}
		
		return true;
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
			}
			else
				Console.Write("HistoryHead\n");
				
			DebugDumpHistory(item.FirstChild, indent + 1);
			item = item.NextSibling;
		}
	}
	public void DebugDumpHistory(string msg)
	{
		Console.WriteLine("\n" + msg + "\n========\n");
	
		HistoryItem i = History;
		while(i.Parent != null)
			i = i.Parent;
		DebugDumpHistory(i, 1);
	}
}
