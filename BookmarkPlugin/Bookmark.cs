using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;

namespace BookmarkPlugin
{
	public class BookmarkPanel : Panel
	{
		private IPluginHost Host;
		private TreeViewAdv Tree;
		private TreeModel	TreeModel;
		private NodeStateIcon NodeControlIcon;
		private NodeTextBox NodeControlName;
		private ToolStrip ToolBar;

		
		public BookmarkPanel(IPluginHost host)
		{
			Host = host;
			Host.AddMenuItem("Edit/Add Bookmark").Click += OnAddBookmark;
			
			Tree = new TreeViewAdv();
			NodeControlIcon = new NodeStateIcon();
			NodeControlName = new NodeTextBox();
			NodeControlName.DataPropertyName = "Text";
			Tree.Name = "Tree";
			Tree.NodeControls.Add(NodeControlIcon);
			Tree.NodeControls.Add(NodeControlName);
			Tree.UseColumns = true;
			Tree.Columns.Add(new TreeColumn("Name", 100));
			Tree.Columns.Add(new TreeColumn("Position", 100));
			Tree.Columns.Add(new TreeColumn("Length", 100));

			Tree.Dock = DockStyle.Fill;
			TreeModel = new TreeModel();
			Tree.Model = TreeModel;
			Controls.Add(Tree);
			
			ToolBar = new ToolStrip();
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolBar.Items.Add(Settings.Instance.Image("newfolder_16.png")).ToolTipText = "New Folder";
			ToolBar.Items.Add(Settings.Instance.Image("unknown_op.png")).ToolTipText = "Add Bookmark";
			ToolBar.Items.Add(new ToolStripSeparator());
			ToolBar.Items.Add(Settings.Instance.Image("delete_16.png")).ToolTipText = "Delete";
			ToolBar.Items.Add(new ToolStripSeparator());
			ToolBar.Items.Add(Host.Settings.Image("first_16.png"));
			ToolBar.Items.Add(Host.Settings.Image("prev_16.png"));
			ToolBar.Items.Add(Host.Settings.Image("next_16.png"));
			ToolBar.Items.Add(Host.Settings.Image("last_16.png"));
			Controls.Add(ToolBar);
			
			Tree.NodeMouseDoubleClick += OnNodeDoubleClick;
		}
		
		public void OnAddBookmark(object sender, EventArgs e)
		{
			Node n = new Node("New Bookmark");
			n.Tag = new PieceBuffer.Range(Host.ActiveView.Document.CreateMarkAbsolute(Host.ActiveView.Selection.Start/8),
			                              Host.ActiveView.Document.CreateMarkAbsolute(Host.ActiveView.Selection.End/8));
			TreeModel.Nodes.Add(n);
			Tree.SelectedNode = Tree.FindNode(new TreePath(n));
			NodeControlName.BeginEdit();
		}
		
		private void OnNodeDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
		{
			Node node = Tree.GetPath(e.Node).LastNode as Node;
			PieceBuffer.Range range = node.Tag as PieceBuffer.Range;
			Host.ActiveView.Selection.Set(range.Start.Position*8, range.End.Position*8);
		}
	}

	public class BookmarkPlugin : IPlugin
	{
		string IPlugin.Name { get { return "Bookmarks"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new BookmarkPanel(host), "Bookmarks", DefaultWindowPosition.Left, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
