using System;
using System.Windows.Forms;


public partial class HexView
{
	protected override bool IsInputKey(Keys keys)
	{
		return true;
	}
	
	protected override bool IsInputChar(char c)
	{
		return true;
	}
	
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		// If the Delete key is used as an accelerator it won't be passed to OnKeyDown, etc.
		// Here we intercept it and pass it to OnKeyDown so we can process it as a normal key.
		if(keyData == Keys.Delete)
		{
			OnKeyDown(new KeyEventArgs(keyData));
			return true;
		}
		
		return base.ProcessCmdKey(ref msg, keyData);
	}
	
	protected override void OnKeyDown(KeyEventArgs e)
	{
		switch(e.KeyCode)
		{
			case Keys.Right:
				if(e.Shift)
					Selection.End += LayoutDimensions.BitsPerDigit;
				else
				{
					if(Selection.Length > 0)
						Selection.Set(Selection.End, Selection.End);
					else if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
				        Selection.Set(Selection.End + 8, Selection.End + 8);
					else
						Selection.Set(Selection.End + LayoutDimensions.BitsPerDigit, Selection.End + LayoutDimensions.BitsPerDigit);
				}
				EnsureVisible(Selection.End);
				break;
			case Keys.Left:
				if(e.Shift)
					Selection.End -= LayoutDimensions.BitsPerDigit;
				else
				{
					if(Selection.Length > 0)
						Selection.Set(Selection.Start, Selection.Start);
					else if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
						Selection.Set(Selection.Start - 8, Selection.Start - 8);
					else
						Selection.Set(Selection.Start - LayoutDimensions.BitsPerDigit, Selection.Start - LayoutDimensions.BitsPerDigit);
				}
				EnsureVisible(Selection.End);
				break;
			case Keys.Up:
				if(e.Shift)
					Selection.End -= LayoutDimensions.BitsPerRow;
				else
					Selection.Set(Selection.Start - LayoutDimensions.BitsPerRow, Selection.Start - LayoutDimensions.BitsPerRow);
				EnsureVisible(Selection.End);
				break;
			case Keys.Down:
				if(e.Shift)
					Selection.End += LayoutDimensions.BitsPerRow;
				else
					Selection.Set(Selection.Start + LayoutDimensions.BitsPerRow, Selection.Start + LayoutDimensions.BitsPerRow);
				EnsureVisible(Selection.End);
				break;
			case Keys.PageDown:
				if(e.Shift)
					Selection.End += LayoutDimensions.VisibleLines * LayoutDimensions.BitsPerRow;
				else
					Selection.Set(Selection.Start + LayoutDimensions.VisibleLines * LayoutDimensions.BitsPerRow,
					              Selection.Start + LayoutDimensions.VisibleLines * LayoutDimensions.BitsPerRow);
				EnsureVisible(Selection.End);
				break;
			case Keys.PageUp:
				if(e.Shift)
					Selection.End -= LayoutDimensions.VisibleLines * LayoutDimensions.BitsPerRow;
				else
					Selection.Set(Selection.Start - LayoutDimensions.VisibleLines * LayoutDimensions.BitsPerRow,
					              Selection.Start - LayoutDimensions.VisibleLines * LayoutDimensions.BitsPerRow);
				EnsureVisible(Selection.End);
				break;
			case Keys.Home:
			{
				long addr;
				if((e.Modifiers & Keys.Control) != 0)
					addr = 0;
				else
					addr = (Selection.End / LayoutDimensions.BitsPerRow) * LayoutDimensions.BitsPerRow;
				if(e.Shift)
					Selection.End = addr;
				else
					Selection.Set(addr, addr);
				EnsureVisible(addr);
				break;
			}
			case Keys.End:
			{
				long addr;
				if((e.Modifiers & Keys.Control) != 0)
					addr = Document.Length * 8;
				else
					addr = (((Selection.End / LayoutDimensions.BitsPerRow) + 1) * LayoutDimensions.BitsPerRow) - LayoutDimensions.BitsPerDigit;
				if(e.Shift)
					Selection.End = addr + LayoutDimensions.BitsPerDigit;
				else
					Selection.Set(addr, addr);
				EnsureVisible(addr);		
				break;
			}
			case Keys.Insert:
				if(_EditMode == EditMode.OverWrite)
					EditMode = EditMode.Insert;
				else
					EditMode = EditMode.OverWrite;
				break;
			case Keys.Delete:
			case Keys.Back:
				if(Selection.Length > 0)
				{
					Document.Remove(Selection.BufferRange);
				}
				else
				{
					if(e.KeyCode == Keys.Delete)
						Document.Remove(Selection.BufferRange.Start.Position, Selection.BufferRange.Start.Position + 1);
					else
					{
						Document.Remove(Selection.BufferRange.Start.Position - 1, Selection.BufferRange.Start.Position);
						Selection.Set(Selection.Start - 8, Selection.End - 8);
					}
				}
				break;
			default:
				break;
		}
	}
	
	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		int x = -1;
		
		if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
		{
			x = e.KeyChar;

			if(Selection.Length != 0)
			{
				Document.Insert(Selection.BufferRange.Start, Selection.BufferRange.End, (byte)x);
				Selection.Set(Selection.End, Selection.End);
			}
			else
			{
				switch(_EditMode)
				{
					case EditMode.OverWrite:
						if(Selection.Start / 8 < Document.Length)
						{
							PieceBuffer.Mark end = Document.Marks.Add(Selection.BufferRange.Start.Position + 1);
							Document.Insert(Selection.BufferRange.Start, end, (byte)x);
							Document.Marks.Remove(end);
							Selection.Set(Selection.Start + 8, Selection.End + 8);
						}
						break;
					case EditMode.Insert:
						Document.Insert((byte)x);
						Selection.Set(Selection.Start + 8, Selection.Start + 8);
						break;
				}
			}
		}
		else
		{
			if(e.KeyChar >= '0' && e.KeyChar <= '9')
				x = e.KeyChar - '0';
			else if(e.KeyChar >= 'a' && e.KeyChar <= 'z')
				x = e.KeyChar - 'a' + 10;
			else if(e.KeyChar >= 'A' && e.KeyChar <= 'Z')
				x = e.KeyChar - 'A' + 10;
			
			if(x < 0 || x >= _DataRadix)
				return;
		
			if(Selection.Length != 0)
			{
				Document.Insert(Selection.BufferRange.Start, Selection.BufferRange.End, (byte)x);
				Selection.Set(Selection.End, Selection.End);
			}
			else
			{
				switch(_EditMode)
				{
					case EditMode.OverWrite:
						if(Selection.Start / 8 < Document.Length)
						{
							UpdateWord(Selection.Start, x);
							Selection.Set(Selection.Start + LayoutDimensions.BitsPerDigit, Selection.End + LayoutDimensions.BitsPerDigit);
						}
						break;
					case EditMode.Insert:
						if(Selection.Start % (_BytesPerWord * 8) == 0)
						{
							Console.WriteLine("Inserting at " + Selection.Start); 
							Document.Insert((byte)(x << (_BytesPerWord * 8 - LayoutDimensions.BitsPerDigit)));
						}
						else
						{
							Console.WriteLine("Updating at " + Selection.Start);
							UpdateWord(Selection.Start, x);
						}
						Selection.Set(Selection.Start + LayoutDimensions.BitsPerDigit, Selection.End + LayoutDimensions.BitsPerDigit);
						break;
				}
			}
		}
	}
}
