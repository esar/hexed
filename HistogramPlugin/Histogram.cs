using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.ComponentModel;

namespace HistogramPlugin
{
	public class HistogramData
	{
		protected HistogramControl Owner;
		
		protected int _Length;
		public int Length
		{
			get { return _Length; }
			set { _Length = value; Array.Resize(ref Buckets, _Length); Owner.Invalidate(); }
		}
		
		protected long[] Buckets;
		public long this[int index]
		{
			get { return Buckets[index]; }
			set 
			{
				if(index == _MaxBucket && value < _Max)
				{
					_Max = 0;
					for(int i = 0; i < Buckets.Length; ++i)
					{
						if(Buckets[i] > _Max)
						{
							_Max = Buckets[i];
							_MaxBucket = i;
						}
					}
				}
				else if(value > _Max)
				{
					_Max = value;
					_MaxBucket = index;
				}
				
				if(index == _MinBucket && value > _Min)
				{
					_Min = Int64.MaxValue;
					for(int i = 0; i < Buckets.Length; ++i)
					{
						if(Buckets[i] < _Min)
						{
							_Min = Buckets[i];
							_MinBucket = i;
						}
					}
				}
				else if(value < Min)
				{
					_Min = value;
					_MinBucket = index;
				}
				
				Buckets[index] = value; 
				Owner.Invalidate(); 
			}
		}

		protected long _Max;
		public long Max
		{
			get { return _Max; }
		}
		
		protected int _MaxBucket = -1;
		public int MaxBucket
		{
			get { return _MaxBucket; }
		}
		
		protected long _Min;
		public long Min
		{
			get { return _Min; }
		}
		
		protected int _MinBucket;
		public int MinBucket
		{
			get { return _MinBucket; }
		}
		
		public HistogramData(HistogramControl owner)
		{
			Owner = owner;
		}
		
		public void Clear()
		{
			Array.Clear(Buckets, 0, Buckets.Length);
			_Max = 0;
			_MaxBucket = 0;
		}
	}
	
	public class HistogramControl : UserControl
	{
		protected int MouseHighlightIndex = -1;
		protected ToolTip Tip = new ToolTip();
		
		protected HistogramData _Data;
		public HistogramData Data
		{
			get { return _Data; }
		}
		
		public HistogramControl()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.UserPaint, true);
			
