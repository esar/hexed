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
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;


class TreeListViewChildItemCollection : ArrayList
{
	TreeListViewItem _owner;

	public TreeListViewChildItemCollection(TreeListViewItem owner)
	{
		_owner = owner;
	}

	public TreeListViewItem Add(string name)
	{
		if(_owner.CheckState == CheckState.Indeterminate)
			_owner.CheckState = CheckState.Unchecked;

		TreeListViewItem item = new TreeListViewItem(name);
		item.Parent = _owner;
		item.Indent = _owner.Indent + 1;
		base.Add(item);
		return item;
	}

	public TreeListViewItem Add(TreeListViewItem item)
	{
		if(_owner.CheckState == CheckState.Indeterminate)
			_owner.CheckState = CheckState.Unchecked;

		item.Indent = _owner.Indent + 1;
		base.Add(item);
		return item;
	}

	public override int Add(object o)
	{
		throw new Exception("Only TreeListViewItems can be added");
	}
}

class TreeListViewItem : ListViewItem
{
	TreeListViewChildItemCollection _ChildItems;
	CheckState _CheckState;
	int _Indent;
	TreeListViewItem _Parent;

	public TreeListViewItem(string name) : base(name)
	{
		_Indent = 0;
		_ChildItems = new TreeListViewChildItemCollection(this);
		_CheckState = CheckState.Indeterminate;
	}

	public TreeListViewChildItemCollection ChildItems
	{
		get		{ return _ChildItems; }
	}

	public CheckState CheckState
	{
		get		{ return _CheckState; }
		set		
		{ 
			_CheckState = value; 
			if(value == CheckState.Checked)
				ImageIndex = 2;
			else if(value == CheckState.Unchecked)
				ImageIndex = 1;
			else
				ImageIndex = 0;
		}
	}

	public int Indent
	{
		get		{ return _Indent; }
		set		{ _Indent = value; }
	}
	
	public TreeListViewItem Parent
	{
		get { return _Parent; }
		set { _Parent = value; }
	}
}

class TreeListViewItemCollection : ListView.ListViewItemCollection
{
	public TreeListViewItemCollection(TreeListView owner) : base(owner)
	{
	}

	public new TreeListViewItem Add(string name)
	{
		return (TreeListViewItem)base.Add(new TreeListViewItem(name));
	}
}

class TreeListView : ListView
{
	private TreeListViewItemCollection _Items;

	public TreeListView()
	{
		_Items = new TreeListViewItemCollection(this);

		System.Resources.ResourceManager resources = new System.Resources.ResourceManager("hexed.treelistview", System.Reflection.Assembly.GetExecutingAssembly());

		View = View.Details;
		
		SmallImageList = new ImageList();
		SmallImageList.ImageSize = new Size(16, 16);
		SmallImageList.Images.Add((Icon)resources.GetObject("treenone.ico"));
		SmallImageList.Images.Add((Icon)resources.GetObject("treeplus.ico"));
		SmallImageList.Images.Add((Icon)resources.GetObject("treeminus.ico"));
	}

	public new TreeListViewItemCollection Items
	{
		get		{ return _Items; }
	}


	protected override void OnDoubleClick(EventArgs e)
	{
		base.OnDoubleClick(e);

		TreeListViewItem item = (TreeListViewItem)SelectedItems[0];

		int itemIndent = GetIndent(item.Index);

		if(item.CheckState == CheckState.Unchecked)
			OnItemExpanding(item);
		else if(item.CheckState == CheckState.Checked)
			OnItemCollapsing(item);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		bool handled = false;

		TreeListViewItem item = (TreeListViewItem)GetItemAt(e.X, e.Y);
		if(item != null)
		{
			if(e.X <= item.Indent*16 + 16)
			{
				handled = true;

				if(e.X >= item.Indent * 16)
				{
					if(item.CheckState == CheckState.Unchecked)
						OnItemExpanding(item);
					else if(item.CheckState == CheckState.Checked)
						OnItemCollapsing(item);
				}
			}
		}

		if(!handled)
			base.OnMouseDown(e);
	}

	protected virtual void OnBeginTrack(TreeListViewHeaderNotifyEventArgs e)
	{
	}

	protected virtual void OnEndTrack(TreeListViewHeaderNotifyEventArgs e)
	{
	}

	protected virtual void OnBeginDrag(TreeListViewHeaderNotifyEventArgs e)
	{
	}

	protected virtual void OnEndDrag(TreeListViewHeaderNotifyEventArgs e)
	{
	}

	protected virtual void OnTrack(TreeListViewHeaderNotifyEventArgs e)
	{
	}


	protected virtual void OnItemExpanding(TreeListViewItem item)
	{
		int x = 1;
		foreach(TreeListViewItem child in item.ChildItems)
		{
			_Items.Insert(item.Index + x++, child);
			SetIndent(child.Index, child.Indent);
		}

		item.CheckState = CheckState.Checked;
	}

	protected virtual void OnItemCollapsing(TreeListViewItem item)
	{
		foreach(TreeListViewItem child in item.ChildItems)
		{
			if(child.CheckState == CheckState.Checked)
				OnItemCollapsing(child);
			_Items.Remove(child);
		}

		item.CheckState = CheckState.Unchecked;
	}


