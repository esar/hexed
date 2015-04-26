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
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.CodeDom.Compiler;

namespace StructurePlugin
{
	class StructureWorker : BackgroundWorker
	{
		protected Document _Document;
		public Document Document { get { return _Document; } }
		protected string _DefinitionFilename;
		public string DefinitionFilename { get { return _DefinitionFilename; } }
		protected long _StartPosition;
		public long StartPosition { get { return _StartPosition; } }
		protected long _EndPosition;
		public long EndPosition { get { return _EndPosition; } }
		protected ProgressNotification _Progress;
		public ProgressNotification Progress { get { return _Progress; } }
		
		public StructureWorker(Document document, string definitionFilename, long startPosition, long endPosition, ProgressNotification progress)
		{
			_Document = document;
			_DefinitionFilename = definitionFilename;
			_StartPosition = startPosition;
			_EndPosition = endPosition;
			_Progress = progress;
			WorkerReportsProgress = true;
			WorkerSupportsCancellation = true;
		}
		
		protected override void OnDoWork(DoWorkEventArgs e)
		{
			base.OnDoWork(e);

			ReportProgress(0, "Compiling structure definition...");
			StructureDefinitionCompiler compiler = new StructureDefinitionCompiler();
			CompilerResult result = compiler.Parse(DefinitionFilename);
			
			ReportProgress(50, "Applying structure definition...");
			if(result.Structure != null)
			{
				long pos = StartPosition;
				try
				{
					result.Structure.ApplyStructure(Document, ref pos, true);
				}
				catch(Exception ex)
				{
					result.Errors.Add(new CompilerError(null, 0, 0, "", ex.ToString()));
				}
				//result.Structure.Dump();
			}

			e.Result = result;
		}

		protected override void OnProgressChanged(ProgressChangedEventArgs e)
		{
			Progress.Update(e.ProgressPercentage, (string)e.UserState);
			base.OnProgressChanged(e);
		}

		protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
		{
			int index = 0;
			Record root;
			Record old = null;
			Record record = ((CompilerResult)e.Result).Structure;

			if(!Document.MetaData.ContainsKey("Structure"))
			{
				root = new RootRecord();
				Document.MetaData.Add("Structure", root);
			}
			else
				root = (Record)Document.MetaData["Structure"];

			foreach(Record r in root._Children)
			{
				if(r.Position == record.Position && r.Type == record.Type)
				{
					old = r;
					break;
				}
				else if(r.Position > record.Position)
					break;

				++index;
			}

			record.BaseName = record.Type;
			root._Children.Insert(index, record);
			if(old != null)
				root._Children.Remove(old);
			
			base.OnRunWorkerCompleted(e);
		}
	}
	
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
		protected StructureWorker Worker;

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
			Host.ActiveViewChanged += OnActiveViewChanged;
			
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
			_NodeControlName.EditEnabled = false;

			_NodeControlValue.DataPropertyName = "StringValue";
			_NodeControlValue.ParentColumn = _TreeColumnValue;
			_NodeControlValue.EditEnabled = true;
			
			_NodeControlType.DataPropertyName = "Type";
			_NodeControlType.ParentColumn = _TreeColumnType;
			_NodeControlType.EditEnabled = false;
			
			_TreeView.Dock = DockStyle.Fill;
			Controls.Add(_TreeView);
			
			ToolBar = new ToolStrip();
			ToolBar.Dock = DockStyle.Top;
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			Controls.Add(ToolBar);

			ToolBar.Items.Add(Settings.Instance.Image("open_16.png")).Click += new EventHandler(OnOpenStructureDef);
		}

		protected void OnActiveViewChanged(object sender, EventArgs e)
		{
			if(Host.ActiveView != null)
			{
				object obj;
				if(Host.ActiveView.Document.MetaData.TryGetValue("Structure", out obj))
				{
					_TreeView.Model = new StructureTreeModel((Record)obj);
				}
				else
					_TreeView.Model = null;
			}
			else
				_TreeView.Model = null;
		}
		
		protected void OnOpenStructureDef(object sender, EventArgs e)
		{
			ApplyStructureDialog dlg = new ApplyStructureDialog(Host.ActiveView, Host.Settings.BasePath + "/StructureDefinitions");
			
			if(dlg.ShowDialog() == DialogResult.OK)
			{
				_TreeView.Model = null;
				
				ProgressNotification progress = new ProgressNotification(); 
				Host.ProgressNotifications.Add(progress);
				Worker = new StructureWorker(Host.ActiveView.Document, dlg.DefinitionPath, dlg.StartPosition, dlg.EndPosition, progress);
				Worker.RunWorkerCompleted += OnWorkerCompleted;
				Worker.RunWorkerAsync();
			}
		}

		protected void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			CompilerResult result = (CompilerResult)e.Result;
			
			Host.ProgressNotifications.Remove(((StructureWorker)sender).Progress);
			if(Host.ActiveView != null && Host.ActiveView.Document == ((StructureWorker)sender).Document && result.Structure != null)
			{
				object obj;
				if(Host.ActiveView.Document.MetaData.TryGetValue("Structure", out obj))
				{
					_TreeView.Model = new StructureTreeModel((Record)obj);
				}
				else
					_TreeView.Model = null;
			}
			
			if(result.Errors.Count > 0)
			{
				CompileErrorDialog dlg = new CompileErrorDialog();
				dlg.Errors = result.Errors;
				dlg.StartPosition = FormStartPosition.CenterParent;
				dlg.ShowDialog();
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
					sel.Set((long)record.Position, (long)(record.Position + record.Length));
						
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
		public Image Image { get { return Settings.Instance.Image("structure_16.png"); } }
		public string Name { get { return "Structure"; } }
		public string Description { get { return "Displays a documents structure based on structure definition scripts"; } }
		public string Author { get { return "Stephen Robinson"; } }
		public string Version { get { return "1.0"; } }
		public string Copyright { get { return "(c)2008 Stephen Robinson"; } }
		public string Url { get { return "http://www.esar.org.uk/"; } }

		public void Initialize(IPluginHost host)
		{
			host.AddWindow(new StructurePanel(host), "Structure", host.Settings.Image("structure_16.png"), DefaultWindowPosition.Left, true);
		}
		
		public void Dispose()
		{
		}
	}
}
