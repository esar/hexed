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
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;


// TODO: When HexView selection changes, highlight relevant bookmark node or clear selection if not in a bookmark any more


namespace BookmarkPlugin
{
	class Bookmarks : TreeModel {}
	
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
		private Document		Document;
		private TreeViewAdv 	Tree;
		private Bookmarks		Bookmarks;
		private TreeColumn		TreeColumnName;
		private TreeColumn		TreeColumnPosition;
		private TreeColumn		TreeColumnLength;
		private NodeStateIcon	NodeControlIcon;
		private NodeTextBox		NodeControlName;
		private NodeTextBox		NodeControlPosition;
		private NodeTextBox		NodeControlLength;
		private DocumentRangeIndicator RangeIndicator;
		private ToolStrip ToolBar;

		
		public BookmarkPanel(IPluginHost host)
		{
			Host = host;
			
			Host.Commands.Add("Bookmark/Add Bookmark", "Adds a new bookmark", "Add Bookmark",
			                  Host.Settings.Image("bookmark_16.png"),
			                  new Keys[] { Keys.Control | Keys.B },
			                  OnAddBookmark);
			Host.Commands.Add("Bookmark/New Folder", "Creates a new bookmark folder", "New Folder",
			                  Host.Settings.Image("newfolder_16.png"),
			                  null,
			                  OnNewFolder);
			Host.Commands.Add("Bookmark/Delete", "Deletes the selected bookmark or folder", "Delete",
			                  Host.Settings.Image("delete_16.png"),
			                  null,
			                  OnDelete);
			Host.Commands.Add("Bookmark/Move First", "Jumps to the first bookmark", "Move First",
			                  Host.Settings.Image("first_16.png"),
			                  null,
			                  OnMoveFirst);
			Host.Commands.Add("Bookmark/Move Previous", "Jumps to the previous bookmark", "Move Previous",
			                  Host.Settings.Image("prev_16.png"),
			                  new Keys[] { Keys.Control | Keys.Up },
			                  OnMovePrev);
			Host.Commands.Add("Bookmark/Move Next", "Jumps to the next bookmark", "Move Next",
			                  Host.Settings.Image("next_16.png"),
			                  new Keys[] { Keys.Control | Keys.Down },
			                  OnMoveNext);
			Host.Commands.Add("Bookmark/Move Last", "Jumps to the last bookmark", "Move Last",
			                  Host.Settings.Image("last_16.png"),
			                  null,
			                  OnMoveLast);
			
			Host.AddMenuItem(Menus.Main, "Edit", Host.CreateMenuItem("Bookmark/Add Bookmark"));
			Host.ActiveViewChanged += OnActiveViewChanged;
			
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
			Controls.Add(Tree);
			
			RangeIndicator = new DocumentRangeIndicator();
			RangeIndicator.Dock = DockStyle.Left;
			Controls.Add(RangeIndicator);
			
			ToolBar = new ToolStrip();
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/New Folder"));
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/Add Bookmark"));
			ToolBar.Items.Add(new ToolStripSeparator());
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/Delete"));
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/Move First"));
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/Move Previous"));
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/Move Next"));
			ToolBar.Items.Add(Host.CreateToolButton("Bookmark/Move Last"));
			Controls.Add(ToolBar);
			
			Tree.SelectionChanged += OnTreeSelectionChanged;
			Tree.ItemDrag += OnTreeItemDrag;
			Tree.DragOver += OnTreeDragOver;
			Tree.DragDrop += OnTreeDragDrop;
		}
		
		protected void PopulateRangeIndicator(System.Collections.ObjectModel.Collection<Node> nodes)
		{
			foreach(BookmarkNode n in nodes)
			{
				if(n.IsLeaf)
					RangeIndicator.Ranges.Add(n.Range.Start.Position, n.Range.End.Position);
				else
					PopulateRangeIndicator(n.Nodes);
			}
		}
		
		protected void OnActiveViewChanged(object sender, EventArgs e)
		{
			Bookmarks  = null;
			Document = null;
			if(Host.ActiveView != null)
				Document = Host.ActiveView.Document;
			
			if(Document != null)
			{
				object o;
				if(!Document.MetaData.TryGetValue("Bookmarks", out o))
				{
					Bookmarks = new Bookmarks();
					Document.MetaData.Add("Bookmarks", Bookmarks);
				}
				else
					Bookmarks = (Bookmarks)o;
				
				RangeIndicator.DocumentLength = Document.Length;
				RangeIndicator.Ranges.Clear();
				RangeIndicator.SelectedRange = null;
				PopulateRangeIndicator(Bookmarks.Nodes);
			}
				
			Tree.Model = Bookmarks;
		}
		
