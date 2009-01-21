using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;


namespace ChecksumPlugin
{
	public class ChecksumResult
	{
		public string Name;
		public byte[] Checksum;
		
		public ChecksumResult(string name, byte[] checksum)
		{
			Name = name;
			Checksum = checksum;
		}
	}
	
	class ChecksumTreeNode : Node
	{
		private string _Value;
		public string Value
		{
			get { return _Value; }
			set
			{
				if(_Value != value)
				{
					_Value = value;
					NotifyModel();
				}
			}
		}
		
		public override bool IsLeaf
		{
			get { return (Nodes.Count == 0); }
		}
		
		public ChecksumTreeNode(string text) : base(text)
		{
		}
	}
	
	class WorkerArgs
	{
		public List<string> Algorithms;
		public Document Document;
		public long StartPosition;
		public long EndPosition;
		
		public WorkerArgs(List<string> algorithms, Document doc, long start, long end)
		{
			Algorithms = algorithms;
			Document = doc;
			StartPosition = start;
			EndPosition = end;
		}
	}
	
	public class ResultWindow : Panel
	{
		IPluginHost Host;
		BackgroundWorker Worker;
		ProgressNotification Progress;

		private TreeViewAdv TreeView;
		private TreeModel	TreeModel;
		private NodeCheckBox NodeControlCheckBox;
		private NodeStateIcon NodeControlIcon;
		private NodeTextBox NodeControlName;
		private NodeTextBox NodeControlValue;
		private TreeColumn NameColumn;
		private TreeColumn ValueColumn;
		
		ToolStrip ToolBar = new ToolStrip();
		ToolStripButton ConfigButton;
		ToolStripComboBox SelectionComboBox;
		ToolStripTextBox SelectionTextBox;
		ToolStripButton RefreshButton;

		bool IgnoreCheckStateChange = false;
		
