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
using System.Windows.Forms;
using System.Security.Permissions;


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
	
	[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
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
					else if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || 
					                                 DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
						Selection.Set(Selection.End + 8, Selection.End + 8);
					else
						Selection.Set(Selection.End + LayoutDimensions.BitsPerDigit, 
						              Selection.End + LayoutDimensions.BitsPerDigit);
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
					else if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || 
					                                 DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
						Selection.Set(Selection.Start - 8, Selection.Start - 8);
					else
						Selection.Set(Selection.Start - LayoutDimensions.BitsPerDigit, 
						              Selection.Start - LayoutDimensions.BitsPerDigit);
				}
				EnsureVisible(Selection.End);
				break;
			case Keys.Up:
				if(e.Shift)
					Selection.End -= LayoutDimensions.BitsPerRow;
				else
					Selection.Set(Selection.Start - LayoutDimensions.BitsPerRow, 
					              Selection.Start - LayoutDimensions.BitsPerRow);
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
					addr = (((Selection.End / LayoutDimensions.BitsPerRow) + 1) 
					        * LayoutDimensions.BitsPerRow) - LayoutDimensions.BitsPerDigit;
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
						Document.Remove(Selection.BufferRange.Start.Position, 
						                Selection.BufferRange.Start.Position + 1);
					else
					{
						Document.Remove(Selection.BufferRange.Start.Position - 1, 
						                Selection.BufferRange.Start.Position);
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
		
		if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || 
		                            DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
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
							Selection.Set(Selection.Start + LayoutDimensions.BitsPerDigit, 
							              Selection.End + LayoutDimensions.BitsPerDigit);
						}
						break;
					case EditMode.Insert:
						long start = Selection.Start;
						if(start % (_BytesPerWord * 8) == 0)
							AddWord(start, x);
						else
							UpdateWord(start, x);

						start += LayoutDimensions.BitsPerDigit;

						// account for rounding error when using a radix like 10
						// that doesn't have a whole number of bits per digit
						int roundingError = (_BytesPerWord * 8) - (LayoutDimensions.BitsPerDigit * LayoutDimensions.NumWordDigits);
						if((start + roundingError) % (_BytesPerWord * 8) == 0)
							start += roundingError;

						Selection.Set(start, start);
						break;
				}
			}
		}
	}
}

