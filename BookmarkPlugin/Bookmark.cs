using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;

namespace BookmarkPlugin
{
	class BookmarkNode : Node
	{
		private PieceBuffer.Range _Range;
		public PieceBuffer.Range Range
		{
			get { return _Range; }
			set 
			{
				if(_Range != null)
				{
					_Range.Start.Changed -= OnMarkChanged;
					_Range.End.Changed -= OnMarkChanged;
				}
				
				_Range = value;
				
				if(_Range != null)
				{
					_Range.Start.Changed += OnMarkChanged; 
					_Range.End.Changed += OnMarkChanged;
				}
			}
		}
		
		public long Position
		{
			get { return _Range.Start.Position; }
		}
		
		public string PositionString
		{
			get { return _IsLeaf ? _Range.Start.Position.ToString() : String.Empty; }
		}
		
		public long Length
		{
			get { return _Range.Length; }
		}
		
		public string LengthString
		{
			get { return IsLeaf ? _Range.Length.ToString() : String.Empty; }
		}
		
		private bool _IsLeaf;
		public override bool IsLeaf
		{
			get { return _IsLeaf; }
		}
		
		public BookmarkNode(bool isFolder, string text) : base(text)
		{
			_IsLeaf = !isFolder;
		}
		
		protected void OnMarkChanged(object sender, EventArgs e)
		{
			NotifyModel();
		}
	}
		
	
	public class BookmarkPanel : Panel
	{
		private IPluginHost 	Host;
		private TreeViewAdv 	Tree;
		private TreeModel		TreeModel;
		private TreeColumn		TreeColumnName;
		private TreeColumn		TreeColumnPosition;
		private TreeColumn		TreeColumnLength;
		private NodeStateIcon	NodeControlIcon;
		private NodeTextBox		NodeControlName;
		private NodeTextBox		NodeControlPosition;
		private NodeTextBox		NodeControlLength;
		private ToolStrip ToolBar;

		
		public BookmarkPanel(IPluginHost host)
		{
			Host = host;
			Host.AddMenuItem("Edit/Add Bookmark").Click += OnAddBookmark;
			
			Tree = new TreeViewAdv();
			TreeColumnName = new TreeColumn("Name", 100);
			TreeColumnPosition = new TreeColumn("Position", 100);
			TreeColumnLength = new TreeColumn("Length", 100);
			NodeControlIcon = new NodeStateIcon();
			NodeControlName = new NodeTextBox();
			NodeControlPosition = new NodeTextBox();
			NodeControlLength = new NodeTextBox();
			Tree.Name = "Tree";
			Tree.AllowDrop = true;
			Tree.UseColumns = true;
			Tree.FullRowSelect = true;
			Tree.Cursor = System.Windows.Forms.Cursors.Default;
			Tree.Columns.Add(TreeColumnName);
			Tree.Columns.Add(TreeColumnPosition);
			Tree.Columns.Add(TreeColumnLength);
			Tree.NodeControls.Add(NodeControlIcon);
			Tree.NodeControls.Add(NodeControlName);
			Tree.NodeControls.Add(NodeControlPosition);
			Tree.NodeControls.Add(NodeControlLength);
			NodeControlIcon.ParentColumn = TreeColumnName;
			NodeControlIcon.DataPropertyName = "Icon";
			NodeControlName.ParentColumn = TreeColumnName;
			NodeControlName.DataPropertyName = "Text";
			NodeControlPosition.ParentColumn = TreeColumnPosition;
			NodeControlPosition.DataPropertyName = "PositionString";
			NodeControlLength.ParentColumn = TreeColumnLength;
			NodeControlLength.DataPropertyName = "LengthString";

			Tree.Dock = DockStyle.Fill;
			TreeModel = new TreeModel();
			Tree.Model = TreeModel;
			Controls.Add(Tree);
			
			ToolBar = new ToolStrip();
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolStripItem item;
			item = ToolBar.Items.Add(Settings.Instance.Image("newfolder_16.png"));
			item.ToolTipText = "New Folder";
			item.Click += OnNewFolder;
			item = ToolBar.Items.Add(Settings.Instance.Image("unknown_op.png"));
			item.ToolTipText = "Add Bookmark";
			item.Click += OnAddBookmark;
			ToolBar.Items.Add(new ToolStripSeparator());
			item = ToolBar.Items.Add(Settings.Instance.Image("delete_16.png"));
			item.ToolTipText = "Delete";
			item.Click += OnDelete;
			ToolBar.Items.Add(new ToolStripSeparator());
			item = ToolBar.Items.Add(Host.Settings.Image("first_16.png"));
			item.ToolTipText = "Move First";
			item.Click += OnMoveFirst;
			item = ToolBar.Items.Add(Host.Settings.Image("prev_16.png"));
			item.ToolTipText = "Move Previous";
			item.Click += OnMovePrev;
			item = ToolBar.Items.Add(Host.Settings.Image("next_16.png"));
			item.ToolTipText = "Move Next";
			item.Click += OnMoveNext;
			item = ToolBar.Items.Add(Host.Settings.Image("last_16.png"));
			item.ToolTipText = "Move Last";
			item.Click += OnMoveLast;
			Controls.Add(ToolBar);
			
			Tree.NodeMouseDoubleClick += OnNodeDoubleClick;
			Tree.ItemDrag += OnTreeItemDrag;
			Tree.DragOver += OnTreeDragOver;
			Tree.DragDrop += OnTreeDragDrop;
		}
		