		Dictionary<string, Type> LocalAlgorithms = new Dictionary<string,Type>();

		
		public ResultWindow(IPluginHost host)
		{
			Host = host;
						
			LocalAlgorithms.Add("ADLER-32", typeof(Adler32Managed));
			foreach(CrcModel m in CrcManaged.Models)
				LocalAlgorithms.Add(m.Name, typeof(CrcManaged));
			LocalAlgorithms.Add("MD2", typeof(MD2Managed));
			LocalAlgorithms.Add("MD4", typeof(MD4Managed));
			foreach(SumModel m in SumManaged.Models)
				LocalAlgorithms.Add(m.Name, typeof(SumManaged));

			Worker = new BackgroundWorker();
			Worker.WorkerReportsProgress = true;
			Worker.WorkerSupportsCancellation = true;
			Worker.DoWork += OnDoWork;
			Worker.ProgressChanged += OnProgressChanged;
			Worker.RunWorkerCompleted += OnCompleted;

			
			TreeView = new TreeViewAdv();
			NameColumn = new TreeColumn("Algorithm", 200);
			ValueColumn = new TreeColumn("Value", 200);
			TreeView.UseColumns = true;
			TreeView.Columns.Add(NameColumn);
			TreeView.Columns.Add(ValueColumn);
			NodeControlCheckBox = new NodeCheckBox();
			NodeControlCheckBox.DataPropertyName = "CheckState";
			NodeControlCheckBox.ParentColumn = NameColumn;
			NodeControlCheckBox.CheckStateChanged += OnNodeControlCheckStateChanged;
			NodeControlIcon = new NodeStateIcon();
			NodeControlIcon.ParentColumn = NameColumn;
			NodeControlName = new NodeTextBox();
			NodeControlName.DataPropertyName = "Text";
			NodeControlName.ParentColumn = NameColumn;
			NodeControlValue = new NodeTextBox();
			NodeControlValue.DataPropertyName = "Value";
			NodeControlValue.ParentColumn = ValueColumn;
			TreeView.NodeControls.Add(NodeControlCheckBox);
			TreeView.NodeControls.Add(NodeControlIcon);
			TreeView.NodeControls.Add(NodeControlName);
			TreeView.NodeControls.Add(NodeControlValue);
			TreeView.Cursor = System.Windows.Forms.Cursors.Default;
			TreeView.FullRowSelect = true;
			TreeView.GridLineStyle = ((Aga.Controls.Tree.GridLineStyle)((Aga.Controls.Tree.GridLineStyle.Horizontal | Aga.Controls.Tree.GridLineStyle.Vertical)));
			TreeView.LineColor = System.Drawing.SystemColors.ControlDark;
			
			TreeView.Dock = DockStyle.Fill;
			TreeModel = new TreeModel();
			Controls.Add(TreeView);

			

			Node node = new ChecksumTreeNode("Checksum");
			TreeModel.Nodes.Add(node);
			node.Nodes.Add(new ChecksumTreeNode("ADLER-32"));
			Node crcNode = new ChecksumTreeNode("CRC");
			node.Nodes.Add(crcNode);
			foreach(CrcModel m in CrcManaged.Models)
			{
				string[] parts = m.Name.Split(new char[] {'/'}, 2);
				Node parent = null;
				if(parts.Length > 1)
				{
					List<Node> nodes = new List<Node>();
					foreach(Node n in crcNode.Nodes)
						if(n.Text == parts[0])
							nodes.Add(n);
					foreach(Node n in nodes)
					{
						if(n.IsLeaf)
						{
							crcNode.Nodes.Remove(n);
							parent = new ChecksumTreeNode(parts[0]);
							crcNode.Nodes.Add(parent);
							parent.Nodes.Add(new ChecksumTreeNode(n.Text));
						}
						else
							parent = n;
					}
				
					if(parent == null)
					{
						parent = new ChecksumTreeNode(parts[0]);
						crcNode.Nodes.Add(parent);
					}

					parent.Nodes.Add(new ChecksumTreeNode(m.Name));
				}
				else
					crcNode.Nodes.Add(new ChecksumTreeNode(m.Name));
			}
			
			node = new ChecksumTreeNode("Cryptographic Hash");
			TreeModel.Nodes.Add(node);
			node.Nodes.Add(new ChecksumTreeNode("MD2"));
			node.Nodes.Add(new ChecksumTreeNode("MD4"));
			node.Nodes.Add(new ChecksumTreeNode("MD5"));
			node.Nodes.Add(new ChecksumTreeNode("SHA1"));
			node.Nodes.Add(new ChecksumTreeNode("SHA256"));
			node.Nodes.Add(new ChecksumTreeNode("SHA384"));
			node.Nodes.Add(new ChecksumTreeNode("SHA512"));
			node.Nodes.Add(new ChecksumTreeNode("RIPEMD160"));
//			foreach(SumModel m in SumManaged.Models)
//				Algorithms.Add(String.Format("Checksum/SUM/{0}", m.Name));

			TreeView.Model = TreeModel;
			

			ConfigButton = new ToolStripButton(Host.Settings.Image("options_16.png"));
			ConfigButton.ToolTipText = "Configure";
			ToolBar.Items.Add(ConfigButton);
			ToolBar.Items.Add(new ToolStripSeparator());
			
			SelectionComboBox = new ToolStripComboBox();
			SelectionComboBox.Items.Add("Selection");
			SelectionComboBox.Items.Add("Document");
			SelectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			SelectionComboBox.SelectedIndex = 0;
			SelectionComboBox.Overflow = ToolStripItemOverflow.Never;
			ToolBar.Items.Add(SelectionComboBox);
			
			RefreshButton = new ToolStripButton(Host.Settings.Image("go_16.png"));
			RefreshButton.ToolTipText = "Calculate";
			RefreshButton.Click += OnCalculate;
			RefreshButton.Overflow = ToolStripItemOverflow.Never;
			ToolBar.Items.Add(RefreshButton);
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			Controls.Add(ToolBar);
		}
		
		
		void FindSelectedAlgorithms(System.Collections.ObjectModel.Collection<Node> nodes, List<string> list)
		{
			foreach(Node n in nodes)
			{
				if(!n.IsLeaf)
					FindSelectedAlgorithms(n.Nodes, list);
				else if(n.CheckState == CheckState.Checked)
					list.Add(n.Text);
			}
		}
		
