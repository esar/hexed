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
			ProgressBar.Dock = DockStyle.Fill;
			StatusBar.Items.Add(ProgressBar);
			Controls.Add(StatusBar);
			ProgressBar.Dock = DockStyle.Bottom;
			Controls.Add(StatusBar);
		}
		
		public void OnCalculate(object sender, EventArgs e)
		{
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
			
			PieceBuffer buffer = Host.ActiveView.Document.Buffer;
			long max = 0;
			
			
			long total = Host.ActiveView.Document.Buffer.Length;
			long len = Host.ActiveView.Document.Buffer.Length;
			long offset = 0;
			long lastReportTime = System.Environment.TickCount;
			byte[] bytes = new byte[4096];
			while(len > 0)
			{
				int partLen = 4096 > len ? (int)len : 4096;
				buffer.GetBytes(offset, bytes, partLen);
				
				for(int i = 0; i < partLen; ++i)
				{
				}
				
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
			
/*			int i = 0;
			int lastReportTime = System.Environment.TickCount;
			while(true)
			{
				
				if(++i > 100)
					i = 0;

				if(System.Environment.TickCount > lastReportTime + 1000)
				{
					Worker.ReportProgress(i);
					lastReportTime = System.Environment.TickCount;
				}
				
				if(Worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}
			} */
		}
		
		public void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressBar.Value = e.ProgressPercentage;
		}
		
		public void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
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
				/*
				PieceBuffer buffer = Host.ActiveView.Document.Buffer;
				long max = 0;
				long[] buckets = new long[0x100];
				
				for(int i = 0; i < 0x100; ++i)
					buckets[i] = 0;
				
				long len = Host.ActiveView.Document.Buffer.Length;
				long offset = 0;
				byte[] bytes = new byte[4096];
				while(len > 0)
				{
					int partLen = 4096 > len ? (int)len : 4096;
					buffer.GetBytes(offset, bytes, partLen);
					
					for(int i = 0; i < partLen; ++i)
					{
						buckets[bytes[i]]++;
						if(buckets[bytes[i]] > max)
							max = buckets[bytes[i]];
					}
					
					len -= partLen;
					offset += partLen;
				}
				*/


				ResultWindow results = new ResultWindow(Host);
				results.Show();
			}
		}
	}
}
