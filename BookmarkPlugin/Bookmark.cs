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
			set { _Range = value; }
		}
		
		public long Position
		{
			get { return _Range.Start.Position; }
		}
		
		public long Length
		{
			get { return _Range.End.Position - _Range.Start.Position; }
		}
		
		public BookmarkNode(string text) : base(text)
		{
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
			Tree.UseColumns = true;
			Tree.Columns.Add(TreeColumnName);
			Tree.Columns.Add(TreeColumnPosition);
			Tree.Columns.Add(TreeColumnLength);
			Tree.NodeControls.Add(NodeControlIcon);
			Tree.NodeControls.Add(NodeControlName);
			Tree.NodeControls.Add(NodeControlPosition);
			Tree.NodeControls.Add(NodeControlLength);
			NodeControlIcon.ParentColumn = TreeColumnName;
			NodeControlName.ParentColumn = TreeColumnName;
			NodeControlName.DataPropertyName = "Text";
			NodeControlPosition.ParentColumn = TreeColumnPosition;
			NodeControlPosition.DataPropertyName = "Position";
			NodeControlLength.ParentColumn = TreeColumnLength;
			NodeControlLength.DataPropertyName = "Length";

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
		}
		
		public void OnAddBookmark(object sender, EventArgs e)
		{
			BookmarkNode n = new BookmarkNode("New Bookmark");
			n.Range = Host.ActiveView.Document.Marks.AddRange(Host.ActiveView.Selection.Start / 8,
			                                                  Host.ActiveView.Selection.End / 8);
			TreeModel.Nodes.Add(n);
			Tree.SelectedNode = Tree.FindNode(new TreePath(n));
			NodeControlName.BeginEdit();
		}

		private void OnNewFolder(object sender, EventArgs e)
		{
			BookmarkNode n = new BookmarkNode("New Folder");
			TreeModel.Nodes.Add(n);
			Tree.SelectedNode = Tree.FindNode(new TreePath(n));
			NodeControlName.BeginEdit();
		}
		
		private void OnDelete(object sender, EventArgs e)
		{
			if(Tree.SelectedNode != null)
			{
				BookmarkNode node = Tree.GetPath(Tree.SelectedNode).LastNode as BookmarkNode;
				// TODO: This might not be the same document!
				Host.ActiveView.Document.Marks.Remove(node.Range.Start);
				Host.ActiveView.Document.Marks.Remove(node.Range.End);
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
