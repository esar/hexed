using System;
using System.Diagnostics;



public partial class PieceBuffer
{
	public class Mark
	{
		public event EventHandler Changed;
		
		private MarkCollection _Owner;
		protected MarkCollection Owner
		{
			get { return _Owner; }
			set { _Owner = value; }
		}
		
		protected long _Position;
		public long Position
		{
			get { return _Position; }
			set { _Position = Owner.Move(this, value); }
		}
		
		protected Mark(MarkCollection owner)
		{
			_Owner = owner;
		}
		
		public void Remove()
		{
			_Owner.Remove(this);
		}
		
		protected void OnChanged()
		{
			if(Changed != null)
				Changed(this, EventArgs.Empty);
		}
	}
	
	protected class InternalMark : Mark
	{	
		public new long Position
		{
			get { return _Position; }
			set { _Position = value; OnChanged(); }
		}
		
		private Piece _Piece;
		public Piece Piece
		{
			get { return _Piece; }
			set { _Piece = value; }
		}
		
		private long _Offset;
		public long Offset
		{
			get { return _Offset; }
			set { _Offset = value; }
		}
		
		private InternalMark _Prev;
		public InternalMark Prev
		{
			get { return _Prev; }
			set { _Prev = value; }
		}
		
		private InternalMark _Next;
		public InternalMark Next
		{
			get { return _Next; }
			set { _Next = value; }
		}
		
		public InternalMark(MarkCollection owner) : base(owner)
		{
			_Piece = null;
			_Offset = Int64.MaxValue;
			_Position = Int64.MaxValue;
			_Next = this;
			_Prev = this;
		}
		
		public InternalMark(MarkCollection owner, Piece piece, long offset, long position) : base(owner)
		{
			_Piece = piece;
			_Offset = offset;
			_Position = position;
			_Next = this;
			_Prev = this;
		}		

