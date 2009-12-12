using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;


public partial class PieceBuffer : IDisposable
{
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
		
		protected long _OldLength;
		public long OldLength { get { return _OldLength; } }
		
		protected long _NewLength;
		public long NewLength { get { return _NewLength; } }
		
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
		public new long OldLength 
		{
			get { return _OldLength; }
			set { _OldLength = value; } 
		}
		public new long NewLength 
		{
			get { return _NewLength; }
			set { _NewLength = value; } 
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

		protected long _PieceStartPosition;
		public long PieceStartPosition
		{
			get { return _PieceStartPosition; }
			set { _PieceStartPosition = value; }
		}

		protected long _PieceOldLength;
		public long PieceOldLength
		{
			get { return _PieceOldLength; }
			set { _PieceOldLength = value; }
		}

		protected long _PieceNewLength;
		public long PieceNewLength
		{
			get { return _PieceNewLength; }
			set { _PieceNewLength = value; }
		}

		public InternalHistoryItem(DateTime date, HistoryOperation op, 
		                           long startPosition, long oldLength, long newLength, 
		                           long pieceStartPos, long pieceOldLength, long pieceNewLength,
		                           Piece head, Piece tail, int groupLevel)
		{
			Active = true;
			Date = date;
			Operation = op;
			StartPosition = startPosition;
			OldLength = oldLength;
			NewLength = newLength;
			Head = head;
			Tail = tail;
			GroupLevel = groupLevel;
			PieceStartPosition = pieceStartPos;
			PieceOldLength = pieceOldLength;
			PieceNewLength = pieceNewLength;
		}
	}

	protected void BeginHistoryGroup() { ++HistoryGroupLevel; }
	protected void EndHistoryGroup() { --HistoryGroupLevel; }

	protected void AddHistory(HistoryOperation operation, long startPosition, long oldLength, long newLength, 
	                          long pieceStartPos, long pieceOldLength, long pieceNewLength, Piece start, Piece end)
	{
		InternalHistoryItem oldItem = _History;
		InternalHistoryItem newItem = new InternalHistoryItem(DateTime.Now, operation, startPosition, 
		                                                      oldLength, newLength, 
		                                                      pieceStartPos, pieceOldLength, pieceNewLength,
		                                                      start, end, HistoryGroupLevel);

		newItem.NextSibling = History.FirstChild;
		_History.FirstChild = newItem;
		newItem.Parent = History;
		_History = newItem;
		
		if(HistoryAdded != null)
			HistoryAdded(this, new HistoryEventArgs(oldItem, newItem));

		DebugDumpHistory(String.Empty);
	}

	protected void UndoRedo(long editStartPos, long editEndPos, long oldLength, long newLength,
	                        long changeStartPos, long changeEndPos)
	{
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Undo: Enter: Invalid mark chain");

		if(History != null)
		{
			Piece removeHead = _History.Head.Prev.Next;
			Piece removeTail = _History.Tail.Next.Prev;
			Piece insertHead = _History.Head;
			Piece insertTail = _History.Tail;
			Piece insertAfter = _History.Head.Prev;
			Mark editStartMark = Marks.Add(editStartPos);
			Mark editEndMark = Marks.Add(editEndPos);

			if(removeHead != insertTail.Next && removeTail != insertHead.Prev)
			{
				_History.Head = removeHead;
				_History.Tail = removeTail;
				Piece.ListRemoveRange(removeHead, removeTail);
			}
			else
			{
				Piece empty = new Piece(AllocatedPieces);
				empty.Prev = insertAfter;
				empty.Next = insertAfter.Next;
				_History.Head = empty;
				_History.Tail = empty;
			}

			if(insertHead != insertTail)
				Piece.ListInsertRange(insertAfter, insertHead, insertTail);
			else if(insertHead.Block != null)
					Piece.ListInsert(insertAfter, insertHead);

			_Marks.UpdateAfterReplace(editStartMark, editEndMark, 0, newLength - oldLength, null);
			_Marks.Remove(editStartMark);
			_Marks.Remove(editEndMark);

			BufferChangedEventArgs change = new BufferChangedEventArgs(changeStartPos, 
										   changeEndPos);
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
				long changeEndPos;
				if(_History.OldLength == _History.NewLength)
					changeEndPos = _History.StartPosition + _History.OldLength;
				else
					changeEndPos = Length;

				UndoRedo(_History.PieceStartPosition, 
				         _History.PieceStartPosition + _History.PieceNewLength, 
				         _History.NewLength, _History.OldLength,
				         _History.StartPosition, changeEndPos);
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

				long changeEndPos;
				if(_History.OldLength == _History.NewLength)
					changeEndPos = _History.StartPosition + _History.OldLength;
				else
					changeEndPos = Length;

				UndoRedo(_History.PieceStartPosition, 
				         _History.PieceStartPosition + _History.PieceOldLength,
				         _History.OldLength, _History.NewLength,
				         _History.StartPosition, changeEndPos);
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
				long changeEndPos;
				if(_History.OldLength == _History.NewLength)
					changeEndPos = _History.StartPosition + _History.OldLength;
				else
					changeEndPos = Length;

				UndoRedo(_History.PieceStartPosition, 
				         _History.PieceStartPosition + _History.PieceNewLength, 
				         _History.NewLength, _History.OldLength,
				         _History.StartPosition, changeEndPos);
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

				long changeEndPos;
				if(_History.OldLength == _History.NewLength)
					changeEndPos = _History.StartPosition + _History.OldLength;
				else
					changeEndPos = Length;

				UndoRedo(_History.PieceStartPosition, 
				         _History.PieceStartPosition + _History.PieceOldLength,
				         _History.OldLength, _History.NewLength,
				         _History.StartPosition, changeEndPos);
				_History.Active = true;
			}
			
			if(HistoryJumped != null)
				HistoryJumped(this, new HistoryEventArgs(oldItem, destination));
		}
	}
}

