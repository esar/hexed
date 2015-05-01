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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.ComponentModel;

namespace StatisticsPlugin
{
	public class StatisticsWorker : BackgroundWorker
	{
		private class ProgressInfo
		{
			public string Message;
			public long[] Counts;
			public ProgressInfo(string msg, long[] counts) { Message = msg; Counts = counts; }
		}
		
		private readonly Document Document;
		private readonly long StartPosition;
		private readonly long EndPosition;
		private readonly Statistics Statistics;

		private bool _Cancelled;
		public bool Cancelled { get { return _Cancelled; } }
		
		public readonly ProgressNotification Progress;
		

		public StatisticsWorker(Document document, long startPosition, long endPosition, Statistics statistics, ProgressNotification progress)
		{
			Document = document;
			StartPosition = startPosition;
			EndPosition = endPosition;
			Statistics = statistics;
			Progress = progress;
			
			WorkerReportsProgress = true;
			WorkerSupportsCancellation = true;
		}
		
		protected override void OnDoWork(DoWorkEventArgs e)
		{
			const int BLOCK_SIZE = 4096*1024;

			long total;
			long offset;
			long lastReportTime = System.Environment.TickCount;
			long lastReportBytes = 0;
			long[] counts = new long[256];
			byte[] bytes = new byte[BLOCK_SIZE];
			
			if(StartPosition == -1 && EndPosition == -1)
			{
				total = Document.Length;
				offset = 0;
			}
			else
			{
				total = (EndPosition - StartPosition) / 8;
				offset = StartPosition / 8;
			}

			long len = total;			
			while(len > 0)
			{
				int partLen = BLOCK_SIZE > len ? (int)len : BLOCK_SIZE;
				Document.GetBytes(offset, bytes, partLen);

				for(int i = 0; i < partLen; ++i)
					counts[bytes[i]] += 1;
				
				len -= partLen;
				offset += partLen;
				
				if(System.Environment.TickCount > lastReportTime + 500)
				{
					float MBps = (float)(offset - lastReportBytes) / (1024 * 1024);
					MBps /= (float)(System.Environment.TickCount - lastReportTime) / 1000;
					ReportProgress((int)((100.0 / total) * (total - len)), new ProgressInfo(String.Format("Calculating statistics...  ({0:0.##} MB/s)", MBps), counts));
					counts = new long[256];
					lastReportBytes = offset;
					lastReportTime = System.Environment.TickCount;
				}
				
				if(CancellationPending)
				{
					_Cancelled = true;
					return;
				}
			}
			
			ReportProgress(100, new ProgressInfo(String.Empty, counts));
		}

		protected override void OnProgressChanged(ProgressChangedEventArgs e)
		{
			ProgressInfo progress = (ProgressInfo)e.UserState;
			Statistics.Add(progress.Counts);
			
			base.OnProgressChanged(e);
			Progress.Update(e.ProgressPercentage, progress.Message);
		}
		
		protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
		{
			base.OnRunWorkerCompleted(e);
		}
	}
	

	public class Statistics : IHistogramDataSource
	{
		protected int _Length;
		public int Length
		{
			get { return _Length; }
			set { _Length = value; Array.Resize(ref Buckets, _Length); }
		}
		
		protected long[] Buckets;
		public long this[int index]	{ get { return Buckets[index]; } }

		protected long _Max;
		public long Max { get { return _Max; } }
		protected int _MaxCount;
		public int MaxCount { get { return _MaxCount; } }
		
		protected long _Min;
		public long Min	{ get { return _Min; } }
		protected int _MinCount;
		public int MinCount { get { return _MinCount; } }
		
		protected long _Sum;
		public long Sum	{ get { return _Sum; } }
		
		protected double _Mean;
		public double Mean { get { return _Mean; } }
		
		protected long _Median;
		public long Median { get { return _Median; } }
		
		protected double _StdDev;
		public double StdDev { get { return _StdDev; } }
		
		protected double _Skewness;
		public double Skewness { get { return _Skewness; } }
		