		public void OnAddBookmark(object sender, EventArgs e)
		{
			BookmarkNode n = new BookmarkNode(false, "New Bookmark");
			n.Range = Host.ActiveView.Document.Marks.AddRange(Host.ActiveView.Selection.Start / 8,
			                                                  Host.ActiveView.Selection.End / 8);
			TreeModel.Nodes.Add(n);
			Tree.SelectedNode = Tree.FindNode(new TreePath(n));
			NodeControlName.BeginEdit();
		}

		private void OnNewFolder(object sender, EventArgs e)
		{
			BookmarkNode n = new BookmarkNode(true, "New Folder");
			TreeModel.Nodes.Add(n);
			Tree.SelectedNode = Tree.FindNode(new TreePath(n));
			NodeControlName.BeginEdit();
		}

		private void DeleteChildNodes(BookmarkNode node)
		{
			foreach(BookmarkNode n in node.Nodes)
			{
				if(n.IsLeaf)
				{
					n.Range.Remove();
					n.Range = null;
				}
				else
					DeleteChildNodes(n);
			}
		}
		
		private void OnDelete(object sender, EventArgs e)
		{
			if(Tree.SelectedNode != null)
			{
				BookmarkNode node = Tree.GetPath(Tree.SelectedNode).LastNode as BookmarkNode;
				if(node.IsLeaf)
				{
					node.Range.Remove();
					node.Range = null;
				}
				else
					DeleteChildNodes(node);
				TreeModel.Nodes.Remove(node);
			}
		}
		
		private void OnMoveFirst(object sender, EventArgs e)
		{
		}
		
		private void OnMovePrev(object sender, EventArgs e)
		{
		}
		
		private void OnMoveNext(object sender, EventArgs e)
		{
			if(Tree.SelectedNode != null)
				Tree.SelectedNode = Tree.SelectedNode.NextNode;
		}
		
		private void OnMoveLast(object sender, EventArgs e)
		{
		}
		
		private void OnNodeDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
		{
			BookmarkNode node = Tree.GetPath(e.Node).LastNode as BookmarkNode;
			if(node.Range != null)
			{
				Host.ActiveView.Selection.Set(node.Range.Start.Position*8, node.Range.End.Position*8);
				Host.ActiveView.EnsureVisible(node.Range.Start.Position*8);
			}
		}
		
		protected void OnTreeItemDrag(object sender, ItemDragEventArgs e)
		{
			Tree.DoDragDropSelectedNodes(DragDropEffects.Move);
		}
		
		protected void OnTreeDragOver(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(typeof(TreeNodeAdv[])) && Tree.DropPosition.Node != null)
			{
				TreeNodeAdv[] nodes = e.Data.GetData(typeof(TreeNodeAdv[])) as TreeNodeAdv[];
				TreeNodeAdv parent = Tree.DropPosition.Node;
				if(Tree.DropPosition.Position != NodePosition.Inside)
					parent = parent.Parent;

				if(parent.IsLeaf)
				{
					e.Effect = DragDropEffects.None;
					return;
				}
					
				foreach(TreeNodeAdv node in nodes)
				{
					TreeNodeAdv p = parent;
					while(p != null)
					{
						if(node == p)
						{
							e.Effect = DragDropEffects.None;
							return;
						}
						p = p.Parent;
					}
				}

				e.Effect = e.AllowedEffect;
			}
		}
		
		protected void OnTreeDragDrop(object sender, DragEventArgs e)
		{
			Tree.BeginUpdate();

			TreeNodeAdv[] nodes = (TreeNodeAdv[])e.Data.GetData(typeof(TreeNodeAdv[]));
			Node dropNode = Tree.DropPosition.Node.Tag as Node;
			if(Tree.DropPosition.Position == NodePosition.Inside)
			{
				foreach(TreeNodeAdv n in nodes)
					(n.Tag as Node).Parent = dropNode;
				Tree.DropPosition.Node.IsExpanded = true;
			}
			else
			{
				Node parent = dropNode.Parent;
				Node nextItem = dropNode;
				if (Tree.DropPosition.Position == NodePosition.After)
					nextItem = dropNode.NextNode;

				foreach(TreeNodeAdv node in nodes)
					(node.Tag as Node).Parent = null;

				int index = -1;
				index = parent.Nodes.IndexOf(nextItem);
				foreach(TreeNodeAdv node in nodes)
				{
					Node item = node.Tag as Node;
					if(index == -1)
						parent.Nodes.Add(item);
					else
					{
						parent.Nodes.Insert(index, item);
						index++;
					}
				}
			}

			Tree.EndUpdate();
		}
	}

	public class BookmarkPlugin : IPlugin
	{
		string IPlugin.Name { get { return "Bookmarks"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new BookmarkPanel(host), "Bookmarks", host.Settings.Image("bookmark_16.png"), DefaultWindowPosition.Left, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