	protected override void WndProc(ref Message m)
	{ 
//		HDITEM hditem;
		NMHDR nm;
		NMHEADER nmheader;

		switch(m.Msg)
		{
			case WM_NOTIFY:
				nm = (NMHDR) m.GetLParam(typeof(NMHDR));
				switch(nm.code)
				{
					case HDN_BEGINTRACKA:
					case HDN_BEGINTRACKW:
						nmheader = (NMHEADER)m.GetLParam(typeof(NMHEADER));
						OnBeginTrack(new TreeListViewHeaderNotifyEventArgs(nmheader.iItem, nmheader.iButton));
						break;
					case HDN_BEGINDRAG:
						nmheader = (NMHEADER)m.GetLParam(typeof(NMHEADER));
						OnBeginDrag(new TreeListViewHeaderNotifyEventArgs(nmheader.iItem, nmheader.iButton));
						break;
					case HDN_ENDTRACKA:
					case HDN_ENDTRACKW:
						nmheader = (NMHEADER)m.GetLParam(typeof(NMHEADER));
						OnEndTrack(new TreeListViewHeaderNotifyEventArgs(nmheader.iItem, nmheader.iButton));
						break;
					case HDN_ENDDRAG:
						nmheader = (NMHEADER)m.GetLParam(typeof(NMHEADER));
						OnEndDrag(new TreeListViewHeaderNotifyEventArgs(nmheader.iItem, nmheader.iButton));
						break;
					case HDN_TRACKA:
					case HDN_TRACKW:
						nmheader = (NMHEADER)m.GetLParam(typeof(NMHEADER));
						OnTrack(new TreeListViewHeaderNotifyEventArgs(nmheader.iItem, nmheader.iButton));
						break;					
					default:
						break;
				}
				break;
		}

		base.WndProc( ref m ); 	
	}


	private int GetIndent(int index)
	{
		LV_ITEM item = new LV_ITEM();
		
		item.mask = LVIF_INDENT;
		item.iItem = (UInt32)index;

		IntPtr pItem = Marshal.AllocHGlobal(Marshal.SizeOf(item));
		Marshal.StructureToPtr(item, pItem, false);
		SendMessage(Handle, LVM_GETITEM, 0, pItem);
		Marshal.PtrToStructure(pItem, item);
		Marshal.FreeHGlobal(pItem);

		return (int)item.iIndent;
	}

	public void SetIndent(int index, int indent)
	{
		LV_ITEM item = new LV_ITEM();
		
		item.mask = LVIF_INDENT;
		item.iItem = (UInt32)index;
		item.iIndent = (UInt32)indent;

		IntPtr pItem = Marshal.AllocHGlobal(Marshal.SizeOf(item));
		Marshal.StructureToPtr(item, pItem, false);
		SendMessage(Handle, LVM_SETITEM, 0, pItem);
		Marshal.FreeHGlobal(pItem);
	}




	public class TreeListViewHeaderNotifyEventArgs : EventArgs
	{
		public int column;
		public int button;

		public TreeListViewHeaderNotifyEventArgs(int column, int button)
		{
			this.column = column;
			this.button = button;
		}
	} 

	[StructLayout(LayoutKind.Sequential)]
	public struct NMHDR
	{
		public IntPtr hwndFrom;
		public int idFrom;
		public int code;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct NMHEADER
	{
		public NMHDR hdr;
		public int   iItem;
		public int   iButton;
		public IntPtr pItem;
	}

	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
	public struct HDITEM
	{
		public uint    mask;
		public int     cxy;
		public IntPtr  pszText;
		public IntPtr  hbm;
		public int     cchTextMax;
		public int     fmt;
		public int     lParam;
		public int     iImage;
		public int     iOrder;
	}

	const int HDN_FIRST       = (0-300);
	const int HDN_BEGINTRACKA = (HDN_FIRST-6);
	const int HDN_ENDTRACKA   = (HDN_FIRST-7);
	const int HDN_BEGINTRACKW = (HDN_FIRST-26);
	const int HDN_ENDTRACKW   = (HDN_FIRST-27);
	const int HDN_ITEMCLICKW  = (HDN_FIRST-22);
	const int HDN_BEGINDRAG   = (HDN_FIRST-10);
	const int HDN_ENDDRAG     = (HDN_FIRST-11);
	const int HDN_TRACKA      = (HDN_FIRST-8);
	const int HDN_TRACKW      = (HDN_FIRST-28);


	[StructLayout(LayoutKind.Sequential)]
	class LV_ITEM
	{
		public UInt32 mask;
		public UInt32 iItem;
	    public UInt32 iSubItem;
		public UInt32 state;
	    public UInt32 stateMask;
		[MarshalAs(UnmanagedType.LPStr)]
		public string pszText;
	    public UInt32 cchTextMax;
		public UInt32 iImage;
	    public UInt32 lParam;
		public UInt32 iIndent;
	}

	const int WM_NOTIFY = 0x004E;

	const int LVM_GETITEM = 0x1005;
	const int LVM_SETITEM = 0x1006;
	const int LVIF_INDENT = 0x10;
	[DllImport("user32.dll")]
	static extern bool SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, IntPtr lParam);
}
