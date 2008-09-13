using System;
using System.Drawing;
using System.Windows.Forms;


class StructurePanel : Panel
{
	public TreeListView	Tree;
	protected ToolStrip ToolBar;
	protected IPluginHost Host;
	
	public StructurePanel(IPluginHost host)
	{
		Host = host;
		
		Tree = new TreeListView();
		Tree.Dock = DockStyle.Fill;
		Controls.Add(Tree);
		Tree.LabelEdit = true;
		Tree.AllowDrop = true;
		Tree.Columns.Add("Name");
		Tree.Columns.Add("Value");
		Tree.Columns.Add("Type");
		Tree.FullRowSelect = true;
		Tree.GridLines = true;

		Tree.ItemDrag += new ItemDragEventHandler(OnItemDrag);
		Tree.DragEnter += new DragEventHandler(OnDragEnter);
		Tree.DragOver += new DragEventHandler(OnDragOver);
		Tree.DragDrop += new DragEventHandler(OnDragDrop);
		
		ToolBar = new ToolStrip();
		ToolBar.Dock = DockStyle.Top;
		ToolBar.GripStyle = ToolStripGripStyle.Hidden;
		Controls.Add(ToolBar);

		ToolBar.Items.Add(Settings.Instance.Image("open_16.png")).Click += new EventHandler(OnOpenStructureDef);
	}

	protected void AddRecords(TreeListViewChildItemCollection nodes, Record record, ref int count)
	{
		TreeListViewItem node;
		
		if(record.ArrayLength > 1)
		{
			node = nodes.Add(record.Name + "[" + record.ArrayLength + "]");
			if(record.ArrayElements != null)
			{
				int i = 0;
				foreach(Record r in record.ArrayElements)
				{
					TreeListViewItem n = node.ChildItems.Add(record.Name + "[" + (i++) + "]");
					n.Tag = r;
					for(int j = 0; j < r._Children.Count; ++j)
						AddRecords(n.ChildItems, r._Children[j], ref count);
				}
			}
		}
		else
			node = nodes.Add(record.Name);
		node.BackColor = record.BackColor;
		node.SubItems.Add(record.ToString());
		node.SubItems.Add(record.GetType().ToString());
//		if((count++ & 1) == 1 && record.BackColor == Color.Transparent)
//			node.BackColor = Color.Gainsboro;
		node.Tag = record;
		for(int i = 0; i < record._Children.Count; ++i)
			AddRecords(node.ChildItems, record._Children[i], ref count);
	}
	
	protected void OnOpenStructureDef(object sender, EventArgs e)
	{
		FileDialog dlg = new OpenFileDialog();
		
		
		dlg.Title = "Open Structure Definition";
		if(dlg.ShowDialog() == DialogResult.OK)
		{
			Tree.Items.Clear();
			
			Host.ActiveView.Document.ApplyStructureDefinition(dlg.FileName);
			
			int count = 1;
			TreeListViewItem item = Tree.Items.Add("Root");
			for(int i = 0; i < Host.ActiveView.Document.Structure._Children.Count; ++i)
				AddRecords(item.ChildItems, Host.ActiveView.Document.Structure._Children[i], ref count);
		}
	}
	
	protected void OnItemDrag(object sender, ItemDragEventArgs e)
	{
		Tree.DoDragDrop((ListViewItem)e.Item, DragDropEffects.All);
	}

	protected void OnDragEnter(object sender, DragEventArgs e)
	{
		e.Effect = DragDropEffects.All;
	}

	protected void OnDragOver(object sender, DragEventArgs e)
	{
		Point p = Tree.PointToClient(new Point(e.X, e.Y));
		ListViewItem node = Tree.GetItemAt(p.X, p.Y);

		if(node != null)
		{
			Tree.SelectedItems.Clear();
			node.Selected = true;
			e.Effect = DragDropEffects.All;
		}
		else
			e.Effect = 0;
	}

	protected void OnDragDrop(object sender, DragEventArgs e)
	{
		Point p = Tree.PointToClient(new Point(e.X, e.Y));
		TreeListViewItem node = (TreeListViewItem)Tree.GetItemAt(p.X, p.Y);
		
		if(node != null)
		{
			ListViewItem draggedNode = (ListViewItem)e.Data.GetData(DataFormats.Serializable);
			TreeListViewItem newNode = node.ChildItems.Add(draggedNode.Text);
			newNode.Tag = draggedNode.Tag;
			draggedNode.Remove();
		}
	}
}
