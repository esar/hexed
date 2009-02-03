using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StructurePlugin
{
	class StructureTreeModel : Aga.Controls.Tree.ITreeModel
	{
		private Record Structure;
			
		public StructureTreeModel(Record structure)
		{
			Structure = structure;
		}

		public System.Collections.IEnumerable GetChildren(Aga.Controls.Tree.TreePath treePath)
		{
			List<Record> items = new List<Record>();
			
			if(treePath.IsEmpty())
			{
				foreach(Record r in Structure._Children)
					items.Add(r);
			}
			else
			{
				Record parent = (Record)treePath.LastNode; 
				if(parent.ArrayLength > 1)
				{
					Record record = parent; 
					if(record.ArrayElements != null)
					{
						foreach(Record r in record.ArrayElements)
							items.Add(r);
					}
					else
					{
						for(int i = 0; i < (int)parent.ArrayLength; ++i)
							items.Add(record.GetArrayElement((long)i));
					}
				}
				else
				{
					foreach(Record r in parent._Children)
						items.Add(r);
				}
			}
			
			return items;
		}
		
		public bool IsLeaf(Aga.Controls.Tree.TreePath treePath)
		{
			return ((Record)treePath.LastNode)._Children.Count == 0 &&
				   ((Record)treePath.LastNode).ArrayLength <= 1;
		}
		
		public event EventHandler<Aga.Controls.Tree.TreeModelEventArgs> NodesChanged;
		internal void OnNodesChanged(Record item)
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

		public List<Record> SelectedRecords
		{
			get 
			{
				List<Record> list = new List<Record>();
				
				foreach(Aga.Controls.Tree.TreeNodeAdv n in _TreeView.SelectedNodes)
					list.Add((Record)n.Tag);
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

			_NodeControlValue.DataPropertyName = "StringValue";
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
				_TreeView.Model = null;
				
				ProgressNotification progress = new ProgressNotification(); 
				Host.ProgressNotifications.Add(progress);
				
				progress.Update(0, "Compiling structure definition...");
				Application.DoEvents();
				StructureDefinitionCompiler compiler = new StructureDefinitionCompiler();
				Record structure = compiler.Parse(dlg.FileName);
				
				progress.Update(50, "Applying structure definition...");
				Application.DoEvents();
				if(structure != null)
				{
					long pos = 0;
					structure.ApplyStructure(Host.ActiveView.Document, ref pos, true);
					structure.Dump();
					_TreeView.Model = new StructureTreeModel(structure);
				}
				
				if(Host.ActiveView.Document.MetaData.ContainsKey("Structure"))
					Host.ActiveView.Document.MetaData["Structure"] = structure;
				else
					Host.ActiveView.Document.MetaData.Add("Structure", structure);
								
				Host.ProgressNotifications.Remove(progress);
			}
		}

		protected void OnSelectionChanged(object sender, EventArgs e)
		{
			List<Record> records = SelectedRecords;
			if(records.Count == 0)
				return;
			
			Host.ActiveView.ClearHighlights();
			
			if(Host.ActiveView != null)
			{
				Record record = records[0];

				int level = 0;
				while(record != null)
				{
					HexView.SelectionRange sel = new HexView.SelectionRange(Host.ActiveView);
					sel.Set((long)record.Position, (long)(record.Position + (record.Length * record.ArrayLength)));
						
					if(level++ == 0)
					{
						sel.BackColor = Color.FromArgb(255, 200, 255, 200);
						sel.BorderColor = Color.LightGreen;
						sel.BorderWidth = 1;
					}
					else
					{
						sel.BackColor = Color.FromArgb(64, 192,192,192);
						sel.BorderColor = Color.FromArgb(128, 192,192,192);
						sel.BorderWidth = 1;
					}
						
					Host.ActiveView.AddHighlight(sel);
					record = record.Parent;
				}
				Host.ActiveView.EnsureVisible((long)records[0].Position);
			}
		}
	}
	
	public class StructurePlugin : IPlugin
	{
		string IPlugin.Name { get { return "Structure"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		IPluginHost Host;
		
		void IPlugin.Initialize(IPluginHost host)
		{
			Host = host;
			Host.AddWindow(new StructurePanel(host), "Structure", Host.Settings.Image("structure_16.png"), DefaultWindowPosition.Left, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
