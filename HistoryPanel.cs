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
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;


class HistoryPanel : Panel, Aga.Controls.Tree.ITreeModel
{
	private Aga.Controls.Tree.TreeViewAdv _TreeView;
	private Aga.Controls.Tree.NodeControls.NodeStateIcon _NodeControlIcon;
	private Aga.Controls.Tree.NodeControls.NodeTextBox _NodeControlName;
	private Aga.Controls.Tree.NodeControls.NodeTextBox _NodeControlDate;
	private Aga.Controls.Tree.NodeControls.NodeTextBox _NodeControlRange;
	private Aga.Controls.Tree.TreeColumn _TreeColumnName;
	private Aga.Controls.Tree.TreeColumn _TreeColumnDate;
	private Aga.Controls.Tree.TreeColumn _TreeColumnRange;

	private DocumentRangeIndicator RangeIndicator;
	private Dictionary<string, Image> OperationIcons;
	private Image UnknownOperationIcon;
	
	private Font _BoldFont;

	protected IPluginHost Host;
	protected Document LastDocument;
	
	
	public HistoryPanel(IPluginHost host)
	{
		Host = host;
		
		_TreeView = new Aga.Controls.Tree.TreeViewAdv();
		_TreeColumnName = new Aga.Controls.Tree.TreeColumn("Name", 100);
		_TreeColumnDate = new Aga.Controls.Tree.TreeColumn("Date", 100);
		_TreeColumnRange = new Aga.Controls.Tree.TreeColumn("Range", 100);
		_NodeControlIcon = new Aga.Controls.Tree.NodeControls.NodeStateIcon();
		_NodeControlName = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlName.ValueNeeded += OnNameValueNeeded;
		_NodeControlDate = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlRange = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlRange.ValueNeeded += OnRangeValueNeeded;
		_NodeControlName.DrawText += OnDrawNodeText;
		_NodeControlIcon.ValueNeeded += OnIconValueNeeded;

		_BoldFont = new Font(_NodeControlName.Font, FontStyle.Bold);
	
		_TreeView.AllowColumnReorder = true;
		_TreeView.AutoRowHeight = true;
		_TreeView.Columns.Add(_TreeColumnName);
		_TreeView.Columns.Add(_TreeColumnDate);
		_TreeView.Columns.Add(_TreeColumnRange);
		_TreeView.Cursor = System.Windows.Forms.Cursors.Default;
		_TreeView.FullRowSelect = true;
		_TreeView.UseColumns = true;
		_TreeView.GridLineStyle = ((Aga.Controls.Tree.GridLineStyle)((Aga.Controls.Tree.GridLineStyle.Horizontal | Aga.Controls.Tree.GridLineStyle.Vertical)));
		_TreeView.LineColor = System.Drawing.SystemColors.ControlDark;
		_TreeView.LoadOnDemand = true;
		_TreeView.Model = this;
		_TreeView.Name = "_TreeView";
		_TreeView.NodeControls.Add(this._NodeControlIcon);
		_TreeView.NodeControls.Add(this._NodeControlName);
		_TreeView.NodeControls.Add(this._NodeControlDate);
		_TreeView.NodeControls.Add(this._NodeControlRange);
		_TreeView.ShowNodeToolTips = true;
		_TreeView.NodeMouseDoubleClick += OnNodeDoubleClick;
		_TreeView.SelectionChanged += OnSelectionChanged;
		
		_NodeControlIcon.ParentColumn = _TreeColumnName;
		_NodeControlIcon.VirtualMode = true;
		
		_NodeControlName.VirtualMode = true;
		_NodeControlName.ParentColumn = _TreeColumnName;
		_NodeControlName.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlName.UseCompatibleTextRendering = true;
		
		_NodeControlDate.DataPropertyName = "Date";
		_NodeControlDate.ParentColumn = _TreeColumnDate;
		_NodeControlDate.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlDate.UseCompatibleTextRendering = true;
		
		_NodeControlRange.VirtualMode = true;
		_NodeControlRange.ParentColumn = _TreeColumnRange;
		_NodeControlRange.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlRange.UseCompatibleTextRendering = true;
		
		_TreeView.Dock = DockStyle.Fill;
		Controls.Add(_TreeView);

		RangeIndicator = new DocumentRangeIndicator();
		RangeIndicator.Dock = DockStyle.Left;
		Controls.Add(RangeIndicator);
		
		OperationIcons = new Dictionary<string, Image>();
		UnknownOperationIcon = Settings.Instance.Image("icons.unknown_op.png");
		OperationIcons["Insert"] = Settings.Instance.Image("icons.insert.png");
		OperationIcons["Replace"] = Settings.Instance.Image("icons.insert.png");
		OperationIcons["Remove"] = Settings.Instance.Image("icons.remove.png");
		OperationIcons["Open"] = Settings.Instance.Image("icons.open_16.png");
		OperationIcons["New"] = Settings.Instance.Image("icons.new_16.png");
		
		host.ActiveViewChanged += OnActiveViewChanged;
	}


