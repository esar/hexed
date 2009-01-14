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
	
	private Image[] OperationIcons;
	
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
		
		int numOperations = Enum.GetNames(typeof(PieceBuffer.HistoryOperation)).Length;
		OperationIcons = new Image[numOperations];
		Image unknownOpImage = Settings.Instance.Image("unknown_op.png");
		for(int i = 0; i < numOperations; ++i)
			OperationIcons[i] = unknownOpImage;
		OperationIcons[(int)PieceBuffer.HistoryOperation.Insert] = Settings.Instance.Image("insert.png");
		OperationIcons[(int)PieceBuffer.HistoryOperation.Remove] = Settings.Instance.Image("remove.png");
		OperationIcons[(int)PieceBuffer.HistoryOperation.Open] = Settings.Instance.Image("open_16.png");
		OperationIcons[(int)PieceBuffer.HistoryOperation.New] = Settings.Instance.Image("new_16.png");
		
		host.ActiveViewChanged += OnActiveViewChanged;
	}


	public void OnActiveViewChanged(object sender, EventArgs e)
	{
		if(LastDocument != null)
		{
			LastDocument.HistoryAdded -= OnHistoryAdded;
			LastDocument.HistoryUndone -= OnHistoryUndone;
			LastDocument.HistoryRedone -= OnHistoryRedone;
			LastDocument.HistoryJumped -= OnHistoryJumped;
		}
		
		if(Host.ActiveView != null)
		{
			LastDocument = Host.ActiveView.Document;
			LastDocument.HistoryAdded += OnHistoryAdded;
			LastDocument.HistoryUndone += OnHistoryUndone;
			LastDocument.HistoryRedone += OnHistoryRedone;
			LastDocument.HistoryJumped += OnHistoryJumped;
			
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
		e.Value = OperationIcons[(int)item.Operation];
	}
	
	public void OnRangeValueNeeded(object sender, Aga.Controls.Tree.NodeControls.NodeControlValueEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		if(item.Length > 1)
			e.Value = String.Format("{0} -> {1}", item.StartPosition, item.StartPosition + item.Length - 1);
		else
			e.Value = item.StartPosition.ToString();
	}		
	
	public void OnNodeDoubleClick(object sender, Aga.Controls.Tree.TreeNodeAdvMouseEventArgs e)
	{
		PieceBuffer.HistoryItem item = _TreeView.GetPath(e.Node).LastNode as PieceBuffer.HistoryItem;
		if(item != LastDocument.History)
			LastDocument.HistoryJump(item);
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