		protected int _TokenCount;
		public int TokenCount { get { return _TokenCount; } }
		
		protected double _Entropy;
		public double Entropy { get { return _Entropy; } }
		
		public Statistics()
		{
		}
		
		public void Clear()
		{
			Array.Clear(Buckets, 0, Buckets.Length);
			_Max = 0;
			_Min = 0;
			_Sum = 0;
			_Mean = 0;
			_MaxCount = 0;
			_MinCount = 0;
			_TokenCount = 0;
			_Entropy = 0;
		}

		public void Add(long[] counts)
		{
			if(counts.Length != Buckets.Length)
				throw new ArgumentException("The array lengths must match", "counts");
			
			_Min = Int64.MaxValue;
			_Max = Int64.MinValue;
			_TokenCount = 0;
			_Sum = 0;
			for(int i = 0; i < counts.Length; ++i)
			{
				Buckets[i] += counts[i];
				_Sum += Buckets[i];
				if(Buckets[i] > _Max)
				{
					_Max = Buckets[i];
					_MaxCount = i;
				}
				if(Buckets[i] < _Min)
				{
					_Min = Buckets[i];
					_MinCount = i;
				}
				
				if(Buckets[i] > 0)
				{
					_TokenCount += 1;
				}
			}

			_Mean = _Sum / Buckets.Length;
			
			long[] medianArray = new long[Buckets.Length];
			Array.Copy(Buckets, medianArray, Buckets.Length);
			Array.Sort(medianArray);
			if(medianArray.Length % 2 == 0)
				_Median = (medianArray[medianArray.Length / 2] + medianArray[medianArray.Length / 2 + 1]) / 2;
			else
				_Median = medianArray[medianArray.Length / 2];

			_StdDev = 0;
			for(int i = 0; i < Buckets.Length; ++i)
			{
				_StdDev += Math.Pow(Buckets[i] - _Mean, 2);
			}
			_StdDev = Math.Sqrt(_StdDev / Buckets.Length);
			
			_Skewness = 0;
			_Entropy = 0;
			for(int i = 0; i < Buckets.Length; ++i)
			{
				_Skewness = Math.Pow(Buckets[i] - _Mean, 3);
				if(Buckets[i] > 0)
				{
					double probability = (double)Buckets[i] / _Sum;
					_Entropy -= probability * Math.Log(probability, 2);
				}
			}
			
			_Skewness = _Skewness / (Buckets.Length * Math.Pow(_StdDev, 3));
		}
	}
	

	public interface IHistogramDataSource
	{
		long Min { get; }
		long Max { get; }
		int Length { get; }
		long this[int index] { get; }
	}
	
	public class HistogramControl : UserControl
	{
		protected int MouseHighlightIndex = -1;
		protected ToolTip Tip = new ToolTip();
		
		protected IHistogramDataSource _Data;
		public IHistogramDataSource Data
		{
			get { return _Data; }
			set { _Data = value; Invalidate(); }
		}
		
