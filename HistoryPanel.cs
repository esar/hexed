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
	private Aga.Controls.Tree.TreeColumn _TreeColumnName;

	protected IPluginHost Host;
	protected Document LastDocument;
	
	protected Dictionary<PieceBuffer.HistoryItem, HistoryTreeItem> TreeItemMap = new Dictionary<PieceBuffer.HistoryItem,HistoryTreeItem>();
	
	public HistoryPanel(IPluginHost host)
	{
		Host = host;
		
		_TreeView = new Aga.Controls.Tree.TreeViewAdv();
		_TreeColumnName = new Aga.Controls.Tree.TreeColumn("Name", 100);
		_NodeControlIcon = new Aga.Controls.Tree.NodeControls.NodeStateIcon();
		_NodeControlName = new Aga.Controls.Tree.NodeControls.NodeTextBox();

	
		_TreeView.AllowColumnReorder = true;
		_TreeView.AutoRowHeight = true;
//		_TreeView.BackColor = System.Drawing.SystemColors.Window;
		_TreeView.Columns.Add(_TreeColumnName);
		_TreeView.Cursor = System.Windows.Forms.Cursors.Default;
		_TreeView.FullRowSelect = true;
		_TreeView.GridLineStyle = ((Aga.Controls.Tree.GridLineStyle)((Aga.Controls.Tree.GridLineStyle.Horizontal | Aga.Controls.Tree.GridLineStyle.Vertical)));
		_TreeView.LineColor = System.Drawing.SystemColors.ControlDark;
		_TreeView.LoadOnDemand = true;
		_TreeView.Model = this;
		_TreeView.Name = "_TreeView";
		_TreeView.NodeControls.Add(this._NodeControlIcon);
		_TreeView.NodeControls.Add(this._NodeControlName);
		_TreeView.ShowNodeToolTips = true;
		
		_NodeControlIcon.DataPropertyName = "Icon";
		_NodeControlIcon.ParentColumn = _TreeColumnName;
		
		_NodeControlName.DataPropertyName = "Name";
		_NodeControlName.ParentColumn = _TreeColumnName;
		_NodeControlName.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlName.UseCompatibleTextRendering = true;

		_TreeView.Dock = DockStyle.Fill;
		Controls.Add(_TreeView);
		
		host.ActiveViewChanged += OnActiveViewChanged;
	}
	
	public void OnActiveViewChanged(object sender, EventArgs e)
	{
		if(LastDocument != null)
			LastDocument.Buffer.HistoryAdded -= OnHistoryAdded; 
		Host.ActiveView.Document.Buffer.HistoryAdded += OnHistoryAdded;
		LastDocument = Host.ActiveView.Document;
		
		OnStructureChanged();
		
		Console.WriteLine("View Changed");
	}
	
	public void OnHistoryAdded(object sender, PieceBuffer.HistoryAddedEventArgs e)
	{
		OnNodesInserted(e.Item);
		
		Console.WriteLine("History Added");
	}
	
	
	
	public System.Collections.IEnumerable GetChildren(Aga.Controls.Tree.TreePath treePath)
	{
		List<HistoryTreeItem> items = new List<HistoryTreeItem>();

		if(Host.ActiveView == null)
			return items; 
		
		PieceBuffer.HistoryItem hi;		
		if(treePath.IsEmpty())
			hi = Host.ActiveView.Document.Buffer.HistoryRoot;
		else
			hi = ((HistoryTreeItem)treePath.LastNode).Item.FirstChild;

		while(hi != null)
		{
			HistoryTreeItem item;
			if(!TreeItemMap.TryGetValue(hi, out item))
			{
				item = new HistoryTreeItem(null, hi);
				TreeItemMap.Add(hi, item);
			}
			
			items.Add(item);
			hi = hi.NextSibling;
		}
		
		return items;
	}
	
	public bool IsLeaf(Aga.Controls.Tree.TreePath treePath)
	{
		Console.WriteLine("IsLeaf: " + (((HistoryTreeItem)treePath.LastNode).Item.FirstChild == null));
		return ((HistoryTreeItem)treePath.LastNode).Item.FirstChild == null;
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
