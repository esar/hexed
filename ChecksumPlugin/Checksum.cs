using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

namespace ChecksumPlugin
{
	public class ResultWindow : Form
	{
		IPluginHost Host;
		BackgroundWorker Worker;

		Panel ControlPanel = new Panel();
		Button CalcButton = new Button();
		
		ListView ResultList = new ListView();
		
		StatusStrip StatusBar = new StatusStrip();
		ToolStripProgressBar ProgressBar = new ToolStripProgressBar();
		ToolStripLabel ProgressLabel = new ToolStripLabel();
		
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
			ResultList.Columns.Add("Algorithm");
			ResultList.Columns.Add("Result");
			ResultList.Dock = DockStyle.Fill;
			Controls.Add(ResultList);
			
			CalcButton.Click += OnCalculate;
			CalcButton.Text = "Calculate";
			CalcButton.AutoSize = true;
			CalcButton.Dock = DockStyle.Right;
			ControlPanel.AutoSize = true;
			ControlPanel.Controls.Add(CalcButton);
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
			ResultList.Clear();
			
			if(Worker.IsBusy)
			{
				CalcButton.Enabled = false;
				Worker.CancelAsync();
				return;
			}
			
			CalcButton.Text = "Cancel";
			ProgressLabel.Text = "Calculating...";
			Worker.RunWorkerAsync();
		}
		
		public void OnDoWork(object sender, DoWorkEventArgs e)
		{
			const int BLOCK_SIZE = 4096*1024;
			
			System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
			sha1.Initialize();

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

				md5.TransformBlock(bytes, 0, partLen, bytes, 0);
				sha1.TransformBlock(bytes, 0, partLen, bytes, 0);
				
				
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
			
			md5.TransformFinalBlock(bytes, 0, 0);
			sha1.TransformFinalBlock(bytes, 0, 0);
			e.Result = sha1.Hash.Clone();
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
				StringBuilder hash = new StringBuilder();
				foreach(byte b in (byte[])e.Result)
					hash.Append(b.ToString("X2"));
				ListViewItem item = ResultList.Items.Add("SHA1");
				item.SubItems.Add(hash.ToString());
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