		public HistogramControl()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.UserPaint, true);
			BackColor = Color.White;
		}
	
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			
			if(_Data == null)
				return;
			
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
			
			if(_Data == null)
				return;
			
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
	
	public class StatisticsState
	{
		public StatisticsWorker Worker;
		public ProgressNotification Progress;
	}

	public class HexStringComparer : IComparer
	{
		public int Compare(object x, object y)
		{
			return Convert.ToUInt64(x.ToString(), 16).CompareTo(Convert.ToUInt64(y.ToString(), 16));
		}
	}
	
	public class NumericStringComparer : IComparer
	{
		public int Compare(object x, object y)
		{
			return Convert.ToDouble(x.ToString()).CompareTo(Convert.ToDouble(y.ToString()));
		}
	}
	
	public class NaturalStringComparer : IComparer
	{
		public int Compare(object objx, object objy)
		{
			string x = objx.ToString();
			string y = objy.ToString();
			int xStart = 0;
			int yStart = 0;
			int xEnd = x.Length;
			int yEnd = y.Length;
			int xPos = 0;
			int yPos = 0;
			int result;

			do
			{
				if(xPos >= xEnd && yPos >= yEnd)
					return 0;
				if(xPos >= xEnd)
					return -1;
				if(yPos >= yEnd)
					return 1;

				bool xIsDigit = Char.IsDigit(x[xPos]);
				while(xPos < xEnd && Char.IsDigit(x[xPos]) == xIsDigit)
					++xPos;
				
				bool yIsDigit = Char.IsDigit(y[yPos]);
				while(yPos < yEnd && Char.IsDigit(y[yPos]) == yIsDigit)
					++yPos;
				
				if(xIsDigit && yIsDigit)
					result = Convert.ToInt64(x.Substring(xStart, xPos - xStart), 10).CompareTo(Convert.ToInt64(y.Substring(yStart, yPos - yStart), 10));
				else
					result = String.Compare(x.Substring(xStart, xPos - xStart), y.Substring(yStart, yPos - yStart), true);
					
				xStart = xPos;
				yStart = yPos;
				
			} while(result == 0);			
			
			return result;
		}
	}
	
	public class ListViewColumnSorter : System.Collections.IComparer
	{
		private IComparer CurrentComparer;
		private IComparer _DefaultComparer;
		public IComparer DefaultComparer
		{
			get { return _DefaultComparer; }
			set { _DefaultComparer = value; }
		}
		
		private List<IComparer> _ColumnComparers = new List<IComparer>();
		public List<IComparer> ColumnComparers { get { return _ColumnComparers; } }
		
		private int _SortColumn;
		public int SortColumn
		{
			get { return _SortColumn; }
			set 
			{ 
				_SortColumn = value;
				if(_ColumnComparers.Count > _SortColumn)
					CurrentComparer = _ColumnComparers[_SortColumn];
				else
					CurrentComparer = _DefaultComparer;
			}
		}
		
		private SortOrder _SortOrder;
		public SortOrder SortOrder
		{
			get { return _SortOrder; }
			set { _SortOrder = value; }
		}

		public ListViewColumnSorter()
		{
			_DefaultComparer = new Comparer(System.Globalization.CultureInfo.CurrentUICulture);
			CurrentComparer = _DefaultComparer;
		}
		
		public int Compare(object x, object y)
		{
			ListViewItem itemX = (ListViewItem)x;
			ListViewItem itemY = (ListViewItem)y;

			int result = CurrentComparer.Compare(itemX.SubItems[_SortColumn].Text, itemY.SubItems[_SortColumn].Text);
			if(_SortOrder == SortOrder.Descending)
				return 0 - result;
			else
				return result;
		}
	}
	
	public class StatisticsPanel : Panel
	{
		IPluginHost Host;
		ToolStrip ToolBar = new ToolStrip();
		ToolStripComboBox SelectionComboBox = new ToolStripComboBox();
		ToolStripButton GraphButton;
		ToolStripButton TableButton;
		ToolStripButton StatsButton;
		ToolStripButton RefreshButton;
		HistogramControl Graph = new HistogramControl();
		ListView List = new ListView();
		ListViewItem[] ListItems;
		ListView StatsList = new ListView();
		ListViewItem StatsItemMin;
		ListViewItem StatsItemMax;
		ListViewItem StatsItemMean;
		ListViewItem StatsItemMedian;
		ListViewItem StatsItemStdDev;
		ListViewItem StatsItemSkewness;
		ListViewItem StatsItemSum;
		ListViewItem StatsItemTokenCount;
		ListViewItem StatsItemEntropy;
		DocumentRangeIndicator RangeIndicator = new DocumentRangeIndicator();
		Document Document;
		HexView CurrentView;
		StatisticsState State;
		Statistics Statistics;
		
		
		public StatisticsPanel(IPluginHost host)
		{
			Host = host;
			Host.ActiveViewChanged += OnActiveViewChanged;

			Host.Commands.Add("Statistics/Show Graph", "Shows the results as a bar graph", "Show Graph",
			                  Host.Settings.Image("icons.histogram_16.png"),
			                  null,
			                  OnShowGraph);
			Host.Commands.Add("Statistics/Show Table", "Shows the results as a table", "Show Table",
			                  Host.Settings.Image("icons.table_16.png"),
			                  null,
			                  OnShowTable);
			Host.Commands.Add("Statistics/Show Statistics", "Shows the results as a table of statistics", "Show Statistics",
			                  Host.Settings.Image("icons.stats_16.png"),
			                  null,
			                  OnShowStats);			
			Host.Commands.Add("Statistics/Calculate", "Calculates the statistics for the selection or document", "Calculate",
			                  Host.Settings.Image("icons.go_16.png"),
			                  null,
			                  OnCalculate);
			
			Graph.Dock = DockStyle.Fill;
			Controls.Add(Graph);
			
			List.View = View.Details;
			List.Columns.Add("Hex");
			List.Columns.Add("Decimal");
			List.Columns.Add("Char");
			List.Columns.Add("Count");
			List.Columns.Add("Percent");
			
			ListItems = new ListViewItem[256];
			for(int i = 0; i < 256; ++i)
			{
				ListItems[i] = new ListViewItem(new string[] {i.ToString("X2"), 
					                                          i.ToString(), 
					                                          Encoding.ASCII.GetString(new byte[] {(byte)i}),
				                                              String.Empty,
				                                              String.Empty});
				List.Items.Add(ListItems[i]);
			}

			List.Dock = DockStyle.Fill;
			List.Visible = false;
			List.FullRowSelect = true;
			List.HideSelection = false;
			ListViewColumnSorter sorter = new ListViewColumnSorter();
			sorter.ColumnComparers.Add(new HexStringComparer());
			sorter.ColumnComparers.Add(new NumericStringComparer());
			sorter.ColumnComparers.Add(new Comparer(System.Globalization.CultureInfo.CurrentCulture));
			sorter.ColumnComparers.Add(new NumericStringComparer());
			sorter.SortColumn = 0;
			sorter.SortOrder = SortOrder.Ascending;
			List.ListViewItemSorter = sorter;
			List.ColumnClick += OnColumnClick;
			Controls.Add(List);
			
			StatsList.View = View.Details;
			StatsList.Columns.Add("Statistic");
			StatsList.Columns.Add("Value");
			StatsItemMin = StatsList.Items.Add(new ListViewItem(new string[] {"Min", String.Empty}));
			StatsItemMax = StatsList.Items.Add(new ListViewItem(new string[] {"Max", String.Empty}));
			StatsItemMean = StatsList.Items.Add(new ListViewItem(new string[] {"Mean", String.Empty}));
			StatsItemMedian = StatsList.Items.Add(new ListViewItem(new string[] {"Median", String.Empty}));
			StatsItemStdDev = StatsList.Items.Add(new ListViewItem(new string[] {"Standard Deviation", String.Empty}));
			StatsItemSkewness = StatsList.Items.Add(new ListViewItem(new string[] {"Skewness", String.Empty}));
			StatsItemSum = StatsList.Items.Add(new ListViewItem(new string[] {"Sum", String.Empty}));
			StatsItemTokenCount = StatsList.Items.Add(new ListViewItem(new string[] {"Token Count", String.Empty}));
			StatsItemEntropy = StatsList.Items.Add(new ListViewItem(new string[] {"Entropy", String.Empty}));
			StatsList.Dock = DockStyle.Fill;
			StatsList.Visible = false;
			Controls.Add(StatsList);
			
			RangeIndicator.Dock = DockStyle.Bottom;
			RangeIndicator.Orientation = Orientation.Horizontal;
			Controls.Add(RangeIndicator);
			
			GraphButton = Host.CreateToolButton("Statistics/Show Graph");
			ToolBar.Items.Add(GraphButton);
			TableButton = Host.CreateToolButton("Statistics/Show Table");
			ToolBar.Items.Add(TableButton);
			StatsButton = Host.CreateToolButton("Statistics/Show Statistics");
			ToolBar.Items.Add(StatsButton);
			ToolBar.Items.Add(new ToolStripSeparator());
			
			
			SelectionComboBox.Items.Add("Selection");
			SelectionComboBox.Items.Add("Document");
			SelectionComboBox.SelectedIndex = 0;
			SelectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			ToolBar.Items.Add(SelectionComboBox);
			RefreshButton = Host.CreateToolButton("Statistics/Calculate");
			RefreshButton.Enabled = false;
			ToolBar.Items.Add(RefreshButton);
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			Controls.Add(ToolBar);			
		}

		protected void OnColumnClick(object sender, ColumnClickEventArgs e)
		{
			ListView view = (ListView)sender;
			ListViewColumnSorter sorter = (ListViewColumnSorter)view.ListViewItemSorter;
			if(sorter.SortColumn == e.Column)
			{
				if(sorter.SortOrder == SortOrder.Ascending)
					sorter.SortOrder = SortOrder.Descending;
				else
					sorter.SortOrder = SortOrder.Ascending;
			}
			else
				sorter.SortColumn = e.Column;
			
			view.Sort();
		}
		
		protected void PopulateList(Statistics data)
		{
			List.BeginUpdate();
			for(int i = 0; i < 256; ++i)
			{
				long count = data[i];
				float percent = (float)count / data.Sum;
				percent *= 100.0f;
				
				ListItems[i].SubItems[3].Text = count.ToString();
				ListItems[i].SubItems[4].Text = percent.ToString("0.00");
			}
			List.EndUpdate();			
		}
		
		protected void ClearList()
		{
			List.BeginUpdate();
			for(int i = 0; i < 256; ++i)
			{
				ListItems[i].SubItems[3].Text = String.Empty;
				ListItems[i].SubItems[4].Text = String.Empty;
			}
			List.EndUpdate();
		}
		
		protected void PopulateStatsList(Statistics data)
		{
			StatsList.BeginUpdate();
			StatsItemMin.SubItems[1].Text = data.Min.ToString();
			StatsItemMax.SubItems[1].Text = data.Max.ToString();
			StatsItemMean.SubItems[1].Text = data.Mean.ToString("0.##");
			StatsItemMedian.SubItems[1].Text = data.Median.ToString();
			StatsItemStdDev.SubItems[1].Text = data.StdDev.ToString("0.##");
			StatsItemSkewness.SubItems[1].Text = data.Skewness.ToString("0.##");
			StatsItemSum.SubItems[1].Text = data.Sum.ToString();
			StatsItemTokenCount.SubItems[1].Text = data.TokenCount.ToString();
			StatsItemEntropy.SubItems[1].Text = data.Entropy.ToString("0.##");
			StatsList.EndUpdate();
		}
		
		protected void ClearStatsList()
		{
			StatsList.BeginUpdate();
			foreach(ListViewItem i in StatsList.Items)
				i.SubItems[1].Text = String.Empty;
			StatsList.EndUpdate();
		}
		
		protected void OnViewSelectionChanged(object sender, EventArgs e)
		{
			if(CurrentView.Selection.Length == 0)
				SelectionComboBox.SelectedIndex = 1;
			else
				SelectionComboBox.SelectedIndex = 0;
		}
		
		protected void OnActiveViewChanged(object sender, EventArgs e)
		{
			if(CurrentView != null)
				CurrentView.Selection.Changed -= OnViewSelectionChanged;
			CurrentView = Host.ActiveView;
			if(CurrentView != null)
			{
				CurrentView.Selection.Changed += OnViewSelectionChanged;
				OnViewSelectionChanged(CurrentView.Selection, EventArgs.Empty);
			}
			
			Document = null;
			State = null;
			Statistics = null;
			
			if(Host.ActiveView != null)
			{
				Document = Host.ActiveView.Document;
			
				object o;
				if(!Document.MetaData.TryGetValue("Statistics", out o))
				{
					Statistics = new Statistics();
					Statistics.Length = 256;
					Document.MetaData.Add("Statistics", Statistics);
				}
				else
					Statistics = (Statistics)o;
				
				if(!Document.PluginState.TryGetValue(this.GetType().FullName, out o))
				{
					State = new StatisticsState();
					Document.PluginState.Add(this.GetType().FullName, State);
				}
				else
					State = (StatisticsState)o;
			}

			if(Statistics != null)
			{
				if(State.Worker != null && State.Worker.IsBusy)
				{
					RefreshButton.Image = Host.Settings.Image("icons.stop_16.png");
					if(State.Worker.CancellationPending)
						RefreshButton.Enabled = false;
					else
						RefreshButton.Enabled = true;
				}
				else
				{
					RefreshButton.Image = Host.Settings.Image("icons.go_16.png");
					RefreshButton.Enabled = true;
				}

				PopulateList(Statistics);
				PopulateStatsList(Statistics);
			}
			else
			{
				RefreshButton.Image = Host.Settings.Image("icons.go_16.png");
				RefreshButton.Enabled = false;

				ClearList();
				ClearStatsList();
			}
			
			Graph.Data = Statistics;
		}
		
		protected void OnShowGraph(object sender, EventArgs e)
		{
			TableButton.Checked = false;
			StatsButton.Checked = false;
			GraphButton.Checked = true;
			List.Hide();
			StatsList.Hide();
			Graph.Show();
		}
		
		protected void OnShowTable(object sender, EventArgs e)
		{
			GraphButton.Checked = false;
			StatsButton.Checked = false;
			TableButton.Checked = true;
			Graph.Hide();
			StatsList.Hide();
			List.Show();
		}
		
		protected void OnShowStats(object sender, EventArgs e)
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
			if(State.Worker != null && State.Worker.IsBusy)
			{
				RefreshButton.Enabled = false;
				State.Worker.CancelAsync();
				return;
			}
			
			Statistics.Clear();
			RefreshButton.Image = Host.Settings.Image("icons.stop_16.png");
			
			long startPos = -1;
			long endPos = -1;
			if(SelectionComboBox.SelectedIndex == 0)
			{
				startPos = Host.ActiveView.Selection.Start; 
				endPos = Host.ActiveView.Selection.End;
			}

			State.Progress = new ProgressNotification();
			Host.ProgressNotifications.Add(State.Progress);
			State.Worker = new StatisticsWorker(Document, startPos, endPos, Statistics, State.Progress);
			State.Worker.ProgressChanged += OnProgressChanged;
			State.Worker.RunWorkerCompleted += OnCompleted;
			State.Worker.RunWorkerAsync();
		}
		

		public void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			StatisticsWorker worker = (StatisticsWorker)sender;
			
			if(worker == State.Worker)
			{
				PopulateList(Statistics);
				PopulateStatsList(Statistics);
				Graph.Invalidate();
			}
		}
		
		public void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			StatisticsWorker worker = (StatisticsWorker)sender;
			
			Host.ProgressNotifications.Remove(worker.Progress);
			
			worker.Dispose();
			if(State.Worker == worker)
			{
				RefreshButton.Image = Host.Settings.Image("icons.go_16.png");
				RefreshButton.Enabled = true;
				State.Worker = null;
				State.Progress = null;
			}
		}
	}
	
	public class StatisticsPlugin : IPlugin
	{
		public Image Image { get { return Settings.Instance.Image("icons.histogram_16.png"); } }
		public string Name { get { return "Statistics"; } }
		public string Description { get { return "Provides statisitcs about the contents of a document"; } }
		public string Author { get { return "Stephen Robinson"; } }
		public string Version { get { return "1.0"; } }
		public string Copyright { get { return "(c)2008 Stephen Robinson"; } }
		public string Url { get { return "http://www.esar.org.uk/"; } }

		public void Initialize(IPluginHost host)
		{
			host.AddWindow(new StatisticsPanel(host), "Statistics", host.Settings.Image("icons.histogram_16.png"), DefaultWindowPosition.BottomRight, true);
		}
		
		public void Dispose()
		{
		}
	}
}