			_Data = new HistogramData(this);			
		}
	
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			
			float barWidth = (float)ClientRectangle.Width / (float)_Data.Length;
			MouseHighlightIndex = (int)((float)e.Location.X / barWidth);
			if(MouseHighlightIndex >= _Data.Length)
				MouseHighlightIndex = _Data.Length - 1;
			Point tipPoint = new Point(e.Location.X + 20, e.Location.Y + 20);
			
			string tip = String.Format("Hex: {0}\nDec: {1}\nChar: {2}\nCount: {3}", 
			                           MouseHighlightIndex.ToString("X"), 
			                           MouseHighlightIndex.ToString(),
			                           Encoding.ASCII.GetString(new byte[] {(byte)MouseHighlightIndex}),
			                           _Data[MouseHighlightIndex]);
			Tip.Show(tip, this, tipPoint, 100000);
			Invalidate();
		}
		
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			MouseHighlightIndex = -1;
			Tip.Hide(this);
			Invalidate();
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

			float barWidth = (float)ClientRectangle.Width / (float)_Data.Length;
			float scale;
			if(_Data.Max > 0)
				scale = (float)ClientRectangle.Height / _Data.Max;
			else
				scale = 1;
	
			Brush brush = new LinearGradientBrush(new Point(0, 0), new Point((int)Math.Ceiling(barWidth), 0), SystemColors.ActiveCaption, SystemColors.GradientActiveCaption);
			GraphicsPath path = new GraphicsPath();
			PointF[] points = new PointF[Data.Length*2 + 2];
			points[0] = new PointF(0, ClientSize.Height);
			for(int i = 0; i < _Data.Length; ++i)
			{
				points[i*2 + 1] = new PointF(barWidth * i, ClientRectangle.Height - (_Data[i] * scale));
				points[i*2 + 2] = new PointF(barWidth * (i+1), ClientRectangle.Height - (_Data[i] * scale));
			}
			points[points.Length - 1] = new PointF(ClientSize.Width, ClientSize.Height);
			path.AddLines(points);
			path.CloseAllFigures();
			e.Graphics.FillPath(brush, path);
			brush.Dispose();

			brush = new SolidBrush(Color.Red);
			if(MouseHighlightIndex >= 0)
			{
				RectangleF rect = new RectangleF(barWidth * MouseHighlightIndex, 
				                                 0,
				                                 (float)Math.Ceiling(barWidth), 
				                                 ClientSize.Height);
				e.Graphics.FillRectangle(brush, rect);
			}
			
			brush.Dispose();
		}
	}
	
	public class WorkerArgs
	{
		public Document Document;
		public long StartPosition;
		public long EndPosition;
		
		public WorkerArgs(Document document, long start, long end)
		{
			Document = document;
			StartPosition = start;
			EndPosition = end;
		}
	}
	
	public class WorkerProgress
	{
		public long BytesTotal;
		public long BytesDone;
		public long[] Counts;
		public float MBps;
		
		public WorkerProgress(long[] counts, long bytesTotal, long bytesDone, float mbps)
		{
			Counts = counts;
			BytesTotal = bytesTotal;
			BytesDone = bytesDone;
			MBps = mbps;
		}
	}
	
	public class HistogramPanel : Panel
	{
		IPluginHost Host;
		ToolStrip ToolBar = new ToolStrip();
		ToolStripComboBox SelectionComboBox = new ToolStripComboBox();
		ToolStripButton GraphButton;
		ToolStripButton TableButton;
		ToolStripButton StatsButton;
		HistogramControl Graph = new HistogramControl();
		ListView List = new ListView();
		ListView StatsList = new ListView();
		ListViewItem StatsItemMin;
		ListViewItem StatsItemMax;
		ListViewItem StatsItemMean;
		ListViewItem StatsItemMedian;
		DocumentRangeIndicator RangeIndicator = new DocumentRangeIndicator();
		BackgroundWorker Worker;
		ProgressNotification Progress;
		
		
		public HistogramPanel(IPluginHost host)
		{
			Host = host;
			
			Graph.Dock = DockStyle.Fill;
			Controls.Add(Graph);
			Graph.Data.Length = 256;
			
			List.View = View.Details;
			List.Columns.Add("Hex");
			List.Columns.Add("Decimal");
			List.Columns.Add("Char");
			List.Columns.Add("Count");
			List.Columns.Add("Percent");
			for(int i = 0; i < 256; ++i)
			{
				ListViewItem item = List.Items.Add(i.ToString("X"));
				item.SubItems.Add(i.ToString());
				item.SubItems.Add(Encoding.ASCII.GetString(new byte[] {(byte)i}));
				item.SubItems.Add(String.Empty);
				item.SubItems.Add(String.Empty);
			}
			List.Dock = DockStyle.Fill;
			List.Visible = false;
			Controls.Add(List);
			
			StatsList.View = View.Details;
			StatsList.Columns.Add("Statistic");
			StatsList.Columns.Add("Value");
			StatsItemMin = StatsList.Items.Add(new ListViewItem(new string[] {"Min", String.Empty}));
			StatsItemMax = StatsList.Items.Add(new ListViewItem(new string[] {"Max", String.Empty}));
			StatsItemMean = StatsList.Items.Add(new ListViewItem(new string[] {"Mean", String.Empty}));
			StatsItemMedian = StatsList.Items.Add(new ListViewItem(new string[] {"Median", String.Empty}));
			StatsList.Dock = DockStyle.Fill;
			StatsList.Visible = false;
			Controls.Add(StatsList);
			
			RangeIndicator.Dock = DockStyle.Bottom;
			RangeIndicator.Orientation = Orientation.Horizontal;
			Controls.Add(RangeIndicator);
			
			GraphButton = new ToolStripButton(Host.Settings.Image("histogram_16.png"));
			GraphButton.ToolTipText = "Show Graph";
			GraphButton.Click += OnShowGraph;
			ToolBar.Items.Add(GraphButton);
			TableButton = new ToolStripButton(Host.Settings.Image("table_16.png"));
			TableButton.ToolTipText = "Show Table";
			TableButton.Click += OnShowTable;
			ToolBar.Items.Add(TableButton);
			StatsButton = new ToolStripButton(Host.Settings.Image("stats_16.png"));
			StatsButton.ToolTipText = "Show Statistics";
			StatsButton.Click += OnShowStats;
			ToolBar.Items.Add(StatsButton);
			ToolBar.Items.Add(new ToolStripSeparator());
			
			
			SelectionComboBox.Items.Add("Selection");
			SelectionComboBox.Items.Add("Document");
			SelectionComboBox.SelectedIndex = 0;
			SelectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			ToolBar.Items.Add(SelectionComboBox);
			ToolBar.Items.Add(Host.Settings.Image("go_16.png")).Click += OnCalculate;
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			Controls.Add(ToolBar);
			
			Worker = new BackgroundWorker();
			Worker.WorkerReportsProgress = true;
			Worker.WorkerSupportsCancellation = true;
			Worker.DoWork += OnDoWork;
			Worker.ProgressChanged += OnProgressChanged;
			Worker.RunWorkerCompleted += OnCompleted;
		}

		public void OnShowGraph(object sender, EventArgs e)
		{
			TableButton.Checked = false;
			StatsButton.Checked = false;
			GraphButton.Checked = true;
			List.Hide();
			StatsList.Hide();
			Graph.Show();
		}
		
		public void OnShowTable(object sender, EventArgs e)
		{
			GraphButton.Checked = false;
			StatsButton.Checked = false;
			TableButton.Checked = true;
			Graph.Hide();
			StatsList.Hide();
			List.Show();
		}
		
		public void OnShowStats(object sender, EventArgs e)
		{
			GraphButton.Checked = false;
			TableButton.Checked = false;
			StatsButton.Checked = true;
			Graph.Hide();
			List.Hide();
			StatsList.Show();
		}
		
		public void OnCalculate(object sender, EventArgs e)
		{
			Graph.Data.Clear();
			
			Progress = new ProgressNotification();
			Host.ProgressNotifications.Add(Progress);
			
			if(SelectionComboBox.SelectedIndex != 0)
			{
				Worker.RunWorkerAsync(new WorkerArgs(Host.ActiveView.Document,
				                                     -1, 
				                                     -1));
			}
			else
			{
				Worker.RunWorkerAsync(new WorkerArgs(Host.ActiveView.Document, 
				                                     Host.ActiveView.Selection.Start, 
				                                     Host.ActiveView.Selection.End));
			}
		}
		
		public void OnDoWork(object sender, DoWorkEventArgs e)
		{
			const int BLOCK_SIZE = 4096*1024;
			WorkerArgs args = (WorkerArgs)e.Argument;

			long total;
			long offset;
			long lastReportTime = System.Environment.TickCount;
			long lastReportBytes = 0;
			long[] counts = new long[256];
			byte[] bytes = new byte[BLOCK_SIZE];
			
			if(args.StartPosition == -1 && args.EndPosition == -1)
			{
				total = args.Document.Length;
				offset = 0;
			}
			else
			{
				total = (args.EndPosition - args.StartPosition) / 8;
				offset = args.StartPosition / 8;
			}
			
			long len = total;			
			while(len > 0)
			{
				int partLen = BLOCK_SIZE > len ? (int)len : BLOCK_SIZE;
				args.Document.GetBytes(offset, bytes, partLen);

				for(int i = 0; i < partLen; ++i)
					counts[bytes[i]] += 1;
				
				len -= partLen;
				offset += partLen;
				
				if(System.Environment.TickCount > lastReportTime + 250)
				{
					float MBps = (float)(offset - lastReportBytes) / (1024 * 1024);
					MBps /= (float)(System.Environment.TickCount - lastReportTime) / 1000;
					Worker.ReportProgress((int)((100.0 / total) * (total - len)), new WorkerProgress(counts, total, total - len, MBps));
					counts = new long[256];
					lastReportBytes = offset;
					lastReportTime = System.Environment.TickCount;
				}
				
				if(Worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}
			}
			
			Worker.ReportProgress(100, new WorkerProgress(counts, total, total, 0));
		}

		public void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			WorkerProgress progress = (WorkerProgress)e.UserState;

			float percentDone = (float)progress.BytesDone / progress.BytesTotal;
			percentDone *= 100.0f;
			Progress.Update((int)percentDone, String.Format("Calculating statistics... ({0:0.##} MB/s)", progress.MBps));
			
			List.BeginUpdate();
			//Graph.BeginUpdate();
			foreach(ListViewItem i in List.Items)
			{
				int index = i.Index;
				long val = Graph.Data[index] + progress.Counts[index];
				float percent = (float)val / progress.BytesDone;
				percent *= 100.0f;
				
				Graph.Data[index] = val;
				i.SubItems[3].Text = val.ToString();
				i.SubItems[4].Text = percent.ToString("0.00");
			}
			//Graph.EndUpdate();
			List.EndUpdate();
			
			StatsList.BeginUpdate();
			StatsItemMin.SubItems[1].Text = Graph.Data.Min.ToString();
			StatsItemMax.SubItems[1].Text = Graph.Data.Max.ToString();
			StatsItemMean.SubItems[1].Text = ((float)progress.BytesDone / 256).ToString("0.##");
			StatsList.EndUpdate();
		}
		
		public void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			Host.ProgressNotifications.Remove(Progress);
			Progress = null;
		}
	}
	
	public class Histogram : IPlugin
	{
		string IPlugin.Name { get { return "Histogram"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		IPluginHost Host;
		
		void IPlugin.Initialize(IPluginHost host)
		{
			Host = host;
			Host.AddWindow(new HistogramPanel(host), "Histogram", Host.Settings.Image("histogram_16.png"), DefaultWindowPosition.BottomRight, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