		CheckState UpdateCheckStates(Node node)
		{
			bool haveChecked = false;
			bool haveUnchecked = false;
			bool haveIndeterminate = false;
			foreach(Node n in node.Nodes)
			{
				CheckState state;
				if(!n.IsLeaf)
					state = UpdateCheckStates(n);
				else
					state = n.CheckState;
				if(state == CheckState.Checked)
					haveChecked = true;
				else if(state == CheckState.Unchecked)
					haveUnchecked = true;
				else
					haveIndeterminate = true;
			}
			
			if(haveIndeterminate || (haveChecked && haveUnchecked))
				node.CheckState = CheckState.Indeterminate;
			else if(haveChecked)
				node.CheckState = CheckState.Checked;
			else
				node.CheckState = CheckState.Unchecked;
			
			return node.CheckState;
		}

		void SetChildCheckStates(Node node)
		{
			foreach(Node n in node.Nodes)
			{
				n.CheckState = node.CheckState;
				if(!n.IsLeaf)
					SetChildCheckStates(n);
			}
		}
		
		protected void OnNodeControlCheckStateChanged(object sender, TreePathEventArgs e)
		{
			if(IgnoreCheckStateChange)
				return;
			
			IgnoreCheckStateChange = true;

			Node node = e.Path.LastNode as Node;
			if(!node.IsLeaf)
				SetChildCheckStates(node);
			UpdateCheckStates(e.Path.FirstNode as Node);
			
			IgnoreCheckStateChange = false;
		}
		
		List<string> FindSelectedAlgorithms()
		{
			List<string> list = new List<string>();
			FindSelectedAlgorithms(TreeModel.Nodes, list);
			return list;
		}
		
		void FindTreeNodes(System.Collections.ObjectModel.Collection<Node> nodes, List<Node> list, string name)
		{
			foreach(Node n in nodes)
			{
				if(!n.IsLeaf)
					FindTreeNodes(n.Nodes, list, name);
				else if(n.Text == name)
					list.Add(n);
			}
		}
		
		List<Node> FindTreeNodes(string name)
		{
			List<Node> list = new List<Node>();
			FindTreeNodes(TreeModel.Nodes, list, name);
			return list;
		}

		void ClearValues(System.Collections.ObjectModel.Collection<Node> nodes)
		{
			foreach(ChecksumTreeNode n in nodes)
			{
				if(!n.IsLeaf)
					ClearValues(n.Nodes);
				else
					n.Value = String.Empty;
			}
		}
		void ClearValues()
		{
			ClearValues(TreeModel.Nodes);
		}
		
		public void OnCalculate(object sender, EventArgs e)
		{
			ClearValues();
			
			if(Worker.IsBusy)
			{
				RefreshButton.Enabled = false;
				Worker.CancelAsync();
				return;
			}
			
			Progress = new ProgressNotification();
			Host.ProgressNotifications.Add(Progress);
			
			
			List<string> enabledAlgorithms = FindSelectedAlgorithms();
			
			RefreshButton.Image = Host.Settings.Image("stop_16.png");
			if(SelectionComboBox.SelectedIndex != 0)
			{
				Worker.RunWorkerAsync(new WorkerArgs(enabledAlgorithms, 
				                                     Host.ActiveView.Document,
				                                     -1, 
				                                     -1));
			}
			else
			{
				Worker.RunWorkerAsync(new WorkerArgs(enabledAlgorithms, 
				                                     Host.ActiveView.Document, 
				                                     Host.ActiveView.Selection.Start, 
				                                     Host.ActiveView.Selection.End));
			}
		}
		
