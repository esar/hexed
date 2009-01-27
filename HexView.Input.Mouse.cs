using System;
using System.Drawing;
using System.Windows.Forms;


public partial class HexView
{
	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		ScrollToPixel(ScrollPosition - e.Delta);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);

		Focus();
		
		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		if(((int)e.Button & (int)MouseButtons.Left) != 0)
		{
			DragStartHit = hit;
			DragStartPos = new Point(e.X, e.Y);
			switch(hit.Type)
			{
				case HexViewHit.HitType.Data:
				case HexViewHit.HitType.DataSelection:
				case HexViewHit.HitType.Ascii:
				case HexViewHit.HitType.AsciiSelection:
					if((ModifierKeys & Keys.Shift) == Keys.Shift)
						Selection.End = hit.Address;
					else
						Selection.Set(hit.Address, hit.Address);
					break;
				case HexViewHit.HitType.Address:
					if((ModifierKeys & Keys.Shift) == Keys.Shift)
						Selection.End = hit.Address + LayoutDimensions.BitsPerRow;
					else
						Selection.Set(hit.Address, hit.Address + LayoutDimensions.BitsPerRow);
					break;
			}
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);

		if(((int)e.Button & (int)MouseButtons.Left) != 0)
		{
			if(Dragging)
			{
				OnEndDrag(e);
				Dragging = false;
			}
		}
		else if(e.Button == MouseButtons.Right)
		{
			if(ContextMenu != null)
			{
				Point p = new Point(e.X, e.Y);
				ContextMenu(this, new ContextMenuEventArgs(p, HitTest(p)));
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		switch(hit.Type)
		{
			case HexViewHit.HitType.Address:
				Cursor = Cursors.Hand;
				break;
			case HexViewHit.HitType.Data:
			case HexViewHit.HitType.Ascii:
				Cursor = Cursors.IBeam;
				break;
			default:
				Cursor = Cursors.Arrow;
				break;
		}

		base.OnMouseMove(e);

		if(((int)e.Button & (int)MouseButtons.Left) != 0)
		{
			if(!Dragging && (	e.X >= DragStartPos.X + 5 ||
								e.X <= DragStartPos.X - 5 ||
								e.Y >= DragStartPos.Y + 5 ||
								e.Y <= DragStartPos.Y - 5))
			{
				OnBeginDrag(e);
				Dragging = true;
			}
			else if(Dragging)
			{
				OnDrag(e);
			}
		}
	}

	protected void OnBeginDrag(MouseEventArgs e)
	{
		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		if(hit.Type == HexViewHit.HitType.Data)
		{
			Capture = true;
			Selection.Set(hit.Address, hit.Address);
		}
		else if(hit.Type == HexViewHit.HitType.Address)
		{
			Capture = true;
			Selection.Set(hit.Address, hit.Address + LayoutDimensions.BitsPerRow);
		}
	}

	protected void OnDrag(MouseEventArgs e)
	{
		if(e.Y < 0)
		{
			OnScroll(this, new ScrollEventArgs(ScrollEventType.SmallDecrement, 1));
		}
		else if(e.Y > ClientRectangle.Height)
		{
			OnScroll(this, new ScrollEventArgs(ScrollEventType.SmallIncrement, 1));
//			Selection.Set(DragStartHit.Address, Selection.End + (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits);
		}
		else
		{
			HexViewHit hit = HitTest(new Point(e.X, e.Y));
			switch(hit.Type)
			{
				case HexViewHit.HitType.Data:
				case HexViewHit.HitType.DataSelection:
					Selection.Set(DragStartHit.Address, hit.Address + (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits);
					break;
				case HexViewHit.HitType.Ascii:
				case HexViewHit.HitType.AsciiSelection:
					Selection.Set(DragStartHit.Address, hit.Address + 8);
					break;
				case HexViewHit.HitType.Address:
					if(hit.Address >= DragStartHit.Address)
						Selection.Set(DragStartHit.Address, hit.Address + LayoutDimensions.BitsPerRow);
					else
						Selection.Set(DragStartHit.Address + LayoutDimensions.BitsPerRow, hit.Address);
					break;
			}
		}
	}

	protected void OnEndDrag(MouseEventArgs e)
	{
		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		if(hit.Type == HexViewHit.HitType.Data)
			Selection.End = hit.Address;
		else if(hit.Type == HexViewHit.HitType.Address)
		{
			if(hit.Address >= DragStartHit.Address)
				Selection.End = hit.Address + LayoutDimensions.BitsPerRow;
			else
				Selection.End = hit.Address;
		}
		
		Capture = false;
	}
}
