using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;


class HistoryTreeItem
{
	private HistoryTreeItem _Parent;
	private PieceBuffer.HistoryItem _Item;
	
	public Image Icon
	{
		get { return null; }
	}
	
	public string Date
	{
		get { return _Item.Date.ToString("G"); }
	}
	
	public string Range
	{
		get { return String.Format("{0} - {1}", Item.StartPosition, _Item.EndPosition); }
	}
	
	public string Name
	{
		get { return _Item.Operation.ToString(); }
	}
	
	public PieceBuffer.HistoryItem Item
	{
		get { return _Item; }
	}
	
	public HistoryTreeItem Parent
	{
		get { return _Parent; }
	}
	
	public object[] Path
	{
		get
		{
			Stack<object> parents = new Stack<object>();
			HistoryTreeItem item = this;
			while(item != null)
			{
				parents.Push(item);
				item = item.Parent;
			}
			return parents.ToArray();
		}
	}
	
	public HistoryTreeItem(HistoryTreeItem parent, PieceBuffer.HistoryItem item)
	{
		_Parent = parent;
		_Item = item;
	}
}



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
	
	private Font _BoldFont;

	protected IPluginHost Host;
	protected Document LastDocument;
	
	protected Dictionary<PieceBuffer.HistoryItem, HistoryTreeItem> TreeItemMap = new Dictionary<PieceBuffer.HistoryItem,HistoryTreeItem>();
	
	public HistoryPanel(IPluginHost host)
	{
		Host = host;
		
		_TreeView = new Aga.Controls.Tree.TreeViewAdv();
		_TreeColumnName = new Aga.Controls.Tree.TreeColumn("Name", 100);
		_TreeColumnDate = new Aga.Controls.Tree.TreeColumn("Date", 100);
		_TreeColumnRange = new Aga.Controls.Tree.TreeColumn("Range", 100);
		_NodeControlIcon = new Aga.Controls.Tree.NodeControls.NodeStateIcon();
		_NodeControlName = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlDate = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlRange = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlName.DrawText += OnDrawNodeText;

		_BoldFont = new Font(_NodeControlName.Font, FontStyle.Bold);
	
		_TreeView.AllowColumnReorder = true;
		_TreeView.AutoRowHeight = true;
//		_TreeView.BackColor = System.Drawing.SystemColors.Window;
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
		
		_NodeControlIcon.DataPropertyName = "Icon";
		_NodeControlIcon.ParentColumn = _TreeColumnName;
		
		_NodeControlName.DataPropertyName = "Name";
		_NodeControlName.ParentColumn = _TreeColumnName;
		_NodeControlName.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlName.UseCompatibleTextRendering = true;
		
		_NodeControlDate.DataPropertyName = "Date";
		_NodeControlDate.ParentColumn = _TreeColumnDate;
		_NodeControlDate.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlDate.UseCompatibleTextRendering = true;
		
		_NodeControlRange.DataPropertyName = "Range";
		_NodeControlRange.ParentColumn = _TreeColumnRange;
		_NodeControlRange.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlRange.UseCompatibleTextRendering = true;
		
		_TreeView.Dock = DockStyle.Fill;
		Controls.Add(_TreeView);
		
		host.ActiveViewChanged += OnActiveViewChanged;
	}
	
	public void OnActiveViewChanged(object sender, EventArgs e)
	{
		if(LastDocument != null)
		{
			LastDocument.Buffer.HistoryAdded -= OnHistoryAdded;
			LastDocument.Buffer.HistoryUndone -= OnHistoryUndone;
			LastDocument.Buffer.HistoryRedone -= OnHistoryRedone;
		}
		
		LastDocument = Host.ActiveView.Document;
		LastDocument.Buffer.HistoryAdded += OnHistoryAdded;
		LastDocument.Buffer.HistoryUndone += OnHistoryUndone;
		LastDocument.Buffer.HistoryRedone += OnHistoryRedone;
		
		OnStructureChanged();
		
		Console.WriteLine("View Changed");
	}
	
	public void OnDrawNodeText(object sender, Aga.Controls.Tree.NodeControls.DrawEventArgs e)
	{
		HistoryTreeItem item = _TreeView.GetPath(e.Node).LastNode as HistoryTreeItem;
		if(item.Item.Active == false)
			e.TextColor = Color.Gray;
		if(item.Item == LastDocument.Buffer.History)
			e.Font = _BoldFont;
	}
	
	public void OnHistoryAdded(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(NodesInserted == null || NodesRemoved == null || StructureChanged == null)
			return;
		
		HistoryTreeItem oldTreeItem;
		if(!TreeItemMap.TryGetValue(e.OldItem, out oldTreeItem))
			return;
		
		if(e.NewItem.NextSibling != null)
		{
			Aga.Controls.Tree.TreeNodeAdv parentNode = _TreeView.FindNode(new Aga.Controls.Tree.TreePath(oldTreeItem.Path));
			Aga.Controls.Tree.TreeNodeAdv nextNode = parentNode.NextNode;
			List<object> removedNodes = new List<object>();
			while(nextNode != null)
			{
				removedNodes.Add(_TreeView.GetPath(nextNode).LastNode);
				nextNode = nextNode.NextNode;
			}
			
			NodesRemoved(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), removedNodes.ToArray()));
			StructureChanged(this, new Aga.Controls.Tree.TreePathEventArgs(new Aga.Controls.Tree.TreePath(oldTreeItem.Path)));
		}
		
		HistoryTreeItem newTreeItem = new HistoryTreeItem(null, e.NewItem);
		Aga.Controls.Tree.TreePath parentPath = new Aga.Controls.Tree.TreePath();
		TreeItemMap.Add(e.NewItem, newTreeItem);
		NodesInserted(this, new Aga.Controls.Tree.TreeModelEventArgs(parentPath, new int[] {-1}, new object[] {newTreeItem}));
		
		_TreeView.EnsureVisible(_TreeView.FindNode(new Aga.Controls.Tree.TreePath(newTreeItem.Path)));
	}
	
	public void OnHistoryUndone(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(NodesChanged != null)
		{
			HistoryTreeItem oldTreeItem;
			if(TreeItemMap.TryGetValue(e.OldItem, out oldTreeItem))
			{
				HistoryTreeItem newTreeItem;
				if(TreeItemMap.TryGetValue(e.NewItem, out newTreeItem))
					NodesChanged(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), new object[] {oldTreeItem}));
			}
		}
	}
	
	public void OnHistoryRedone(object sender, PieceBuffer.HistoryEventArgs e)
	{
		if(NodesChanged != null)
		{
			HistoryTreeItem newTreeItem;
			if(TreeItemMap.TryGetValue(e.NewItem, out newTreeItem))
				NodesChanged(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(), new object[] {newTreeItem}));
		}
	}
	
	
	
	public System.Collections.IEnumerable GetChildren(Aga.Controls.Tree.TreePath treePath)
	{
		List<HistoryTreeItem> items = new List<HistoryTreeItem>();

		if(Host.ActiveView == null)
			return items; 
			
		PieceBuffer.HistoryItem histItem;
		if(treePath.IsEmpty())
			histItem = Host.ActiveView.Document.Buffer.HistoryRoot;
		else
			histItem = ((HistoryTreeItem)treePath.LastNode).Item.NextSibling;
		
		while(histItem != null)
		{
			HistoryTreeItem treeItem;
			if(!TreeItemMap.TryGetValue(histItem, out treeItem))
			{
				treeItem = new HistoryTreeItem(null, histItem);
				TreeItemMap.Add(histItem, treeItem);
			}
			
			items.Add(treeItem);
			histItem = histItem.FirstChild;
		}

		return items;
	}
	
	public bool IsLeaf(Aga.Controls.Tree.TreePath treePath)
	{
		Console.WriteLine("IsLeaf: " + (((HistoryTreeItem)treePath.LastNode).Item.FirstChild == null));
		return ((HistoryTreeItem)treePath.LastNode).Item.NextSibling == null;
	}
	
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesChanged;
	internal void OnNodesChanged(HistoryTreeItem item)
	{
//		if (NodesChanged != null)
//		{
//			TreePath path = GetPath(item.Parent);
//			NodesChanged(this, new TreeModelEventArgs(path, new object[] { item }));
//		}
	}

	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesInserted;
	public void OnNodesInserted(PieceBuffer.HistoryItem item)
	{
		if(NodesInserted != null)
		{
			HistoryTreeItem parent;
			if(TreeItemMap.TryGetValue(item.Parent, out parent))
			{
				HistoryTreeItem i = new HistoryTreeItem(parent, item);
				TreeItemMap.Add(item, i);
				
				NodesInserted(this, new Aga.Controls.Tree.TreeModelEventArgs(new Aga.Controls.Tree.TreePath(parent.Path), new int[] {0}, new object[] { i }));
				Aga.Controls.Tree.TreeNodeAdv node = _TreeView.FindNode(new Aga.Controls.Tree.TreePath(i.Path));
//				_TreeView.EnsureVisible(node);
				_TreeView.SelectedNode = node;
			}
		}
	}
	
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesRemoved;
	public event EventHandler<Aga.Controls.Tree.TreePathEventArgs> StructureChanged;
	public void OnStructureChanged()
	{
		if (StructureChanged != null)
			StructureChanged(this, new Aga.Controls.Tree.TreePathEventArgs());
	}
	
}