	public void OnActiveViewChanged(object sender, EventArgs e)
	{
		if(LastDocument != null)
		{
			LastDocument.HistoryAdded -= OnHistoryAdded;
			LastDocument.HistoryRemoved -= OnHistoryRemoved;
			LastDocument.HistoryUndone -= OnHistoryUndone;
			LastDocument.HistoryRedone -= OnHistoryRedone;
			LastDocument.HistoryJumped -= OnHistoryJumped;
			LastDocument.HistoryCleared -= OnHistoryCleared;
		}
		
		if(Host.ActiveView != null)
		{
			LastDocument = Host.ActiveView.Document;
			LastDocument.HistoryAdded += OnHistoryAdded;
			LastDocument.HistoryRemoved += OnHistoryRemoved;
			LastDocument.HistoryUndone += OnHistoryUndone;
			LastDocument.HistoryRedone += OnHistoryRedone;
			LastDocument.HistoryJumped += OnHistoryJumped;
			LastDocument.HistoryCleared += OnHistoryCleared;
			
			if(StructureChanged != null)
				StructureChanged(this, new Aga.Controls.Tree.TreePathEventArgs());
		}
	}
	
	public void OnDrawNodeText(object sender, Aga.Controls.Tree.NodeControls.DrawEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		if(item.Active == false)
			e.TextColor = Color.Gray;
		if(item == LastDocument.History)
			e.Font = _BoldFont;
	}

	public void OnNameValueNeeded(object sender, Aga.Controls.Tree.NodeControls.NodeControlValueEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		e.Value = item.Operation.ToString();
	}
	
	public void OnIconValueNeeded(object sender, Aga.Controls.Tree.NodeControls.NodeControlValueEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		Image icon;
		if(OperationIcons.TryGetValue(item.Operation, out icon))
			e.Value = icon;
		else
			e.Value = UnknownOperationIcon;
	}
	
	public void OnRangeValueNeeded(object sender, Aga.Controls.Tree.NodeControls.NodeControlValueEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		if(item.OldLength > 1)
			e.Value = String.Format("{0} -> {1}", item.StartPosition, item.StartPosition + item.OldLength - 1);
		else
			e.Value = item.StartPosition.ToString();
	}		
	
	public void OnNodeDoubleClick(object sender, Aga.Controls.Tree.TreeNodeAdvMouseEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		if(item != LastDocument.History)
			LastDocument.HistoryJump(item);
	}

