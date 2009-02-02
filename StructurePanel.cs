using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;

class StructureTreeModel : Aga.Controls.Tree.ITreeModel
{
	private IPluginHost _Host;
		
	public StructureTreeModel(IPluginHost host)
	{
		_Host = host;
	}

	public System.Collections.IEnumerable GetChildren(Aga.Controls.Tree.TreePath treePath)
	{
		List<StructureTreeItem> items = new List<StructureTreeItem>();
		
		if(treePath.IsEmpty())
		{
			for(int i = 0; i < _Host.ActiveView.Document.Structure._Children.Count; ++i)
			{
				Record r = _Host.ActiveView.Document.Structure._Children[i];
				if(r.ArrayLength > 1)
					items.Add(new StructureTreeItem(r.Name + "[" + r.ArrayLength + "]", r.ToString(), r.GetType().ToString(), r, this));
				else
					items.Add(new StructureTreeItem(r.Name, r.ToString(), r.GetType().ToString(), r, this));
			}
		}
		else
		{
			if(((StructureTreeItem)treePath.LastNode).Record.ArrayLength > 1)
			{
				Record record = ((StructureTreeItem)treePath.LastNode).Record; 
				if(record.ArrayElements != null)
				{
					int i = 0;
					foreach(Record r in record.ArrayElements)
						items.Add(new StructureTreeItem(r.Name + "[" + (i++) + "]", r.ToString(), r.GetType().ToString(), r, this));
				}
				else
				{
					for(int i = 0; i < (int)((StructureTreeItem)treePath.LastNode).Record.ArrayLength; ++i)
					{
						Record r = record.GetArrayElement((ulong)i);
						items.Add(new StructureTreeItem(r.Name + "[" + i + "]", r.ToString(), r.GetType().ToString(), r, this));
					}
				}
			}
			else
			{
				for(int i = 0; i < ((StructureTreeItem)treePath.LastNode).Record._Children.Count; ++i)
				{
					Record r = ((StructureTreeItem)treePath.LastNode).Record._Children[i];
					if(r.ArrayLength > 1)
						items.Add(new StructureTreeItem(r.Name + "[" + r.ArrayLength + "]", r.ToString(), r.GetType().ToString(), r, this));
					else
						items.Add(new StructureTreeItem(r.Name, r.ToString(), r.GetType().ToString(), r, this));
				}
			}
		}
		
		return items;
	}
	
	public bool IsLeaf(Aga.Controls.Tree.TreePath treePath)
	{
		return ((StructureTreeItem)treePath.LastNode).Record._Children.Count == 0 &&
			   ((StructureTreeItem)treePath.LastNode).Record.ArrayLength <= 1;
	}
	
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesChanged;
	internal void OnNodesChanged(StructureTreeItem item)
	{
//		if (NodesChanged != null)
//		{
//			TreePath path = GetPath(item.Parent);
//			NodesChanged(this, new TreeModelEventArgs(path, new object[] { item }));
//		}
	}

	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesInserted;
	public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesRemoved;
	public event EventHandler<Aga.Controls.Tree.TreePathEventArgs> StructureChanged;
	public void OnStructureChanged()
	{
//		if (StructureChanged != null)
//			StructureChanged(this, new TreePathEventArgs());
	}
}

class StructureTreeItem
{
	private Image _Icon;
	private string _Name;
	private string _Value;
	private string _Type;
	private StructureTreeModel _Model;
	private Record _Record;
	
	public Image Icon
	{
		get { return _Icon; }
		set { _Icon = value; }
	}
	
	public string Name
	{
		get { return _Name; }
		set { _Name = value; }
	}
	
	public string Value
	{
		get { return _Value; }
		set { _Value = value; _Record.SetValue(value); }
	}
	
	public string Type
	{
		get { return _Type; }
		set { _Type = value; }
	}
	
	public Record Record
	{
		get { return _Record; }
	}
	
	public StructureTreeItem(string name, string value, string type, Record record, StructureTreeModel model)
	{
		_Name = name;
		_Value = value;
		_Type = type;
		_Model = model;
		_Record = record;
	}
}

class StructurePanel : Panel
{
	private Aga.Controls.Tree.TreeViewAdv _TreeView;
	private Aga.Controls.Tree.NodeControls.NodeStateIcon _NodeControlIcon;
	private Aga.Controls.Tree.NodeControls.NodeTextBox _NodeControlName;
	private Aga.Controls.Tree.NodeControls.NodeTextBox _NodeControlValue;
	private Aga.Controls.Tree.NodeControls.NodeTextBox _NodeControlType;
	private Aga.Controls.Tree.TreeColumn _TreeColumnName;
	private Aga.Controls.Tree.TreeColumn _TreeColumnValue;
	private Aga.Controls.Tree.TreeColumn _TreeColumnType;

	protected ToolStrip ToolBar;
	protected IPluginHost Host;

	public event EventHandler SelectionChanged;
	
