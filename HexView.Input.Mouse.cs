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
			HexViewHit prevHit = DragStartHit;
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

			// Need to make sure caret moves between data and ascii panes even if the
			// address doesn't change.
			if(prevHit != null && prevHit.Address == hit.Address && prevHit.Type != hit.Type)
				OnSelectionChanged(Selection, EventArgs.Empty);
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
			if(!Dragging && (e.X >= DragStartPos.X + 5 ||
			                 e.X <= DragStartPos.X - 5 ||
			                 e.Y >= DragStartPos.Y + 5 ||
			                 e.Y <= DragStartPos.Y - 5))
			{
				OnBeginDrag(e);
				Dragging = true;
			}
			else if(Dragging)
				OnDrag(e);
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
		HexViewHit hit = HitTest(new Point(e.X, e.Y), DragStartHit.Type);
		
		if(e.Y < 0)
		{
			OnScroll(this, new ScrollEventArgs(ScrollEventType.SmallDecrement, 1));
			DragScrollTimer.Enabled = true;
		}
		else if(e.Y > ClientRectangle.Height)
		{
			OnScroll(this, new ScrollEventArgs(ScrollEventType.SmallIncrement, 1));
			DragScrollTimer.Enabled = true;
		}
		else
			DragScrollTimer.Enabled = false;

		switch(hit.Type)
		{
			case HexViewHit.HitType.Data:
			case HexViewHit.HitType.DataSelection:
			case HexViewHit.HitType.Ascii:
			case HexViewHit.HitType.AsciiSelection:
				Selection.Set(DragStartHit.Address, hit.Address);
				break;
			case HexViewHit.HitType.Address:
				if(hit.Address >= DragStartHit.Address)
					Selection.Set(DragStartHit.Address, hit.Address + LayoutDimensions.BitsPerRow);
				else
					Selection.Set(DragStartHit.Address + LayoutDimensions.BitsPerRow, hit.Address);
				break;
		}
	}

	protected void OnDragScrollTimer(object sender, EventArgs e)
	{
		Point p = PointToClient(Control.MousePosition);
		if(p.Y < 0 || p.Y > ClientRectangle.Height)
			OnDrag(new MouseEventArgs(MouseButtons.None, 0, p.X, p.Y, 0));
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
		DragScrollTimer.Enabled = false;
	}
}

