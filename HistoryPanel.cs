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

	private ToolStrip         ToolBar;
	private ToolStripComboBox TaggedRevisionComboBox;
	private ToolStripButton   TagAddButton;
	private ToolStripButton   TagDeleteButton;
	private ToolStripButton   TagRevertButton;
	private List< KeyValuePair<string, PieceBuffer.HistoryItem> > TaggedRevisionList;

	private Dictionary<string, Image> OperationIcons;
	private Image UnknownOperationIcon;
	
	private Font _BoldFont;

	protected IPluginHost Host;
	protected Document LastDocument;
	
	
	public HistoryPanel(IPluginHost host)
	{
		Host = host;
	
		Host.Commands.Add("History/Tag Revision", "Tags the current revision in the undo/redo history", "Tag Revision",
		                  Host.Settings.Image("icons.bookmark_16.png"),
		                  null,
		                  OnTagRevision);
		Host.Commands.Add("History/Revert To Tag", "Jumps to the selected tag", "Revert To Tag",
		                  Host.Settings.Image("icons.go_16.png"),
		                  null,
		                  OnRevertToTag);
		Host.Commands.Add("History/Delete Tag", "Deletes the selected tag", "Delete Tag",
		                  Host.Settings.Image("icons.delete_16.png"),
		                  null,
		                  OnDeleteTag);
	
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
		
		TaggedRevisionList = new List< KeyValuePair<string, PieceBuffer.HistoryItem> >();

		ToolBar = new ToolStrip();
		TagAddButton = Host.CreateToolButton("History/Tag Revision");
		TagAddButton.Enabled = false;
		ToolBar.Items.Add(TagAddButton);
		ToolBar.Items.Add(new ToolStripSeparator());
		TaggedRevisionComboBox = new ToolStripComboBox();
		TaggedRevisionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		TaggedRevisionComboBox.Dock = DockStyle.Fill;
		TaggedRevisionComboBox.ComboBox.DataSource = new BindingSource(TaggedRevisionList, null);
		TaggedRevisionComboBox.ComboBox.DisplayMember = "Key";
		TaggedRevisionComboBox.ComboBox.ValueMember = "Value";
		TaggedRevisionComboBox.ComboBox.SelectedValueChanged += OnTaggedRevisionSelectedValueChanged;
		ToolBar.Items.Add(TaggedRevisionComboBox);
		TagDeleteButton = Host.CreateToolButton("History/Delete Tag");
		TagDeleteButton.Enabled = false;
		ToolBar.Items.Add(TagDeleteButton);
		TagRevertButton = Host.CreateToolButton("History/Revert To Tag");
		TagRevertButton.Enabled = false;
		ToolBar.Items.Add(TagRevertButton);
		ToolBar.GripStyle = ToolStripGripStyle.Hidden;
		Controls.Add(ToolBar);

		OperationIcons = new Dictionary<string, Image>();
		UnknownOperationIcon = Settings.Instance.Image("icons.unknown_op.png");
		OperationIcons["Insert"] = Settings.Instance.Image("icons.insert.png");
		OperationIcons["Replace"] = Settings.Instance.Image("icons.insert.png");
		OperationIcons["Remove"] = Settings.Instance.Image("icons.remove.png");
		OperationIcons["Open"] = Settings.Instance.Image("icons.open_16.png");
		OperationIcons["New"] = Settings.Instance.Image("icons.new_16.png");
		
		host.ActiveViewChanged += OnActiveViewChanged;
	}

	public void OnTaggedRevisionSelectedValueChanged(object sender, EventArgs e)
	{
		if(TaggedRevisionComboBox.ComboBox.SelectedValue != null)
		{
Console.WriteLine("SelectedValue: " + TaggedRevisionComboBox.ComboBox.SelectedValue.ToString());
			TagDeleteButton.Enabled = true;
			TagRevertButton.Enabled = true;
		}
		else
		{
Console.WriteLine("SelectedValue: null");
			TagDeleteButton.Enabled = false;
			TagRevertButton.Enabled = false;
		}
	}

	public void OnTagRevision(object sender, EventArgs e)
	{
		string name = "";

		if(InputDialog.Show("Tag Revision", "Tag Name:", ref name) == DialogResult.OK)
		{
			TaggedRevisionList.Add(new KeyValuePair<string, PieceBuffer.HistoryItem>(name, LastDocument.History));
			((BindingSource)TaggedRevisionComboBox.ComboBox.DataSource).ResetBindings(false);
			TaggedRevisionComboBox.ComboBox.SelectedIndex = TaggedRevisionList.Count - 1;
		}
	}

	public void OnRevertToTag(object sender, EventArgs e)
	{
		if(TaggedRevisionComboBox.ComboBox.SelectedValue != null)
			LastDocument.HistoryJump(TaggedRevisionComboBox.ComboBox.SelectedValue as PieceBuffer.HistoryItem);
	}

	public void OnDeleteTag(object sender, EventArgs e)
	{
		if(TaggedRevisionComboBox.ComboBox.SelectedIndex >= 0)
		{
			if(MessageBox.Show(this, "Are you sure you want to delete the tag?",
			                   "Delete Tag?",
			                   MessageBoxButtons.YesNo,
			                   MessageBoxIcon.Question) == DialogResult.Yes)
			{
				TaggedRevisionList.RemoveAt(TaggedRevisionComboBox.ComboBox.SelectedIndex);
				((BindingSource)TaggedRevisionComboBox.ComboBox.DataSource).ResetBindings(false);
				TaggedRevisionComboBox.SelectedIndex = TaggedRevisionList.Count - 1;
				OnTaggedRevisionSelectedValueChanged(this, null);
			}
		}
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
				long offset = 0;

				RangeIndicator.Ranges.Clear();
				if(item.OldLength > 0)
				{
					RangeIndicator.Ranges.Add(new DocumentRange(item.StartPosition, 
					                                            item.StartPosition + item.OldLength,
					                                            Color.FromArgb(128, 255, 0, 0)));
					offset = item.OldLength;
				}
				if(item.NewLength > 0)
				{
					RangeIndicator.Ranges.Add(new DocumentRange(item.StartPosition + offset,
					                                            item.StartPosition + offset + item.NewLength,
					                                            Color.FromArgb(128, 0, 255, 0)));
				}

				RangeIndicator.DocumentLength = item.DocumentLength + item.NewLength;
			}
			else
				RangeIndicator.Ranges.Clear();

			TagAddButton.Enabled = true;
		}
		else
		{
			RangeIndicator.Ranges.Clear();
			TagAddButton.Enabled = false;
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