		public void OnAddBookmark(object sender, EventArgs e)
		{
			Host.BringToFront(this);
			BookmarkNode n = new BookmarkNode(false, "New Bookmark");
			n.Range = Host.ActiveView.Document.Marks.AddRange(Host.ActiveView.Selection.Start / 8,
			                                                  Host.ActiveView.Selection.End / 8);
			RangeIndicator.DocumentLength = Host.ActiveView.Document.Length;
			RangeIndicator.Ranges.Add(n.Range.Start.Position, n.Range.End.Position);
			Bookmarks.Nodes.Add(n);
			Tree.SelectedNode = Tree.FindNode(new TreePath(n));
			NodeControlName.BeginEdit();
		}

		private void OnNewFolder(object sender, EventArgs e)
		{
			Host.BringToFront(this);
			BookmarkNode n = new BookmarkNode(true, "New Folder");
			Bookmarks.Nodes.Add(n);
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
			Host.BringToFront(this);
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
				Bookmarks.Nodes.Remove(node);
			}
		}
		
		private void OnMoveFirst(object sender, EventArgs e)
		{
			TreeNodeAdv root = Tree.FindNode(new TreePath());
			if(root.Children.Count > 0)
			{
				TreeNodeAdv n = root.Children[0];
				if(!n.IsLeaf)
					n = GetNextBookmarkNode(n);
				if(n != null)
					Tree.SelectedNode = n;
			}
		}
		
		private TreeNodeAdv GetPrevBookmarkNode(TreeNodeAdv node)
		{
			TreeNodeAdv n = node;
			
			do
			{
				if(n.Index > 0)
					n = n.Parent.Children[n.Index - 1];
				else
					n = n.Parent;
				
				if(n != null && !n.IsLeaf)
				{
					TreeNodeAdv next = GetNextBookmarkNode(n);
					if(next != null && next != node)
					{
						TreeNodeAdv next2;
						while((next2 = GetNextBookmarkNode(next)) != node)
							next = next2;
						n = next;
					}
				}
				
			} while(n != null && !n.IsLeaf);

			return n;
		}
		
		private void OnMovePrev(object sender, EventArgs e)
		{
			if(Tree.SelectedNode != null)
			{
				TreeNodeAdv n = GetPrevBookmarkNode(Tree.SelectedNode);
				if(n != null)
					Tree.SelectedNode = n;
			}
		}
		
		private TreeNodeAdv GetNextBookmarkNode(TreeNodeAdv n)
		{
			do
			{
				if(n.Children.Count > 0)
					n = n.Children[0];
				else if(n.NextNode != null)
					n = n.NextNode;
				else
				{
					do
					{
						n = n.Parent;
						
					} while(n != null && n.NextNode == null);
					
					if(n != null)
						n = n.NextNode;
				}
				
			} while(n != null && !n.IsLeaf);

			return n;
		}
		
		private void OnMoveNext(object sender, EventArgs e)
		{
			if(Tree.SelectedNode != null)
			{
				TreeNodeAdv n = GetNextBookmarkNode(Tree.SelectedNode);
				if(n != null)
					Tree.SelectedNode = n;
			}
		}
		
		private void OnMoveLast(object sender, EventArgs e)
		{
			TreeNodeAdv node = Tree.FindNode(new TreePath());
			while(node != null && node.Children.Count > 0)
				node = node.Children[node.Children.Count - 1];
			if(node != null && !node.IsLeaf)
				node = GetPrevBookmarkNode(node);
			if(node != null)
				Tree.SelectedNode = node;
		}
		
		private void OnTreeSelectionChanged(object sender, EventArgs e)
		{
			if(Tree.SelectedNode != null)
			{
				BookmarkNode node = Tree.GetPath(Tree.SelectedNode).LastNode as BookmarkNode;
				if(node.Range != null)
				{
					RangeIndicator.SelectedRange = new DocumentRange(node.Range.Start.Position, node.Range.End.Position);
					Host.ActiveView.Selection.Set(node.Range.Start.Position*8, node.Range.End.Position*8);
					Host.ActiveView.EnsureVisible(node.Range.Start.Position*8);
				}
				else
					RangeIndicator.SelectedRange = null;
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
		public Image Image { get { return Settings.Instance.Image("bookmark_16.png"); } }
		public string Name { get { return "Bookmarks"; } }
		public string Description { get { return "Provides a mechanism for bookmarking sections of a document"; } }
		public string Author { get { return "Stephen Robinson"; } }
		public string Version { get { return "1.0"; } }
		public string Copyright { get { return "(c)2008 Stephen Robinson"; } }
		public string Url { get { return "http://www.esar.org.uk/"; } }

		public void Initialize(IPluginHost host)
		{
			host.AddWindow(new BookmarkPanel(host), "Bookmarks", host.Settings.Image("bookmark_16.png"), DefaultWindowPosition.Left, true);
		}
		
		public void Dispose()
		{
		}
	}
}