	private void OnSelectionChanged(object sender, EventArgs e)
	{
		if(_TreeView.SelectedNode != null)
		{
			PieceBuffer.HistoryItem item = _TreeView.GetPath(_TreeView.SelectedNode).LastNode as PieceBuffer.HistoryItem;
			if(item != null)
			{
				RangeIndicator.Ranges.Clear();
				if(item.OldLength > 0)
				{
					RangeIndicator.Ranges.Add(new DocumentRange(item.StartPosition, 
					                                            item.StartPosition + item.OldLength,
					                                            Color.FromArgb(128, 255, 0, 0)));
				}
				if(item.NewLength > 0)
				{
					RangeIndicator.Ranges.Add(new DocumentRange(item.StartPosition,
					                                            item.StartPosition + item.NewLength,
					                                            Color.FromArgb(128, 0, 255, 0)));
				}

				if(item.NewLength - item.OldLength > 0)
					RangeIndicator.DocumentLength = item.DocumentLength + (item.NewLength - item.OldLength);
				else
					RangeIndicator.DocumentLength = item.DocumentLength;
			}
			else
				RangeIndicator.Ranges.Clear();
		}
	}
	
	public void OnHistoryAdded(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(NodesInserted == null || NodesRemoved == null || StructureChanged == null)
			return;
		
		if(e.NewItem.NextSibling != null)
		{
			Aga.Controls.Tree.TreeNodeAdv parentNode = _TreeView.FindNode(new Aga.Controls.Tree.TreePath(new object[] {e.OldItem}));
			Aga.Controls.Tree.TreeNodeAdv nextNode = parentNode.NextNode;
			List<object> removedNodes = new List<object>();
			while(nextNode != null)
			{
				removedNodes.Add(_TreeView.GetPath(nextNode).LastNode);
				nextNode = nextNode.NextNode;
			}
			
			NodesRemoved(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), removedNodes.ToArray()));
			StructureChanged(this, new Aga.Controls.Tree.TreePathEventArgs(new Aga.Controls.Tree.TreePath(new object[] {e.OldItem})));
		}
		
		Aga.Controls.Tree.TreePath parentPath = new Aga.Controls.Tree.TreePath();
		NodesInserted(this, new Aga.Controls.Tree.TreeModelEventArgs(parentPath, new int[] {-1}, new object[] {e.NewItem}));
		
		_TreeView.EnsureVisible(_TreeView.FindNode(new Aga.Controls.Tree.TreePath(new object [] {e.NewItem})));
	}
	
	public void OnHistoryRemoved(object sender, PieceBuffer.HistoryEventArgs e)
	{
		NodesRemoved(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), 
		                                                            new object[] { e.OldItem }));
	}

	public void OnHistoryUndone(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(NodesChanged != null)
			NodesChanged(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), new object[] {e.OldItem}));
	}
	
	public void OnHistoryRedone(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(NodesChanged != null)
			NodesChanged(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), new object[] {e.NewItem}));
	}
	
	public void OnHistoryJumped(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(StructureChanged != null)
		{
			StructureChanged(this, new Aga.Controls.Tree.TreePathEventArgs());
			//NodesChanged(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), new object[] {e.OldItem}));
			//NodesChanged(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), new object[] {e.NewItem}));
		}
	}

	public void OnHistoryCleared(object sender, EventArgs args)
	{
		if(StructureChanged != null)
			StructureChanged(this, new Aga.Controls.Tree.TreePathEventArgs());
	}
	
	//
	// ITreeModel
	//
	
	public System.Collections.IEnumerable GetChildren(Aga.Controls.Tree.TreePath treePath)
	{
		List<PieceBuffer.HistoryItem> items = new List<PieceBuffer.HistoryItem>();

		if(Host.ActiveView == null)
			return items; 
			
		PieceBuffer.HistoryItem histItem;
		if(treePath.IsEmpty())
			histItem = Host.ActiveView.Document.HistoryRoot;
		else
			histItem = ((PieceBuffer.HistoryItem)treePath.LastNode).NextSibling;
		
		while(histItem != null)
		{
			items.Add(histItem);
			histItem = histItem.FirstChild;
		}

		return items;
	}
	
	public bool IsLeaf(Aga.Controls.Tree.TreePath treePath)
	{
		return ((PieceBuffer.HistoryItem)treePath.LastNode).NextSibling == null;
	}
	
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesChanged;
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesInserted;
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesRemoved;
	public event EventHandler<Aga.Controls.Tree.TreePathEventArgs> StructureChanged;
}
