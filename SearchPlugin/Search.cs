using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SearchPlugin
{
	class DocumentMatchBucketCollection
	{
		DocumentMatchIndicator Owner;
		long DocumentLength;
		bool[] Buckets;
		public long BucketWidth;
		
		public int BucketCount
		{
			get { return Buckets.Length; }
		}
		
		public bool this[int i]
		{
			get { return Buckets[i]; }
		}
		
		public DocumentMatchBucketCollection(DocumentMatchIndicator owner, long documentLength, int numBuckets)
		{
			Owner = owner;
			DocumentLength = documentLength;
			Buckets = new bool[numBuckets];
			BucketWidth = documentLength / numBuckets;
		}
		
		public void Add(long position)
		{
			Buckets[position / BucketWidth] = true;
			Owner.Invalidate();
		}
	}
	
	class DocumentMatchIndicator : UserControl
	{
		private long _SelectedMatch = -1;
		public long SelectedMatch
		{
			get { return _SelectedMatch; }
			set { _SelectedMatch = value; Invalidate(); }
		}
		
		public DocumentMatchBucketCollection Matches;
		
		protected SolidBrush Brush;
		protected SolidBrush HighlightBrush;
		
		public DocumentMatchIndicator()
		{
			Brush = new SolidBrush(Color.Blue);
			HighlightBrush = new SolidBrush(Color.Red);
			Width = 25;
		}

		public void Reset(long documentLength, int numBuckets)
		{
			Matches = new DocumentMatchBucketCollection(this, documentLength, numBuckets);
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			const int border = 5;
			
			base.OnPaint(e);

			if(Matches == null)
				return;
			
			float bucketPixelWidth = (float)(ClientSize.Height - border*2) / Matches.BucketCount;
			
			for(int i = 0; i < Matches.BucketCount; ++i)
			{
				if(Matches[i])
				{
					RectangleF r = new RectangleF(border, bucketPixelWidth * i + border, ClientSize.Width - border*2, bucketPixelWidth);

					if(i != _SelectedMatch / Matches.BucketWidth)
						e.Graphics.FillRectangle(Brush, r);
				}
			}

			if(_SelectedMatch >= 0)
			{
				int i = (int)(_SelectedMatch / Matches.BucketWidth);
				RectangleF r = new RectangleF(border, bucketPixelWidth * i + border, ClientSize.Width - border*2, bucketPixelWidth);
				e.Graphics.FillRectangle(HighlightBrush, r);
			}				
			
			e.Graphics.DrawRectangle(SystemPens.ButtonShadow, new Rectangle(border, border, ClientSize.Width - 2*border, ClientSize.Height - 2*border));
		}
	}
	
	class SearchPanel : Panel
	{
		IPluginHost Host;
		DocumentMatchIndicator MatchIndicator = new DocumentMatchIndicator();
		ListView ListView = new ListView();
		ToolStrip ToolBar = new ToolStrip();
		
		public SearchPanel(IPluginHost host)
		{
			Host = host;
			
			ListView.Columns.Add("Position");
			ListView.Columns.Add("Length");
			ListView.Columns.Add("Value");
			ListView.View = View.Details;
			ListView.Dock = DockStyle.Fill;
			Controls.Add(ListView);
			
			MatchIndicator.Dock = DockStyle.Left;
			Controls.Add(MatchIndicator);
			
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolStripItem item = new ToolStripButton(Host.Settings.Image("newfolder_16.png"));
			item.ToolTipText = "New Search";
			item.Click += OnSearch;
			ToolBar.Items.Add(item);
			ToolBar.Items.Add(new ToolStripSeparator());
			ToolBar.Items.Add(Host.Settings.Image("first_16.png"));
			ToolBar.Items.Add(Host.Settings.Image("prev_16.png"));
			ToolBar.Items.Add(Host.Settings.Image("next_16.png"));
			ToolBar.Items.Add(Host.Settings.Image("last_16.png"));			
			Controls.Add(ToolBar);
			
			
			ListView.DoubleClick += OnListViewDoubleClick;
			ListView.SelectedIndexChanged += OnListViewSelectedIndexChanged;
		}
		
		public IEnumerable<long> Search(Document document, string pattern)
		{
			PatternMatchBMH matcher = new PatternMatchBMH();
			matcher.Initialize(pattern, true);

			long offset = 0;
			byte[] data = new byte[1024*1024];
			while(offset < document.Length)
			{
				long len = (document.Length - offset) > (1024 * 1024) ? (1024 * 1024) : (document.Length - offset);
				document.GetBytes(offset, data, len);
				
				foreach(int i in matcher.SearchBlock(data, 0, (int)len))
					yield return offset + i;

				offset += len;
			}
		}

		protected void OnSearch(object sender, EventArgs e)
		{
			SearchDialog dlg = new SearchDialog();
			if(dlg.ShowDialog() != DialogResult.OK)
				return;
			
			ListView.Items.Clear();
			MatchIndicator.Reset(Host.ActiveView.Document.Length, 512);
			
			foreach(long i in Search(Host.ActiveView.Document, dlg.Pattern))
			{
				MatchIndicator.Matches.Add(i);
				ListViewItem item = ListView.Items.Add(i.ToString());
				item.SubItems.Add("0");
				item.SubItems.Add(dlg.Pattern);
			}
		}
		
		protected void OnListViewDoubleClick(object sender, EventArgs e)
		{
			if(ListView.SelectedItems.Count > 0)
			{
				ListViewItem item = ListView.SelectedItems[0];
				long start = Convert.ToInt64(item.Text) * 8;
				long end = Convert.ToInt64(item.Text) * 8;
				Host.ActiveView.Selection.Set(start, end);
				Host.ActiveView.EnsureVisible(start);
			}
		}
		
		protected void OnListViewSelectedIndexChanged(object sender, EventArgs e)
		{
			if(ListView.SelectedItems.Count > 0)
				MatchIndicator.SelectedMatch = Convert.ToInt64(ListView.SelectedItems[0].Text);
			else
				MatchIndicator.SelectedMatch = -1;
		}
	}
	
	
	public class SearchPlugin : IPlugin
	{
		string IPlugin.Name { get { return "Search"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new SearchPanel(host), "Search", DefaultWindowPosition.Left, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
