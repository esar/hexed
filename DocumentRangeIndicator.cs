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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;


public class DocumentRange
{
	protected long _StartPosition;
	public long StartPosition
	{
		get { return _StartPosition; }
		set { _StartPosition = value; }
	}
	
	protected long _EndPosition;
	public long EndPosition
	{
		get { return _EndPosition; }
		set { _EndPosition = value; }
	}
	
	public long Length
	{
		get { return _EndPosition - _StartPosition; }
		set { _EndPosition -= (_EndPosition - _StartPosition) - value; }
	}

	protected Color _Color;
	public Color Color
	{
		get { return _Color; }
		set { _Color = value; }
	}
	
	public DocumentRange() {}
	public DocumentRange(long start, long end)
	{
		_StartPosition = start;
		_EndPosition = end;
	}

	public DocumentRange(long start, long end, Color color)
	{
		_StartPosition = start;
		_EndPosition = end;
		_Color = color;
	}
}


public class DocumentRangeCollection : ICollection<DocumentRange>
{
	protected DocumentRangeIndicator Owner;
	protected List<DocumentRange> Ranges;
	
	public DocumentRangeCollection(DocumentRangeIndicator owner)
	{
		Owner = owner;
		Ranges = new List<DocumentRange>();
	}
	
	public virtual DocumentRange this[int index]
	{
		get { return Ranges[index]; }
		set { Ranges[index] = value; }
	}

	public virtual int Count
	{
		get { return Ranges.Count; }
	}

	public virtual bool IsReadOnly
	{
		get { return false; }
	}

	public virtual void Add(DocumentRange range)
	{
		Ranges.Add(range);
		Owner.Invalidate();
	}

	public void Add(long start, long end)
	{
		Ranges.Add(new DocumentRange(start, end));
		Owner.Invalidate();
	}

	public void Add(long start, long end, Color color)
	{
		Ranges.Add(new DocumentRange(start, end, color));
		Owner.Invalidate();
	}

	public virtual bool Remove(DocumentRange range) 
	{
		bool result = Ranges.Remove(range);
		if(result)
			Owner.Invalidate();
		return result;
	}

	public bool Contains(DocumentRange range)
	{
		return Ranges.Contains(range);
	}
 
	public virtual void CopyTo(DocumentRange[] dest, int index)
	{
		Ranges.CopyTo(dest, index);
	}

	public virtual void Clear()
	{
		Ranges.Clear();
		Owner.Invalidate();
	}

	public virtual IEnumerator<DocumentRange> GetEnumerator()
	{
		return Ranges.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Ranges.GetEnumerator();
	}
}

public class DocumentRangeIndicator : UserControl
{
	protected DocumentRange _SelectedRange;
	public DocumentRange SelectedRange
	{
		get { return _SelectedRange; }
		set { _SelectedRange = value; Invalidate(); }
	}
	
	protected DocumentRangeCollection _Ranges;
	public DocumentRangeCollection Ranges
	{
		get { return _Ranges; }
	}
	
	protected long _DocumentLength;
	public long DocumentLength
	{
		get { return _DocumentLength; }
		set { _DocumentLength = value; Invalidate(); }
	}
	
	protected Orientation _Orientation;
	public Orientation Orientation
	{
		get { return _Orientation; }
		set 
		{ 
			_Orientation = value;
		}
	}
	
	protected SolidBrush Brush;
	protected SolidBrush HighlightBrush;
	
	public DocumentRangeIndicator()
	{
		_Ranges = new DocumentRangeCollection(this);
		Brush = new SolidBrush(SystemColors.ButtonShadow);
		HighlightBrush = new SolidBrush(SystemColors.Highlight);
		_Orientation = Orientation.Vertical;
		Width = 25;
		Height = 25;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		const int border = 5;
		
		base.OnPaint(e);

		if(_DocumentLength > 0)
		{
			float scaleRatio = (float)(ClientSize.Height - 2 * border) / _DocumentLength;
			foreach(DocumentRange r in _Ranges)
			{
				RectangleF rect = new RectangleF(border, 
				                                 (float)r.StartPosition * scaleRatio + border, 
				                                 ClientSize.Width - border * 2, 
				                                 (float)(r.EndPosition - r.StartPosition) * scaleRatio);
				if(rect.Height < 1)
					rect.Height = 1;

				if(r.Color != Color.Empty)
				{
					Brush brush = new SolidBrush(r.Color);
					e.Graphics.FillRectangle(brush, rect);
					brush.Dispose();
				}
				else
					e.Graphics.FillRectangle(Brush, rect);
			}
			
			if(_SelectedRange != null)
			{
				RectangleF rect = new RectangleF(border, 
				                                 (float)_SelectedRange.StartPosition * scaleRatio + border, 
				                                 ClientSize.Width - border * 2, 
				                                 (float)(_SelectedRange.EndPosition - _SelectedRange.StartPosition) * scaleRatio);
				if(rect.Height < 1)
					rect.Height = 1;
				e.Graphics.FillRectangle(HighlightBrush, rect);
			}				
		}
		
		e.Graphics.DrawRectangle(SystemPens.ButtonShadow, new Rectangle(border, border, ClientSize.Width - 2*border, ClientSize.Height - 2*border));
	}
}