		public void OnDoWork(object sender, DoWorkEventArgs e)
		{
			const int BLOCK_SIZE = 4096*1024;
			WorkerArgs args = (WorkerArgs)e.Argument;
			
			Dictionary<string, System.Security.Cryptography.HashAlgorithm> algos = new Dictionary<string, System.Security.Cryptography.HashAlgorithm>();
			foreach(string algo in args.Algorithms)
			{
				Type type;
				System.Security.Cryptography.HashAlgorithm ha;
				
				if(LocalAlgorithms.TryGetValue(algo, out type))
				{
					if(algo.StartsWith("CRC") || algo.StartsWith("SUM"))
						ha = Activator.CreateInstance(type, new object[] {algo}) as System.Security.Cryptography.HashAlgorithm;
					else
						ha = Activator.CreateInstance(type) as System.Security.Cryptography.HashAlgorithm;
				}
				else
					ha = System.Security.Cryptography.CryptoConfig.CreateFromName(algo) as System.Security.Cryptography.HashAlgorithm;
				
				ha.Initialize();
				algos.Add(algo, ha);
			}

			// TODO: Need thread safe method of accessing the buffer
			// TODO: Need to know if the buffer changes while we're processing
			//       maybe we could lock the buffer so it's read only until we're finished?
			long total;
			long offset;
			long lastReportTime = System.Environment.TickCount;
			long lastReportBytes = 0;
			byte[] bytes = new byte[BLOCK_SIZE];
			
			if(args.StartPosition == -1 && args.EndPosition == -1)
			{
				total = args.Document.Length;
				offset = 0;
			}
			else
			{
				Console.WriteLine("Selection: S: " + args.StartPosition + ", E: " + args.EndPosition);
				total = (args.EndPosition - args.StartPosition) / 8;
				offset = args.StartPosition / 8;
			}
			
			long len = total;			
			while(len > 0)
			{
				int partLen = BLOCK_SIZE > len ? (int)len : BLOCK_SIZE;
				args.Document.GetBytes(offset, bytes, partLen);

				foreach(KeyValuePair<string, System.Security.Cryptography.HashAlgorithm> ha in algos)
					ha.Value.TransformBlock(bytes, 0, partLen, bytes, 0);
				
				len -= partLen;
				offset += partLen;
				
				if(System.Environment.TickCount > lastReportTime + 1000)
				{
					float MBps = (float)(offset - lastReportBytes) / (1024 * 1024);
					MBps /= (float)(System.Environment.TickCount - lastReportTime) / 1000;
					Worker.ReportProgress((int)((100.0 / total) * (total - len)), MBps);
					lastReportBytes = offset;
					lastReportTime = System.Environment.TickCount;
				}
				
				if(Worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}
			}

			foreach(KeyValuePair<string, System.Security.Cryptography.HashAlgorithm> ha in algos)
				ha.Value.TransformFinalBlock(bytes, 0, 0);
			
			List<ChecksumResult> results = new List<ChecksumResult>();
			foreach(KeyValuePair<string, System.Security.Cryptography.HashAlgorithm> ha in algos)
				results.Add(new ChecksumResult(ha.Key, ha.Value.Hash));
			e.Result = results;
		}
		
		public void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Progress.Update(e.ProgressPercentage, String.Format("Calculating checksums...  ({0:0.##} MB/s)", (float)e.UserState));
		}
		
		public void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			Host.ProgressNotifications.Remove(Progress);
			Progress = null;
			
			if(e.Cancelled == false)
			{
				List<ChecksumResult> results = e.Result as List<ChecksumResult>;
				
				foreach(ChecksumResult result in results)
				{
					StringBuilder hash = new StringBuilder();
					foreach(byte b in result.Checksum)
						hash.Append(b.ToString("X2"));
					List<Node> nodes = FindTreeNodes(result.Name);
					if(nodes.Count == 1)
					{
						((ChecksumTreeNode)nodes[0]).Value = hash.ToString();
						TreeView.EnsureVisible(TreeView.FindNode(TreeModel.GetPath(nodes[0])));
					}
				}
			}
			
			RefreshButton.Image = Host.Settings.Image("go_16.png");
			RefreshButton.Enabled = true;
		}
	}
	
	public class Checksum : IPlugin
	{
		string IPlugin.Name { get { return "Checksum"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		IPluginHost Host;
		
		void IPlugin.Initialize(IPluginHost host)
		{
			Host = host;
			Host.AddWindow(new ResultWindow(Host), "Checksum", Host.Settings.Image("checksum_16.png"), DefaultWindowPosition.BottomRight, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
