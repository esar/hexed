/*
	This file is part of HexEd

	Copyright (C) 2008-2015  Stephen Robinson <hacks@esar.org.uk>

	HexEd is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License version 2 as 
	published by the Free Software Foundation.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

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
	
	public class HistoryItem
	{
		protected bool _Active;
		public bool Active { get { return _Active; } }
		
		protected DateTime _Date;
		public DateTime Date { get { return _Date; } }
		
		protected string _Operation;
		public string Operation { get { return _Operation; } }
		
		protected long _StartPosition;
		public long StartPosition { get { return _StartPosition; } }
		
		protected long _OldLength;
		public long OldLength { get { return _OldLength; } }
		
		protected long _NewLength;
		public long NewLength { get { return _NewLength; } }
		
		protected long _DocumentLength;
		public long DocumentLength { get { return _DocumentLength; } }

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
		public new string Operation 
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
		public new long DocumentLength
		{
			get { return _DocumentLength; }
			set { _DocumentLength = value; }
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

		protected HistoryTree _GroupHistory;
		public HistoryTree GroupHistory
		{
			get { return _GroupHistory; }
			set { _GroupHistory = value; }
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

		protected HistoryTree _Tree;
		public HistoryTree Tree
		{
			get { return _Tree; }
			set { _Tree = value; }
		}

		public InternalHistoryItem(DateTime date, string op, 
		                           long startPosition, long oldLength, long newLength, long docLength,
		                           long pieceStartPos, long pieceOldLength, long pieceNewLength,
		                           Piece head, Piece tail)
		{
			Active = true;
			Date = date;
			Operation = op;
			StartPosition = startPosition;
			OldLength = oldLength;
			NewLength = newLength;
			DocumentLength = docLength;
			Head = head;
			Tail = tail;
			PieceStartPosition = pieceStartPos;
			PieceOldLength = pieceOldLength;
			PieceNewLength = pieceNewLength;
		}
	}

	protected class HistoryTree
	{
		protected InternalHistoryItem _Root;
		protected InternalHistoryItem _Current;
		protected bool                _Active;
		                                              
		public InternalHistoryItem Root    { get { return _Root; } }
		public InternalHistoryItem Current { get { return _Current; } set { _Current = value; } }
		public bool                IsEmpty { get { return _Root == null; } }
		public bool                Active  { get { return _Active; } set { _Active = value; } }

		public HistoryTree()
		{
			_Root = _Current = null;
			_Active = true;
		}

		public void Add(InternalHistoryItem item)
		{
			if(_Root == null)
				_Root = item;

			if(_Current != null)
			{
				item.NextSibling = _Current.FirstChild;
				_Current.FirstChild = item;
			}
			else
				item.NextSibling = null;

			item.FirstChild = null;
			item.Parent = _Current;
			_Current = item;

			item.Tree = this;
		}
	}

	public HistoryItem BeginHistoryGroup(string operation) 
	{
		InternalHistoryItem item = AddHistory(operation, 0, 0, 0, 0, 0, 0, null, null);
		item.GroupHistory = new HistoryTree();
		AddHistory("New", 0, 0, 0, 0, 0, 0, null, null);
		return item;
	}

	public void EndHistoryGroup(HistoryItem group) 
	{
		InternalHistoryItem item = (InternalHistoryItem)group;

		// If no actual actions happened within this group
		// then delete it and pretend it never happened.
		if(item.GroupHistory.Current == item.GroupHistory.Root)
		{
			if(item.InternalFirstChild != null)
				item.InternalFirstChild.Parent = item.Parent;
			if(item.InternalParent != null)
				item.InternalParent.FirstChild = item.FirstChild;
			if(item.Tree.Current == item)
				item.Tree.Current = item.InternalParent;

			if(HistoryRemoved != null)
				HistoryRemoved(this, new HistoryEventArgs(item, null));
		}

		item.GroupHistory.Active = false;
	}

	protected InternalHistoryItem AddHistory(string operation, long startPosition, long oldLength, long newLength, 
	                                         long pieceStartPos, long pieceOldLength, long pieceNewLength, Piece start, Piece end)
	{
		InternalHistoryItem oldItem = _History.Current;

		HistoryTree history = _History;
		while(history.Current != null && history.Current.GroupHistory != null && history.Current.GroupHistory.Active)
			history = history.Current.GroupHistory;

		InternalHistoryItem newItem = new InternalHistoryItem(DateTime.Now, operation, startPosition, 
		                                                      oldLength, newLength, Length,
		                                                      pieceStartPos, pieceOldLength, pieceNewLength,
		                                                      start, end);

		history.Add(newItem);
		
		if(HistoryAdded != null && oldItem != _History.Current)
			HistoryAdded(this, new HistoryEventArgs(oldItem, _History.Current));

		DebugDumpHistory(String.Empty);

		return newItem;
	}

	protected void UndoRedo(HistoryTree history, 
	                        long editStartPos, long editEndPos, long oldLength, long newLength,
	                        long changeStartPos, long changeEndPos)
	{
		Debug.Assert(Marks.DebugMarkChainIsValid(), "Undo: Enter: Invalid mark chain");

		if(history.Current != null)
		{
			Piece removeHead = history.Current.Head.Prev.Next;
			Piece removeTail = history.Current.Tail.Next.Prev;
			Piece insertHead = history.Current.Head;
			Piece insertTail = history.Current.Tail;
			Piece insertAfter = history.Current.Head.Prev;
			Mark editStartMark = Marks.Add(editStartPos);
			Mark editEndMark = Marks.Add(editEndPos);

			if(removeHead != insertTail.Next && removeTail != insertHead.Prev)
			{
				history.Current.Head = removeHead;
				history.Current.Tail = removeTail;
				Piece.ListRemoveRange(removeHead, removeTail);
			}
			else
			{
				Piece empty = new Piece(AllocatedPieces);
				empty.Prev = insertAfter;
				empty.Next = insertAfter.Next;
				history.Current.Head = empty;
				history.Current.Tail = empty;
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

	protected void Undo(HistoryTree history)
	{
		if(history.Current.GroupHistory == null)
		{
			long changeEndPos;
			if(history.Current.OldLength == history.Current.NewLength)
				changeEndPos = history.Current.StartPosition + history.Current.OldLength;
			else
				changeEndPos = Length;

			UndoRedo(history, 
			         history.Current.PieceStartPosition,
			         history.Current.PieceStartPosition + history.Current.PieceNewLength,
			         history.Current.NewLength, history.Current.OldLength,
			         history.Current.StartPosition, changeEndPos);
		}
		else
		{
			while(history.Current.GroupHistory.Current != history.Current.GroupHistory.Root)
				Undo(history.Current.GroupHistory);
		}

		history.Current.Active = false;
		history.Current = history.Current.InternalParent;
	}

	public void Undo()
	{
		lock(Lock)
		{
			if(_History.Current != _History.Root)
			{
				InternalHistoryItem oldItem = _History.Current;

				Undo(_History);
				
				if(HistoryUndone != null && oldItem != _History.Current)
					HistoryUndone(this, new HistoryEventArgs(oldItem, _History.Current));
			}
		}
	}

	protected void Redo(HistoryTree history)
	{
		history.Current = history.Current.InternalFirstChild;
		history.Current.Active = true;

		if(history.Current.GroupHistory == null)
		{
			long changeEndPos;
			if(history.Current.OldLength == history.Current.NewLength)
				changeEndPos = history.Current.StartPosition + history.Current.OldLength;
			else
				changeEndPos = Length;

			UndoRedo(history,
			         history.Current.PieceStartPosition,
			         history.Current.PieceStartPosition + history.Current.PieceOldLength,
			         history.Current.OldLength, history.Current.NewLength,
			         history.Current.StartPosition, changeEndPos);
		}
		else
		{
			while(history.Current.GroupHistory.Current.FirstChild != null)
				Redo(history.Current.GroupHistory);
		}
	}

	public void Redo()
	{
		lock(Lock)
		{
			if(History.FirstChild != null)
			{
				InternalHistoryItem oldItem = _History.Current;

				Redo(_History);

				if(HistoryRedone != null && oldItem != _History.Current)
					HistoryRedone(this, new HistoryEventArgs(oldItem, _History.Current));
			}
		}
	}
	
	public void HistoryJump(HistoryItem destination)
	{
		lock(Lock)
		{
			InternalHistoryItem oldItem = _History.Current;
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
			while(_History.Current != commonParent)
				Undo(_History);
						
			// Redo to the destination
			while(_History.Current != destination)
			{
				InternalHistoryItem next = redoPath.Pop();
				InternalHistoryItem prev = _History.Current.InternalFirstChild;
				
				item = _History.Current.InternalFirstChild;
				while(item != next)
				{
					prev = item;
					item = item.InternalNextSibling;
				}

				HistoryItem tmp = next.NextSibling;
				next.NextSibling = _History.Current.FirstChild;
				_History.Current.FirstChild = next;
				prev.NextSibling = tmp;
				
				Redo(_History);
			}
			
			if(HistoryJumped != null)
				HistoryJumped(this, new HistoryEventArgs(oldItem, destination));
		}
	}
}

