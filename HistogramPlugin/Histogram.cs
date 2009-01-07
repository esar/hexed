using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace HistogramPlugin
{
	public class HistogramItem
	{
		public float Value;
		public Color Color;
	}
	
	public class HistogramControl : UserControl
	{
		private HistogramItem[] _Data;
		private float			_Max;
		
		public HistogramItem[] Data
		{
			get { return _Data; }
			set 
			{
				_Data = value;
				_Max = 0;
				foreach(HistogramItem i in _Data)
					if(i.Value > _Max)
					_Max = i.Value;
				Invalidate();
			}
		}
		
		public HistogramControl()
		{
			
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			
			SolidBrush brush = null;
			float barWidth = (float)ClientRectangle.Width / (float)_Data.Length;
			for(int i = 0; i < _Data.Length; ++i)
			{
				if(brush == null || _Data[i].Color != brush.Color)
				{
					if(brush != null)
						brush.Dispose();
					brush = new SolidBrush(_Data[i].Color);
				}
				
				float scale = (float)ClientRectangle.Height / _Max;
				
				RectangleF rect = new RectangleF(barWidth * i, 
				                                 ClientRectangle.Height - (_Data[i].Value * scale),
				                                 barWidth, 
				                                 _Data[i].Value * scale);
				e.Graphics.FillRectangle(brush, rect);
			}
			if(brush != null)
				brush.Dispose();
		}
	}
	
	public class ResultWindow : Form
	{
		ListView List = new ListView();
		
		public ResultWindow(HistogramItem[] data)
		{
			HistogramControl graph = new HistogramControl();
			graph.Dock = DockStyle.Fill;
			graph.Data = data;
			Controls.Add(graph);
			
			List.View = View.Details;
			List.Columns.Add("Hex");
			List.Columns.Add("Decimal");
			List.Columns.Add("Octal");
			List.Columns.Add("Binary");
			List.Columns.Add("Percent");
			
			for(int i = 0; i < 0x100; ++i)
			{
				ListViewItem item = List.Items.Add(i.ToString());
				item.SubItems.Add(i.ToString());
				item.SubItems.Add(i.ToString());
				item.SubItems.Add(i.ToString());
				item.SubItems.Add((data[i].Value * 100).ToString());
			}
			
			List.Dock = DockStyle.Bottom;
			Controls.Add(List);
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
			
			ToolStripMenuItem item = host.AddMenuItem("Tools/Analyse/Histogram");
			item.Click += OnHistogram;		
		}
		
		void IPlugin.Dispose()
		{
		}
		
		
		void OnHistogram(object sender, EventArgs e)
		{
			if(Host.ActiveView != null)
			{
				PieceBuffer buffer = Host.ActiveView.Document;
				long max = 0;
				long[] buckets = new long[0x100];
				
				for(int i = 0; i < 0x100; ++i)
					buckets[i] = 0;
				
				long len = Host.ActiveView.Document.Length;
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

				HistogramItem[] data = new HistogramItem[0x100];
				for(int i = 0; i < 0x100; ++i)
				{
					HistogramItem item = new HistogramItem();
					item.Value = (float)buckets[i];
					if(i == 'a' || i == 'e' || i == 'u' || i == 'i' || i == 'o')
						item.Color = Color.Red;
					else
						item.Color = Color.Blue;
					data[i] = item;
				}

				ResultWindow results = new ResultWindow(data);
				results.Show();
			}
		}
	}
}