		public void CopyTo(InternalMark dest)
		{
			dest.Next = Next;
			dest.Prev = Prev;
			dest.Piece = Piece;
			dest.Offset = Offset;
			dest.Position = Position;
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
	
	public class MarkCollection
	{
		private PieceBuffer		Buffer;
		private InternalMark	_Sentinel;
		public Mark		Sentinel
		{
			get { return _Sentinel; }
		}
		private Piece			Pieces;
		private InternalMark	_Start;
		public Mark	Start
		{
			get { return _Start; }
		}
		private InternalMark	_Insert;
		public Mark	Insert
		{
			get { return _Insert; }
		}
		private InternalMark	_End;
		public Mark	End
		{
			get { return _End; }
		}
		
		public MarkCollection(PieceBuffer buffer, Piece pieces, Piece firstPiece, long endPosition)
		{
			Buffer = buffer;
			Pieces = pieces;
			_Sentinel = new InternalMark(this);
			
			_End = new InternalMark(this, pieces, 0, endPosition);
			ListInsert(_Sentinel, _End);
			_Insert = new InternalMark(this, firstPiece, 0, 0);
			ListInsert(_Sentinel, _Insert);
			_Start = new InternalMark(this, firstPiece, 0, 0);
			ListInsert(_Sentinel, _Start);			
		}
		
		public Mark Add()
		{
			Debug.Assert(DebugMarkChainIsValid(), "CreateMark: Enter: Invalid mark chain");
			
			InternalMark mark = new InternalMark(this);
			_Insert.CopyTo(mark);
			ListInsert(_Insert, mark);

			Debug.Assert(DebugMarkChainIsValid(), "CreateMark: Leave: Invalid mark chain");
			return mark;
		}
		
		public Mark Add(long position)
		{
			Mark mark = Add();
			Move(mark, position);
			return mark;
		}
		
		public Range AddRange()
		{
			return new Range(Add(), Add());
		}
		
		public Range AddRange(long start, long end)
		{
			return new Range(Add(start), Add(end));
		}
		
		public void Remove(Mark mark)
		{
			if(mark == _Start || mark == _Insert || mark == _End)
				return;
			
			ListRemove((InternalMark)mark);
			
			Debug.Assert(DebugMarkChainIsValid(), "DestroyMark: Invalid mark chain");
		}
		
		public long Move(Mark mark, long distance)
		{
			if(distance - mark.Position < 0)
				return MoveMarkLeft((InternalMark)mark, mark.Position - distance);
			else
				return MoveMarkRight((InternalMark)mark, distance - mark.Position);
		}
		
		private long MoveMarkRight(InternalMark mark, long distance)
		{
			Debug.Assert(DebugMarkChainIsValid(), "MoveMarkRight: Enter: Invalid mark chain");
			
			// If the desired position isn't contained in the current piece or the next
			// mark is before the desired position
			if(mark.Offset + distance >= mark.Piece.End - mark.Piece.Start ||
			   mark.Next.Position < mark.Position + distance)
			{
				// Find nearest mark before the destination position and move the
				// specified mark to that position (must be to the right in mark chain)
				InternalMark m = mark;
				while((m = m.Next) != _Sentinel && m.Position <= mark.Position + distance)
					;
				if(m.Prev != mark)
				{
					m = m.Prev;
			
					distance -= m.Position - mark.Position;
					
					ListRemove(mark);
					m.CopyTo(mark);
					ListInsert(m, mark);
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
						return mark.Position;
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
			
			return mark.Position;
		}
		
		private long MoveMarkLeft(InternalMark mark, long distance)
		{
			Debug.Assert(DebugMarkChainIsValid(), "MoveMarkLeft: Enter: Invalid mark chain");
			
			if(distance > mark.Position)
				distance = mark.Position;
			
			if(	distance - mark.Offset > 0 ||
			    mark.Prev.Position >= mark.Position - distance ||
				mark.Prev == _Sentinel)
			{
				// Find nearest mark after the destination position and move the
				// specified mark to that position (must be to left in mark chain)
				InternalMark m = mark;
				while((m = m.Prev) != _Sentinel && m.Position >= mark.Position - distance)
					;
				if(m.Next != mark)
				{
					distance -= mark.Position - m.Next.Position;
					ListRemove(mark);
					m.Next.CopyTo(mark);
					ListInsert(m, mark);
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
						return mark.Position;
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
			return mark.Position;
		}
		
		public void UpdateAfterReplace(Mark start, Mark end, long removedLength, long insertedLength, Piece firstInsertedPiece)
		{
			InternalMark curStart = (InternalMark)start;
			InternalMark curEnd = (InternalMark)end;
			
			// Remove/insert is done so now we need to update the marks.
			// First, find the left-most mark with the same position as cur_end.
			InternalMark m = curEnd;
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
						curStart.Position = m.Position + insertedLength - removedLength;
						//curStart.Modified = true;
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
						//curStart.Modified = true;
						curStart = curStart.Next;
					}
				}
			}
			
			// Flag first mark after insert/remove as modified
			//m.Modified = true;
			
			// Update the position of all marks following the removed/inserted
			// pieces if the change is non zero
			if(insertedLength - removedLength != 0)
			{
				do
				{
					m.Position += insertedLength - removedLength;
					
				} while((m = m.Next) != _Sentinel);
			}
			
			// Finally, position the insert mark immediately 
			// after the removed/inserted pieces (curEnd)
			// and make sure the start mark is still at the start
			if(_Insert != curEnd)
			{
				ListRemove(_Insert);
				curEnd.CopyTo(_Insert);
				ListInsert(curEnd, _Insert);
			
			}
			if(_Start.Position != 0)
			{		
				ListRemove(_Start);
				_Start.Piece = Pieces.Next;
				_Start.Offset = 0;
				_Start.Position = 0;
				ListInsert(_Sentinel, _Start);
			}
		}
		
		public override string ToString()
		{
			System.Text.StringBuilder tmp = new System.Text.StringBuilder();
			
			InternalMark mark = _Sentinel;
			while((mark = mark.Next) != _Sentinel)
				tmp.AppendFormat("{{{0},{1}}}", mark.Offset, mark.Position);
			
			return tmp.ToString();		
		}
		
		private static void ListInsert(InternalMark list, InternalMark item)
		{
			item.Next = list.Next;
			item.Prev = list;
			list.Next.Prev = item;
			list.Next = item;
		}

		private static void ListInsertRange(InternalMark list, InternalMark first, InternalMark last)
		{
			last.Next = list.Next;
			first.Prev = list;
			last.Next.Prev = last;
			first.Prev.Next = first;
		}

		private static void ListRemove(InternalMark item)
		{
			item.Prev.Next = item.Next;
			item.Next.Prev = item.Prev;
		}
												
		private static void ListRemoveRange(InternalMark first, InternalMark last)
		{
			first.Prev.Next = last.Next;
			last.Next.Prev = first.Prev;
		}		

		public bool DebugMarkChainIsValid()
		{
			InternalMark m = _Sentinel;
			
			// List sentinal's values must never change
			Debug.Assert(m.Piece == null, "Mark chain sentinel's piece is not null");
			Debug.Assert(m.Offset == Int64.MaxValue, "Mark chain sentinel's offset wrong");
			Debug.Assert(m.Position == Int64.MaxValue, "Mark chain sentinel's position wrong");
			
			// Sentinal links must be valid
			Debug.Assert(m.Next.Prev == m, "Mark chain sentinel's next pointer is bad");
			Debug.Assert(m.Prev.Next == m, "Mark chain sentinel's prev pointer is bad");
			
			// List must never be empty, insert_mark always exists
			Debug.Assert(m.Prev != _Sentinel, "Mark chain is empty");
			
			while((m = m.Prev) != _Sentinel)
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
				InternalMark m = _Sentinel;
				while((m = m.Next) != _Sentinel)
					Debug.Assert(m.Piece != p, "Mark references removed piece");
				
				if(p == removedEnd)
					break;
				p = p.Next;
			}
			
			return true;
		}
	}
}