	public List<Record> SelectedRecords
	{
		get 
		{
			List<Record> list = new List<Record>();
			
			foreach(Aga.Controls.Tree.TreeNodeAdv n in _TreeView.SelectedNodes)
				list.Add(((StructureTreeItem)n.Tag).Record);
			return list; 
		}
	}

	
	public StructurePanel(IPluginHost host)
	{
		Host = host;
		
		_TreeView = new Aga.Controls.Tree.TreeViewAdv();
		_TreeColumnName = new Aga.Controls.Tree.TreeColumn("Name", 100);
		_TreeColumnValue = new Aga.Controls.Tree.TreeColumn("Value", 50);
		_TreeColumnType = new Aga.Controls.Tree.TreeColumn("Type", 50);
		_NodeControlIcon = new Aga.Controls.Tree.NodeControls.NodeStateIcon();
		_NodeControlName = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlValue = new Aga.Controls.Tree.NodeControls.NodeTextBox();
		_NodeControlType = new Aga.Controls.Tree.NodeControls.NodeTextBox();

	
		_TreeView.AllowColumnReorder = true;
		_TreeView.AutoRowHeight = true;
//		_TreeView.BackColor = System.Drawing.SystemColors.Window;
		_TreeView.Columns.Add(_TreeColumnName);
		_TreeView.Columns.Add(_TreeColumnValue);
		_TreeView.Columns.Add(_TreeColumnType);
		_TreeView.Cursor = System.Windows.Forms.Cursors.Default;
//		_TreeView.DefaultToolTipProvider = null;
//		_TreeView.DragDropMarkColor = System.Drawing.Color.Black;
		_TreeView.FullRowSelect = true;
		_TreeView.GridLineStyle = ((Aga.Controls.Tree.GridLineStyle)((Aga.Controls.Tree.GridLineStyle.Horizontal | Aga.Controls.Tree.GridLineStyle.Vertical)));
		_TreeView.LineColor = System.Drawing.SystemColors.ControlDark;
		_TreeView.LoadOnDemand = true;
		_TreeView.Model = null;
		_TreeView.Name = "_TreeView";
		_TreeView.NodeControls.Add(this._NodeControlIcon);
		_TreeView.NodeControls.Add(this._NodeControlName);
		_TreeView.NodeControls.Add(this._NodeControlValue);
		_TreeView.NodeControls.Add(this._NodeControlType);
//		_TreeView.SelectedNode = null;
		_TreeView.ShowNodeToolTips = true;
		_TreeView.UseColumns = true;
		_TreeView.AllowDrop = true;
		_TreeView.ItemDrag += OnItemDrag;
		_TreeView.DragOver += OnDragOver;
		_TreeView.DragDrop += OnDragDrop;
		_TreeView.SelectionChanged += OnSelectionChanged;
//		_TreeView.NodeMouseDoubleClick += new System.EventHandler<Aga.Controls.Tree.TreeNodeAdvMouseEventArgs>(this._treeView_NodeMouseDoubleClick);
//		_TreeView.ColumnClicked += new System.EventHandler<Aga.Controls.Tree.TreeColumnEventArgs>(this._treeView_ColumnClicked);
//		_TreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this._treeView_MouseClick);
		
		_NodeControlIcon.DataPropertyName = "Icon";
		_NodeControlIcon.ParentColumn = _TreeColumnName;
		
		_NodeControlName.DataPropertyName = "Name";
		_NodeControlName.ParentColumn = _TreeColumnName;
		_NodeControlName.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
		_NodeControlName.UseCompatibleTextRendering = true;

		_NodeControlValue.DataPropertyName = "Value";
		_NodeControlValue.ParentColumn = _TreeColumnValue;
		
		_NodeControlType.DataPropertyName = "Type";
		_NodeControlType.ParentColumn = _TreeColumnType;
		
		_TreeView.Dock = DockStyle.Fill;
		Controls.Add(_TreeView);
		
		ToolBar = new ToolStrip();
		ToolBar.Dock = DockStyle.Top;
		ToolBar.GripStyle = ToolStripGripStyle.Hidden;
		Controls.Add(ToolBar);

		ToolBar.Items.Add(Settings.Instance.Image("open_16.png")).Click += new EventHandler(OnOpenStructureDef);
	}

	
	
	protected void OnOpenStructureDef(object sender, EventArgs e)
	{
		FileDialog dlg = new OpenFileDialog();
		
		dlg.Title = "Open Structure Definition";
		if(dlg.ShowDialog() == DialogResult.OK)
		{
			Host.ActiveView.Document.ApplyStructureDefinition(dlg.FileName);
			_TreeView.Model = new StructureTreeModel(Host);
		}
	}

	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		if(SelectionChanged != null)
			SelectionChanged(this, new EventArgs());
	}
	
	protected void OnItemDrag(object sender, ItemDragEventArgs e)
	{
		_TreeView.DoDragDropSelectedNodes(DragDropEffects.Move);
	}

	protected void OnDragEnter(object sender, DragEventArgs e)
	{
//		e.Effect = DragDropEffects.All;
	}

	protected void OnDragOver(object sender, DragEventArgs e)
	{
		if(e.Data.GetDataPresent(typeof(Aga.Controls.Tree.TreeNodeAdv[])) && _TreeView.DropPosition.Node != null)
		{
			Aga.Controls.Tree.TreeNodeAdv[] nodes = e.Data.GetData(typeof(Aga.Controls.Tree.TreeNodeAdv[])) as Aga.Controls.Tree.TreeNodeAdv[];
			Aga.Controls.Tree.TreeNodeAdv parent = _TreeView.DropPosition.Node;
			if(_TreeView.DropPosition.Position != Aga.Controls.Tree.NodePosition.Inside)
				parent = parent.Parent;

			foreach(Aga.Controls.Tree.TreeNodeAdv node in nodes)
			{
//				if(!CheckNodeParent(parent, node))
//				{
//					e.Effect = DragDropEffects.None;
//					return;
//				}
			}

			e.Effect = e.AllowedEffect;
		}
	}

	protected void OnDragDrop(object sender, DragEventArgs e)
	{
		/*
		Point p = Tree.PointToClient(new Point(e.X, e.Y));
		TreeListViewItem node = (TreeListViewItem)Tree.GetItemAt(p.X, p.Y);
		
		if(node != null)
		{
			ListViewItem draggedNode = (ListViewItem)e.Data.GetData(DataFormats.Serializable);
			TreeListViewItem newNode = node.ChildItems.Add(draggedNode.Text);
			newNode.Tag = draggedNode.Tag;
			draggedNode.Remove();
		}
		*/
	}
}
