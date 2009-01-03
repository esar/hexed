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
	
	public class ResultWindow : Form
	{
		IPluginHost Host;
		BackgroundWorker Worker;

		TableLayoutPanel ControlPanel = new TableLayoutPanel();
		Button CalcButton = new Button();
		
		ListView ResultList = new ListView();
		
		StatusStrip StatusBar = new StatusStrip();
		ToolStripProgressBar ProgressBar = new ToolStripProgressBar();
		ToolStripLabel ProgressLabel = new ToolStripLabel();

		string[] Algorithms = { "MD5",
			                    "SHA1",
			                    "SHA256",
			                    "SHA384",
			                    "SHA512",
			                    "RIPEMD160" };
		
		public ResultWindow(IPluginHost host)
		{
			Host = host;
			
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
				item.Checked = true;
				item.SubItems.Add(String.Empty);
			}
			ResultList.Dock = DockStyle.Fill;
			Controls.Add(ResultList);
			
			CalcButton.Click += OnCalculate;
			CalcButton.Text = "Calculate";
			CalcButton.AutoSize = true;
			CalcButton.Anchor = AnchorStyles.Right;
			ControlPanel.AutoSize = true;
			ControlPanel.RowCount = 1;
			ControlPanel.ColumnCount = 2;
			ControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			ControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ControlPanel.Controls.Add(CalcButton, 1, 0);
			ControlPanel.Dock = DockStyle.Top;
			Controls.Add(ControlPanel);
			
			ProgressLabel.Text = "Ready";
			StatusBar.Items.Add(ProgressLabel);
			ProgressBar.Maximum = 100;
			ProgressBar.Value = 0;
			ProgressBar.Alignment = ToolStripItemAlignment.Right;
			
			StatusBar.Items.Add(ProgressBar);
			Controls.Add(StatusBar);
			ProgressBar.Dock = DockStyle.Bottom;
			Controls.Add(StatusBar);
		}
		
		public void OnCalculate(object sender, EventArgs e)
		{
			foreach(ListViewItem i in ResultList.Items)
				if(i.SubItems.Count > 1)
					i.SubItems[1].Text = String.Empty;
			
			if(Worker.IsBusy)
			{
				CalcButton.Enabled = false;
				Worker.CancelAsync();
				return;
			}
			
			List<string> enabledAlgorithms = new List<string>();
			foreach(ListViewItem i in ResultList.Items)
				if(i.Checked)
					enabledAlgorithms.Add(i.Name);
			
			CalcButton.Text = "Cancel";
			ProgressLabel.Text = "Calculating...";
			Worker.RunWorkerAsync(enabledAlgorithms);
		}
		
		public void OnDoWork(object sender, DoWorkEventArgs e)
		{
			const int BLOCK_SIZE = 4096*1024;
			
			Dictionary<string, System.Security.Cryptography.HashAlgorithm> algos = new Dictionary<string, System.Security.Cryptography.HashAlgorithm>();
			foreach(string algo in (List<string>)e.Argument)
			{
				System.Security.Cryptography.HashAlgorithm ha = System.Security.Cryptography.CryptoConfig.CreateFromName(algo) as System.Security.Cryptography.HashAlgorithm;
				ha.Initialize();
				algos.Add(algo, ha);
			}

			// TODO: Need thread safe method of accessing the buffer
			// TODO: Need to know if the buffer changes while we're processing
			//       maybe we could lock the buffer so it's read only until we're finished?
			PieceBuffer buffer = Host.ActiveView.Document.Buffer;
			long total = Host.ActiveView.Document.Buffer.Length;
			long len = Host.ActiveView.Document.Buffer.Length;
			long offset = 0;
			long lastReportTime = System.Environment.TickCount;
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
					Worker.ReportProgress((int)((100.0 / total) * (total - len)));
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
			ProgressLabel.Text = "Calculating..." + e.ProgressPercentage + "%";
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
			
			ProgressLabel.Text = "Ready";
			ProgressBar.Value = 0;
			CalcButton.Text = "Calculate";
			CalcButton.Enabled = true;
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
			
			ToolStripMenuItem item = host.AddMenuItem("Tools/Analyse/Checksum");
			item.Click += OnChecksum;		
		}
		
		void IPlugin.Dispose()
		{
		}
		
		
		void OnChecksum(object sender, EventArgs e)
		{
			if(Host.ActiveView != null)
			{
				ResultWindow results = new ResultWindow(Host);
				results.Show();
			}
		}
	}
}
