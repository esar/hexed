using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

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
	
	public class ResultWindow : Panel
	{
		IPluginHost Host;
		BackgroundWorker Worker;

		ListView ResultList = new ListView();
		
		ToolStrip ToolBar = new ToolStrip();
		ToolStripButton ConfigButton;
		ToolStripComboBox SelectionComboBox;
		ToolStripTextBox SelectionTextBox;
		ToolStripButton RefreshButton;
		StatusStrip StatusBar = new StatusStrip();
		ToolStripProgressBar ProgressBar = new ToolStripProgressBar();
		ToolStripLabel ProgressLabel = new ToolStripLabel();

		List<string> Algorithms = new List<string>();
		Dictionary<string, Type> LocalAlgorithms = new Dictionary<string,Type>();

		
		public ResultWindow(IPluginHost host)
		{
			Host = host;
		
			Algorithms.Add("ADLER32");
			foreach(CrcModel m in CrcManaged.Models)
				Algorithms.Add(m.Name);
			Algorithms.Add("MD2");
			Algorithms.Add("MD4");
			Algorithms.Add("MD5");
			Algorithms.Add("SHA1");
			Algorithms.Add("SHA256");
			Algorithms.Add("SHA384");
			Algorithms.Add("SHA512");
			foreach(SumModel m in SumManaged.Models)
				Algorithms.Add(m.Name);
			Algorithms.Add("RIPEMD160");
			
			LocalAlgorithms.Add("ADLER32", typeof(Adler32Managed));
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

			ResultList.View = View.Details;
			ResultList.CheckBoxes = true;
			ResultList.Columns.Add("Algorithm");
			ResultList.Columns.Add("Result");
			foreach(string algo in Algorithms)
			{
				ListViewItem item = ResultList.Items.Add(algo);
				item.Name = algo;
//				item.Checked = true;
				item.SubItems.Add(String.Empty);
			}
			ResultList.Dock = DockStyle.Fill;
			Controls.Add(ResultList);
			

			ConfigButton = new ToolStripButton(Host.Settings.Image("options_16.png"));
			ConfigButton.ToolTipText = "Configure";
			ToolBar.Items.Add(ConfigButton);
			ToolBar.Items.Add(new ToolStripSeparator());
			
			SelectionComboBox = new ToolStripComboBox();
			SelectionComboBox.Items.Add("Document");
			SelectionComboBox.Items.Add("Selection");
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
			
			
			ProgressBar.Maximum = 100;
			ProgressBar.Value = 0;
			ProgressBar.Alignment = ToolStripItemAlignment.Right;
			StatusBar.Items.Add(ProgressBar);
			
			ProgressLabel.Text = "Ready";
			StatusBar.Items.Add(ProgressLabel);
			
			Controls.Add(StatusBar);
			ProgressBar.Dock = DockStyle.Bottom;
			Controls.Add(StatusBar);
			StatusBar.Hide();
		}
		
		public void OnCalculate(object sender, EventArgs e)
		{
			foreach(ListViewItem i in ResultList.Items)
				if(i.SubItems.Count > 1)
					i.SubItems[1].Text = String.Empty;
			
			if(Worker.IsBusy)
			{
				RefreshButton.Enabled = false;
				Worker.CancelAsync();
				return;
			}
			
			List<string> enabledAlgorithms = new List<string>();
			foreach(ListViewItem i in ResultList.Items)
				if(i.Checked)
					enabledAlgorithms.Add(i.Name);
			
			StatusBar.Show();
			RefreshButton.Image = Host.Settings.Image("stop_16.png");
			ProgressLabel.Text = "Calculating...";
			Worker.RunWorkerAsync(enabledAlgorithms);
		}
		
		public void OnDoWork(object sender, DoWorkEventArgs e)
		{
			const int BLOCK_SIZE = 4096*1024;
			
			Dictionary<string, System.Security.Cryptography.HashAlgorithm> algos = new Dictionary<string, System.Security.Cryptography.HashAlgorithm>();
			foreach(string algo in (List<string>)e.Argument)
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
			PieceBuffer buffer = Host.ActiveView.Document;
			long total = Host.ActiveView.Document.Length;
			long len = Host.ActiveView.Document.Length;
			long offset = 0;
			long lastReportTime = System.Environment.TickCount;
			long lastReportBytes = 0;
			byte[] bytes = new byte[BLOCK_SIZE];
			while(len > 0)
			{
				int partLen = BLOCK_SIZE > len ? (int)len : BLOCK_SIZE;
				buffer.GetBytes(offset, bytes, partLen);

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
			ProgressBar.Value = e.ProgressPercentage;
			ProgressLabel.Text = String.Format("Calculating...{0}%  ({1:0.##} MB/s)", e.ProgressPercentage, (float)e.UserState);
		}
		
		public void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(e.Cancelled == false)
			{
				List<ChecksumResult> results = e.Result as List<ChecksumResult>;
				
				foreach(ChecksumResult result in results)
				{
					StringBuilder hash = new StringBuilder();
					foreach(byte b in result.Checksum)
						hash.Append(b.ToString("X2"));
					ListViewItem[] items = ResultList.Items.Find(result.Name, false);
					if(items.Length == 1)
						items[0].SubItems[1].Text = hash.ToString();
				}
			}
			
			StatusBar.Hide();
			ProgressLabel.Text = "Ready";
			ProgressBar.Value = 0;
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
			Host.AddWindow(new ResultWindow(Host), "Checksum");
			
//			ToolStripMenuItem item = host.AddMenuItem("Tools/Analyse/Checksum");
//			item.Click += OnChecksum;		
		}
		
		void IPlugin.Dispose()
		{
		}
		
		
//		void OnChecksum(object sender, EventArgs e)
//		{
//			if(Host.ActiveView != null)
//			{
//			}
//		}
	}
}
