using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;


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
			long bucket = position / BucketWidth;
			if(bucket > Buckets.Length - 1)
				bucket = Buckets.Length - 1;
			Buckets[bucket] = true;
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

			if(Matches != null)
			{
				float bucketPixelWidth = (float)(ClientSize.Height - border*2) / Matches.BucketCount;
				
				for(int i = 0; i < Matches.BucketCount; ++i)
				{
					if(Matches[i])
					{
						RectangleF r = new RectangleF(border, bucketPixelWidth * i + border, ClientSize.Width - border*2, bucketPixelWidth);
						if(r.Height < 1)
							r.Height = 1;

						if(i != _SelectedMatch / Matches.BucketWidth)
							e.Graphics.FillRectangle(Brush, r);
					}
				}

				if(_SelectedMatch >= 0)
				{
					int i = (int)(_SelectedMatch / Matches.BucketWidth);
					RectangleF r = new RectangleF(border, bucketPixelWidth * i + border, ClientSize.Width - border*2, bucketPixelWidth);
					if(r.Height < 1)
						r.Height = 1;
					e.Graphics.FillRectangle(HighlightBrush, r);
				}				
			}
			
			e.Graphics.DrawRectangle(SystemPens.ButtonShadow, new Rectangle(border, border, ClientSize.Width - 2*border, ClientSize.Height - 2*border));
		}
	}

	
	class SearchPanel : Panel
	{
		const int LIST_ITEM_CACHE_SIZE = 256;
		
		IPluginHost Host;
		DocumentMatchIndicator MatchIndicator = new DocumentMatchIndicator();
		ListView ListView = new ListView();
		ToolStrip ToolBar = new ToolStrip();
		VirtualSearch Search;
		ProgressNotification Progress;
		
		ListViewItem[] ListItemCache = new ListViewItem[LIST_ITEM_CACHE_SIZE];
		ListViewItem ListItemZero;
		long FirstCachedListItemIndex = 0;
		
		
		public SearchPanel(IPluginHost host)
		{
			Host = host;

			host.Commands.Add("Search/Search", "Searches for the specified pattern in the current document", "Search", 
			                  host.Settings.Image("search_16.png"), 
			                  new Keys[] { Keys.Control | Keys.F },
			                  OnSearch);
			host.Commands.Add("Search/Next", "Jumps to the next search result", "Next",
			                  Host.Settings.Image("next_16.png"),
			                  new Keys[] { Keys.F3 },
			                  OnNext);
			host.Commands.Add("Search/Prev", "Jumps to the previous search result", "Prev",
			                  Host.Settings.Image("prev_16.png"),
			                  new Keys[] { Keys.Control | Keys.F3 },
			                  OnPrev);
			host.Commands.Add("Search/First", "Jumps to the first search result", "First",
			                  Host.Settings.Image("first_16.png"),
			                  null,
			                  OnFirst);
			host.Commands.Add("Search/Last", "Jumps to the last search result", "Last",
			                  Host.Settings.Image("last_16.png"),
			                  null,
			                  OnLast);
			                  
			
			ListView.Columns.Add("Position");
			ListView.Columns.Add("Length");
			ListView.Columns.Add("Value");
			ListView.View = View.Details;
			ListView.FullRowSelect = true;
			ListView.HideSelection = false;
			ListView.Dock = DockStyle.Fill;
			ListView.VirtualMode = true;
			ListView.RetrieveVirtualItem += OnListRetrieveVirtualItem;
			Controls.Add(ListView);
			
			MatchIndicator.Dock = DockStyle.Left;
			Controls.Add(MatchIndicator);
			
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolBar.Items.Add(Host.CreateToolButton("Search/Search"));
			ToolBar.Items.Add(new ToolStripSeparator());
			ToolBar.Items.Add(Host.CreateToolButton("Search/First"));
			ToolBar.Items.Add(Host.CreateToolButton("Search/Prev"));
			ToolBar.Items.Add(Host.CreateToolButton("Search/Next"));
			ToolBar.Items.Add(Host.CreateToolButton("Search/Last"));
			Controls.Add(ToolBar);

			Search = new VirtualSearch();
			Search.ResultCountChanged += OnSearchResultCountChanged;
			Search.ProgressChanged += OnSearchProgressChanged;
			
			ListView.SelectedIndexChanged += OnListViewSelectedIndexChanged;
		}

		protected void OnSearchProgressChanged(object sender, VirtualSearchProgressEventArgs e)
		{
			if(Search.IsBusy)
			{
				if(Progress == null)
				{
					Progress = new ProgressNotification();
					Host.ProgressNotifications.Add(Progress);
				}

				Progress.Update(e.PercentComplete, String.Format("Searching...  ({0:0.##}MB/s)", e.MBps));
			}
			else
			{
				Host.ProgressNotifications.Remove(Progress);
				Progress = null;
			}
		}
		
		protected void OnSearchResultCountChanged(object sender, EventArgs e)
		{
			ListView.VirtualListSize = (int)Search.ResultCount;
		}
		
		protected void OnListRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			if(e.ItemIndex == 0 && ListItemZero != null)
			{
				e.Item = ListItemZero;
				return;
			}
			
			if(e.ItemIndex >= FirstCachedListItemIndex && 
			   e.ItemIndex < FirstCachedListItemIndex + LIST_ITEM_CACHE_SIZE)
			{
				if(ListItemCache[e.ItemIndex - FirstCachedListItemIndex] == null)
				{
					SearchResult result = Search[e.ItemIndex];
					if(result != null)
					{
						e.Item = new ListViewItem(new string[] {result.Position.ToString(), result.Length.ToString(), "abc"});
						ListItemCache[e.ItemIndex - FirstCachedListItemIndex] = e.Item;
						if(e.ItemIndex == 0)
							ListItemZero = e.Item;
					}
					else
						e.Item = new ListViewItem(new string[] {"Searching...", "", ""});
				}
				else
					e.Item = ListItemCache[e.ItemIndex - FirstCachedListItemIndex];
			}
			else
			{
				e.Item = CacheListItems(e.ItemIndex);
				if(e.Item == null)
					e.Item = new ListViewItem(new string[] {"Searching...", "", ""});
			}
		}
		
		protected ListViewItem CacheListItems(int index)
		{
			index -= LIST_ITEM_CACHE_SIZE / 2;
			if(index < 0)
				index = 0;
			
			FirstCachedListItemIndex = index;
			for(int i = 0; i < LIST_ITEM_CACHE_SIZE; ++i)
			{
				SearchResult result = Search[FirstCachedListItemIndex + i];
				if(result != null)
				{
					ListItemCache[i] = new ListViewItem(new string[] {result.Position.ToString(), result.Length.ToString(), "abc"});
					if(FirstCachedListItemIndex + i == 0)
						ListItemZero = ListItemCache[i];
				}
				else
					ListItemCache[i] = null;
			}
			
			return ListItemCache[index - FirstCachedListItemIndex];
		}
		
		protected void ClearListItemCache()
		{
			Array.Clear(ListItemCache, 0, LIST_ITEM_CACHE_SIZE);
			FirstCachedListItemIndex = 0;
		}
		
		protected void OnSearch(object sender, EventArgs e)
		{
			SearchDialog dlg = new SearchDialog();
			dlg.StartPosition = FormStartPosition.CenterParent;
			if(dlg.ShowDialog() != DialogResult.OK)
				return;

			ClearListItemCache();
			Search.Initialize(Host.ActiveView.Document, dlg.Pattern);
			MatchIndicator.Reset(Host.ActiveView.Document.Length, 512);
			ListView.Refresh();
		}
		
		protected void OnListViewSelectedIndexChanged(object sender, EventArgs e)
		{
			if(ListView.SelectedIndices.Count > 0)
			{
				SearchResult result = Search[ListView.SelectedIndices[0]];
				MatchIndicator.SelectedMatch = result.Position;
				Host.ActiveView.Selection.Set(result.Position * 8, (result.Position + result.Length) * 8);
				Host.ActiveView.EnsureVisible(result.Position * 8);
			}
			else
				MatchIndicator.SelectedMatch = -1;
		}
		
		protected void OnFirst(object sender, EventArgs e)
		{
			if(Search.ResultCount > 0)
			{
				ListView.SelectedIndices.Clear();
				ListView.SelectedIndices.Add(0);
				ListView.EnsureVisible(1);
			}
		}
		
		protected void OnPrev(object sender, EventArgs e)
		{
			if(Search.ResultCount > 0 && ListView.SelectedIndices.Count > 0 && ListView.SelectedIndices[0] > 0)
			{
				int prev = ListView.SelectedIndices[0] - 1;
				ListView.SelectedIndices.Clear();
				ListView.SelectedIndices.Add(prev);
				ListView.EnsureVisible(prev);
			}
		}
		
		protected void OnNext(object sender, EventArgs e)
		{
			if(ListView.SelectedIndices.Count > 0)
			{
				int next = ListView.SelectedIndices[0] + 1;
				if(next < Search.ResultCount)
				{
					ListView.SelectedIndices.Clear();
					ListView.SelectedIndices.Add(next);
					ListView.EnsureVisible(next);
				}
			}
		}
		
		protected void OnLast(object sender, EventArgs e)
		{
			if(Search.ResultCount > 0)
			{
				ListView.SelectedIndices.Clear();
				ListView.SelectedIndices.Add((int)Search.ResultCount - 1);
				ListView.EnsureVisible((int)Search.ResultCount - 1);
			}
		}
	}
	
	
	public class SearchPlugin : IPlugin
	{
		Image IPlugin.Image { get { return Settings.Instance.Image("search_16.png"); } }
		string IPlugin.Name { get { return "Search"; } }
		string IPlugin.Description { get { return "Provides a mechanism for searching a document for specific data"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }
		string IPlugin.Copyright { get { return "(c)2008 Stephen Robinson"; } }
		string IPlugin.Url { get { return "http://www.esar.org.uk/"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new SearchPanel(host), "Search", host.Settings.Image("search_16.png"), DefaultWindowPosition.Left, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
