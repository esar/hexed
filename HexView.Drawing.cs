using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

#if !MONO
using System.Runtime.InteropServices;
#endif


public partial class HexView
{
	protected void ScrollToPixel(long NewPosition)
	{
		if(NewPosition < 0)
			NewPosition = 0;
		else if(NewPosition > ScrollHeight)
			NewPosition = ScrollHeight;

#if MONO
		Invalidate();
#else
		if(	NewPosition < ScrollPosition - ClientSize.Height ||
			NewPosition > ScrollPosition + ClientSize.Height)
		{
			Invalidate();
		}
		else
		{
			ScrollWindowEx(	this.Handle, 
							0, 
							(int)(ScrollPosition - NewPosition), 
							new RECT(0, 0, ClientSize.Width, ClientSize.Height), 
							null, 
							IntPtr.Zero, 
							null, 
							2);
			if(NewPosition < ScrollPosition)
				Invalidate(new Rectangle(0, 0, ClientSize.Width, (int)(ScrollPosition - NewPosition)));
			else
				Invalidate(new Rectangle(0, ClientSize.Height - (int)(NewPosition - ScrollPosition), ClientSize.Width, (int)(NewPosition - ScrollPosition)));
		}
#endif

		ScrollPosition = NewPosition;
		VScroll.Value = (int)((double)ScrollPosition / ScrollScaleFactor);

		InsertCaret.Position = AddressToClientPoint(Selection.Start);
	}

	private RectangleF MeasureSubString(Graphics graphics, string text, int start, int length, Font font)
	{
		StringFormat		format	= new StringFormat ();
		RectangleF			rect	= new RectangleF(0, 0, 1000, 1000);
		CharacterRange[]	ranges	= { new CharacterRange(start, length) };
		Region[]			regions	= new Region[1];

		format.SetMeasurableCharacterRanges(ranges);
		regions = graphics.MeasureCharacterRanges(text, font, rect, format);
		rect    = regions[0].GetBounds(graphics);
		rect.Width += 1;
		
		return rect;
	}

	public HexViewHit HitTest(Point p)
	{
		if(	p.X >= LayoutDimensions.AddressRect.Left &&
			p.X <= LayoutDimensions.AddressRect.Right &&
			p.Y >= LayoutDimensions.AddressRect.Top &&
			p.Y <= LayoutDimensions.AddressRect.Bottom)
		{
			return HitTestAddress(p);
		}
		else if(p.X >= LayoutDimensions.DataRect.Left &&
				p.X <= LayoutDimensions.DataRect.Right &&
				p.Y >= LayoutDimensions.DataRect.Top &&
				p.Y <= LayoutDimensions.DataRect.Bottom)
		{
			return HitTestData(p);
		}
		else if(p.X >= LayoutDimensions.AsciiRect.Left &&
				p.X <= LayoutDimensions.AsciiRect.Right &&
				p.Y >= LayoutDimensions.AsciiRect.Top &&
				p.Y <= LayoutDimensions.AsciiRect.Bottom)
		{
			return HitTestAscii(p);
		}
		else
			return new HexViewHit(HexViewHit.HitType.Unknown);
	}
	
	public HexViewHit HitTest(Point p, HexViewHit.HitType type)
	{
		switch(type)
		{
			case HexViewHit.HitType.Address:
				return HitTestAddress(p);
			case HexViewHit.HitType.Data:
			case HexViewHit.HitType.DataSelection:
				return HitTestData(p);
			case HexViewHit.HitType.Ascii:
			case HexViewHit.HitType.AsciiSelection:
				return HitTestAscii(p);
			default:
				return new HexViewHit(HexViewHit.HitType.Unknown);
		}
	}
	
	protected HexViewHit HitTestAddress(Point p)
	{
		long line = (long)((double)(p.Y + ScrollPosition) / LayoutDimensions.WordSize.Height);
		long lineAddress = line * LayoutDimensions.BitsPerRow;
		if(lineAddress < 0)
			lineAddress = 0;
		else if(lineAddress > Document.Length)
			lineAddress = Document.Length;
		return new HexViewHit(HexViewHit.HitType.Address, lineAddress);
	}
	
	protected HexViewHit HitTestData(Point p)
	{
		long line = (long)((double)(p.Y + ScrollPosition) / LayoutDimensions.WordSize.Height);
		long lineAddress = line * LayoutDimensions.BitsPerRow;
		float halfDigitWidth = LayoutDimensions.WordSize.Width / LayoutDimensions.NumWordDigits / 2;
		
		float x = p.X;// - LayoutDimensions.DataRect.Left;
		int word = 0;
		while(word < LayoutDimensions.WordRects.Length && x > LayoutDimensions.WordRects[word].Right)
			++word;
		if(word >= LayoutDimensions.WordRects.Length || (word > 0 && x < LayoutDimensions.WordRects[word].Left))
			--word;
		x -= LayoutDimensions.WordRects[word].Left;
		long digitAddress = word * _BytesPerWord * 8;
		
		Graphics g = CreateGraphics();
		int i;
		for(i = 1; i <= LayoutDimensions.NumWordDigits; ++i)
		{
			RectangleF r = MeasureSubString(g, "00000000000000000000000000000000000000000000000000000000000000000", 0, i, _Font);
			if(x <= r.Width - halfDigitWidth)
				break;
		}
		g.Dispose();
		digitAddress += (i - 1) * ((_BytesPerWord * 8) / LayoutDimensions.NumWordDigits);
		digitAddress += lineAddress;
		if(digitAddress < 0)
			digitAddress = 0;
		else if(digitAddress > Document.Length * 8)
			digitAddress = Document.Length * 8;

		if(digitAddress >= Selection.Start && digitAddress < Selection.End)
			return new HexViewHit(HexViewHit.HitType.DataSelection, digitAddress, i, new Point(0, 0));
		else
			return new HexViewHit(HexViewHit.HitType.Data, digitAddress, i, new Point(0, 0));
	}
	
	protected HexViewHit HitTestAscii(Point p)
	{
		long line = (long)((double)(p.Y + ScrollPosition) / LayoutDimensions.WordSize.Height);
		long lineAddress = line * LayoutDimensions.BitsPerRow;
		float halfDigitWidth = LayoutDimensions.WordSize.Width / LayoutDimensions.NumWordDigits / 2;

		Graphics g = CreateGraphics();
		int i;
		for(i = 1; i <= LayoutDimensions.BitsPerRow / 8; ++i)
		{
			RectangleF r = MeasureSubString(g, "00000000000000000000000000000000000000000000000000000000000000000", 0, i, _Font);
			if(p.X - LayoutDimensions.AsciiRect.Left + halfDigitWidth <= r.Width)
				break;
		}
		g.Dispose();
		
		long address = lineAddress + ((i - 1) * 8);
		if(address < 0)
			address = 0;
		else if(address > Document.Length * 8)
			address = Document.Length * 8;
		
		if(address >= Selection.Start && address < Selection.End)
			return new HexViewHit(HexViewHit.HitType.AsciiSelection, address, i, new Point(0, 0));
		else
			return new HexViewHit(HexViewHit.HitType.Ascii, address, i, new Point(0, 0));
	}


	protected void RecalcDimensions()
	{
		Graphics g = CreateGraphics();
		const string digits = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

		if(_BytesPerWord < 8)
		{
			ulong maxWordValue = ((ulong)1 << (_BytesPerWord * 8)) - 1;
			LayoutDimensions.NumWordDigits = (int)Math.Log(maxWordValue, _DataRadix) + 1; //(int)(Math.Log(maxWordValue) / Math.Log(_DataRadix)) + 1;
		}
		else
			LayoutDimensions.NumWordDigits = (int)Math.Log(UInt64.MaxValue, _DataRadix); //(int)(Math.Log(maxWordValue) / Math.Log(_DataRadix)) + 1;
		LayoutDimensions.BitsPerDigit = (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits;
		LayoutDimensions.WordSize = MeasureSubString(g, digits, 0, LayoutDimensions.NumWordDigits, _Font).Size; //g.MeasureString(digits.Substring(0, LayoutDimensions.NumWordDigits), _Font);

		// Calculate number of visible lines
		LayoutDimensions.VisibleLines = (int)Math.Ceiling(ClientSize.Height / LayoutDimensions.WordSize.Height);

		// Calculate width of largest address
		LayoutDimensions.NumAddressDigits = (int)(Math.Log(Document.Length) / Math.Log(_AddressRadix)) + 1;
		SizeF AddressSize = MeasureSubString(g, digits, 0, LayoutDimensions.NumAddressDigits, _Font).Size;
		LayoutDimensions.AddressRect = new RectangleF(	0,
														0,
														AddressSize.Width,
														ClientSize.Height);

		float RemainingWidth =	ClientSize.Width - 
								LayoutDimensions.AddressRect.Width -
								LayoutDimensions.LeftGutterWidth -
								LayoutDimensions.RightGutterWidth;

		if(VScroll.Visible)
			RemainingWidth -= VScroll.Width;

		int GroupsPerRow = 0;
		int WordsPerRow = 0;
		float wordGroupSpacing = LayoutDimensions.WordGroupSpacing;
		if(_WordsPerGroup == 1)
			wordGroupSpacing = 0;
		if(_WordsPerLine < 0)
		{
			float width = 0;
			
			do
			{
				++GroupsPerRow;
				WordsPerRow = GroupsPerRow * _WordsPerGroup;
				LayoutDimensions.BitsPerRow = WordsPerRow * _BytesPerWord * 8;
				width = (((LayoutDimensions.WordSize.Width + LayoutDimensions.WordSpacing) * _WordsPerGroup) + wordGroupSpacing) * GroupsPerRow;
				width += LayoutDimensions.RightGutterWidth;
				width += MeasureSubString(g, digits, 0, LayoutDimensions.BitsPerRow / 8, _Font).Width;
	
			} while(width <= RemainingWidth);
			
			if(GroupsPerRow > 1)
				--GroupsPerRow;
			
			WordsPerRow = GroupsPerRow * _WordsPerGroup;
			LayoutDimensions.BitsPerRow = WordsPerRow * _BytesPerWord * 8;
		}
		else
		{
			WordsPerRow = _WordsPerLine;
			GroupsPerRow = _WordsPerLine / _WordsPerGroup;
			LayoutDimensions.BitsPerRow = _WordsPerLine * _BytesPerWord * 8;
		}
		
		float x = LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth;
		LayoutDimensions.WordRects = new RectangleF[WordsPerRow];
		for(int group = 0; group < GroupsPerRow; ++group)
		{
			for(int word = 0; word < _WordsPerGroup; ++word)
			{
				LayoutDimensions.WordRects[(group * _WordsPerGroup) + word] = new RectangleF(x, 0, LayoutDimensions.WordSize.Width, LayoutDimensions.WordSize.Height);
				x += LayoutDimensions.WordSize.Width + LayoutDimensions.WordSpacing;
			}
			if(_WordsPerGroup > 1)
				x -= LayoutDimensions.WordSpacing;
			x += wordGroupSpacing;
		}
		x -= wordGroupSpacing;
			
		
		
		LayoutDimensions.DataRect = new RectangleF(	LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth,
													0,
													x - (LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth),
													ClientSize.Height);


		SizeF AsciiSize = MeasureSubString(g, digits, 0, LayoutDimensions.BitsPerRow / 8, _Font).Size;
		LayoutDimensions.AsciiRect = new RectangleF(	LayoutDimensions.DataRect.Right + LayoutDimensions.RightGutterWidth,
														0,
														AsciiSize.Width,
														ClientSize.Height);

		const int IntMax = 0x6FFFFFFF;
		ScrollHeight = (long)((((Document.Length * 8) / LayoutDimensions.BitsPerRow) + 1) * LayoutDimensions.WordSize.Height);

		if(ScrollHeight <= ClientSize.Height)
		{
			VScroll.Visible = false;
		}
		else
		{
			if(ScrollHeight > IntMax)
			{
				VScroll.Maximum = IntMax;
				ScrollScaleFactor = (double)(ScrollHeight) / (double)IntMax;
			}
			else
			{
				VScroll.Maximum = (int)ScrollHeight;
				ScrollScaleFactor = 1;
			}
			VScroll.SmallChange = 5;
			VScroll.LargeChange = ClientSize.Height;

			VScroll.Visible = true;
		}

		if(_EditMode == EditMode.Insert)
			InsertCaret.Size = new Size(2, (int)LayoutDimensions.WordSize.Height);
		else
			InsertCaret.Size = new Size((int)(LayoutDimensions.WordSize.Width / LayoutDimensions.NumWordDigits) + 1, 
	                            		(int)LayoutDimensions.WordSize.Height);

		g.Dispose();
	}

	protected Point AddressToClientPoint(long address)
	{
		double y = address / LayoutDimensions.BitsPerRow;
		y *= LayoutDimensions.WordSize.Height;
		y -= ScrollPosition;

		address -= (address / LayoutDimensions.BitsPerRow) * LayoutDimensions.BitsPerRow;
		
		int word = (int)(address / 8) / _BytesPerWord;
		float x = LayoutDimensions.WordRects[word].Left;
		
		address -= word * _BytesPerWord * 8;
		address /= (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits;
		if(address >= LayoutDimensions.NumWordDigits)
			address = LayoutDimensions.NumWordDigits - 1;
		Graphics g = CreateGraphics();
		x += MeasureSubString(g, "00000000000000000000000000000000000000000000000000000000000000000", 0, (int)address, _Font).Width;
		g.Dispose();
			
		return new Point((int)x + 1, (int)(y + LayoutDimensions.WordSize.Height));
	}
	
	protected Point AddressToClientPointAscii(long address)
	{
		double y = address / LayoutDimensions.BitsPerRow;
		y *= LayoutDimensions.WordSize.Height;
		y -= ScrollPosition;

		address -= (address / LayoutDimensions.BitsPerRow) * LayoutDimensions.BitsPerRow;
		address /= 8;
		
		float x = LayoutDimensions.AsciiRect.Left;
		
		Graphics g = CreateGraphics();
		x += MeasureSubString(g, "00000000000000000000000000000000000000000000000000000000000000000", 0, (int)address, _Font).Width;
		g.Dispose();
			
		return new Point((int)x, (int)(y + LayoutDimensions.WordSize.Height));
	}
	
	
	protected GraphicsPath CreateRoundedRectPath(RectangleF rect)
	{
		Size	CornerSize = new Size(10, 10);
		Point	Margin = new Point(2, 2);

		GraphicsPath path = new GraphicsPath();

		RectangleF tl = new RectangleF(rect.Left, rect.Top, CornerSize.Width, CornerSize.Height);
		RectangleF tr = new RectangleF(rect.Right - CornerSize.Width, rect.Top, CornerSize.Width, CornerSize.Height);
		RectangleF bl = new RectangleF(rect.Left, rect.Bottom - CornerSize.Height, CornerSize.Width, CornerSize.Height);
		RectangleF br = new RectangleF(rect.Right - CornerSize.Width, rect.Bottom - CornerSize.Height, CornerSize.Width, CornerSize.Height);

		path.AddArc(tl, 180, 90);
		path.AddArc(tr, 270, 90);
		path.AddArc(br, 360, 90);
		path.AddArc(bl, 90, 90);
		path.CloseAllFigures();

		return path;
	}

	protected delegate Point AddressToPointDelegate(long address);
	protected GraphicsPath CreateRoundedSelectionPath(long startAddress, long endAddress, float yOffset, AddressToPointDelegate addrToPoint)
	{
		if(startAddress > endAddress)
		{
			long tmp = startAddress;
			startAddress = endAddress;
			endAddress = tmp;
		}
		
		--endAddress;

		yOffset = 0 - yOffset;

		long startRow = startAddress / LayoutDimensions.BitsPerRow;
		long startCol = startAddress - (startRow * LayoutDimensions.BitsPerRow);
		long endRow = endAddress / LayoutDimensions.BitsPerRow;
		long endCol = (endAddress - (endRow * LayoutDimensions.BitsPerRow)) + LayoutDimensions.BitsPerDigit;
		long firstVisibleRow = (long)(ScrollPosition / LayoutDimensions.WordSize.Height);
		long lastVisibleRow = (long)((ScrollPosition + ClientSize.Height) / LayoutDimensions.WordSize.Height);


		RectangleF r = new RectangleF(0, 0, 10, 10);
		GraphicsPath path = new GraphicsPath();

		if(startRow < firstVisibleRow)
		{
			// square top
			//
			// +-----------------------+
			// |                       |

			PointF a = addrToPoint((firstVisibleRow - 1) * LayoutDimensions.BitsPerRow);
			a.Y += -10 + yOffset;
			PointF b = addrToPoint((firstVisibleRow - 1) * LayoutDimensions.BitsPerRow + LayoutDimensions.BitsPerRow - 1);
			b.Y += -10 + yOffset;
			b.X += 10;
			path.AddLine(a, b);
		}
		else if(startCol > 0 && endRow > startRow)
		{
			// curved top
			//
			//    +--------------------+
			//    |                    |
			// +--+                    |
			// |                       |

			r.Location = addrToPoint((startRow + 1) * LayoutDimensions.BitsPerRow);
			r.Offset(0, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 180, 90);
			r.Location = addrToPoint(startAddress);
			r.Offset(-10, -10 + yOffset);
			path.AddArc(r, 90, -90);
			r.Location = addrToPoint(startAddress);
			r.Offset(0, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 180, 90);
			r.Location = addrToPoint(startRow * LayoutDimensions.BitsPerRow + LayoutDimensions.BitsPerRow - 1);
			r.Offset(0, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 270, 90);
		}
		else
		{
			// curved top
			//
			// +-----------------------+
			// |                       |

			r.Location = addrToPoint(startAddress);
			r.Offset(0, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 180, 90);
			if(endRow > startRow)
				r.Location = addrToPoint((startRow * LayoutDimensions.BitsPerRow) + LayoutDimensions.BitsPerRow - 1);
			else
				r.Location = addrToPoint(endAddress);
			r.Offset(0, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 270, 90);
		}

		if(endRow > lastVisibleRow)
		{
			// square bottom
			//
			// |                       |
			// +-----------------------+
			
			PointF a = addrToPoint((lastVisibleRow + 1) * LayoutDimensions.BitsPerRow);
			a.Y += 10 + yOffset;
			PointF b = addrToPoint((lastVisibleRow + 1) * LayoutDimensions.BitsPerRow + LayoutDimensions.BitsPerRow - 1);
			b.Y += 10 + yOffset;
			b.X += 10;
			path.AddLine(b, a);		
		}
		else if(endCol < LayoutDimensions.BitsPerRow && endRow > startRow)
		{
			// curved bottom
			//
			// |                       |
			// |                    +--+
			// |                    |
			// +--------------------+

			r.Location = addrToPoint((endRow - 1) * LayoutDimensions.BitsPerRow + LayoutDimensions.BitsPerRow - 1);
			r.Offset(0,  - 10 + yOffset);
			path.AddArc(r, 0, 90);
			r.Location = addrToPoint(endAddress);
			r.Offset(10, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 270, -90);
			r.Offset(-10, LayoutDimensions.WordSize.Height - 10 + yOffset);
			path.AddArc(r, 0, 90);
			r.Location = addrToPoint(endRow * LayoutDimensions.BitsPerRow);
			r.Offset(0, -10 + yOffset);
			path.AddArc(r, 90, 90);
		}
		else
		{
			// curved bottom
			//
			// |                       |
			// +-----------------------+

			r.Location = addrToPoint(endAddress);
			r.Offset(0, -10 + yOffset);
			path.AddArc(r, 0, 90);
			if(endRow > startRow)
				r.Location = addrToPoint(endRow * LayoutDimensions.BitsPerRow);
			else
				r.Location = addrToPoint(startAddress);
			r.Offset(0, -10 + yOffset);
			path.AddArc(r, 90, 90);
		}

		path.CloseAllFigures();
		return path;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

		double pixelOffset = ScrollPosition + e.ClipRectangle.Top;
		long firstLine = (long)(pixelOffset / LayoutDimensions.WordSize.Height);

		double drawingOffset = e.ClipRectangle.Top - (pixelOffset % LayoutDimensions.WordSize.Height);
		long dataOffset = firstLine * (LayoutDimensions.BitsPerRow / 8);
		int visibleLines = (int)Math.Ceiling((e.ClipRectangle.Bottom - drawingOffset) / LayoutDimensions.WordSize.Height) + 1;
		if(visibleLines > ((Document.Length - dataOffset) / (LayoutDimensions.BitsPerRow / 8)) + 1)
			visibleLines = (int)((Document.Length - dataOffset) / (LayoutDimensions.BitsPerRow / 8)) + 1;
		long dataEndOffset = dataOffset + (visibleLines * (LayoutDimensions.BitsPerRow / 8));
		if(dataEndOffset > Document.Length)
			dataEndOffset = Document.Length;

		e.Graphics.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.ButtonFace)), 0, 0, LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, LayoutDimensions.AddressRect.Height);
		e.Graphics.DrawLine(new Pen(Color.FromKnownColor(KnownColor.ButtonShadow)), LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, 0, LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, LayoutDimensions.AddressRect.Height);

		if(_EvenColumnColor != Color.Transparent && _EvenColumnColor != BackColor)
		{
			for(int i = 0; i < LayoutDimensions.WordRects.Length; ++i)
				if(i % 2 == 0)
					e.Graphics.FillRectangle(new SolidBrush(_EvenColumnColor), new RectangleF(LayoutDimensions.WordRects[i].Left,
					                                                                          0,
					                                                                          LayoutDimensions.WordRects[i].Width,
					                                                                          ClientRectangle.Height));
		}

		if(_OddColumnColor != Color.Transparent && _OddColumnColor != BackColor)
		{
			for(int i = 0; i < LayoutDimensions.WordRects.Length; ++i)
				if(i % 2 == 0)
					e.Graphics.FillRectangle(new SolidBrush(_OddColumnColor), new RectangleF(LayoutDimensions.WordRects[i].Left,
					                                                                         0,
					                                                                         LayoutDimensions.WordRects[i].Width,
					                                                                         ClientRectangle.Height));
		}
		
		foreach(SelectionRange sel in Highlights)
		{
			if(sel.Start / 8 < dataEndOffset && sel.End / 8 > dataOffset)
			{
				GraphicsPath p = CreateRoundedSelectionPath(sel.Start, sel.End, (float)0, AddressToClientPoint);
				e.Graphics.FillPath(new SolidBrush(sel.BackColor), p);
				e.Graphics.DrawPath(new Pen(sel.BorderColor, sel.BorderWidth), p);

				p = CreateRoundedSelectionPath(sel.Start, sel.End, (float)0, AddressToClientPointAscii);
				e.Graphics.FillPath(new SolidBrush(sel.BackColor), p);
				e.Graphics.DrawPath(new Pen(sel.BorderColor, sel.BorderWidth), p);
			}
		}
		
		if(Selection.Length != 0)
		{
			if(Selection.Start / 8 < dataEndOffset && Selection.End / 8 > dataOffset)
			{
				GraphicsPath p = CreateRoundedSelectionPath(Selection.Start, Selection.End, (float)0, AddressToClientPoint);
				e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 200, 255, 200)), p);
				e.Graphics.DrawPath(new Pen(Color.LightGreen, 1), p);

				p = CreateRoundedSelectionPath(Selection.Start, Selection.End, (float)0, AddressToClientPointAscii);
				e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 200, 255, 200)), p);
				e.Graphics.DrawPath(new Pen(Color.LightGreen, 1), p);			
			}
		}

		string str;
		for(int line = 0; line < visibleLines; ++line)
		{
			str = IntToRadixString((ulong)dataOffset, _AddressRadix, LayoutDimensions.NumAddressDigits);
			RectangleF rect = new RectangleF(LayoutDimensions.AddressRect.Left, (float)drawingOffset + line * LayoutDimensions.WordSize.Height, LayoutDimensions.AddressRect.Width, LayoutDimensions.WordSize.Height);
			e.Graphics.DrawString(str, _Font, _Brush, rect.Location);


			str = "";
			rect = new RectangleF(	LayoutDimensions.DataRect.Left, 
									(float)drawingOffset + line * LayoutDimensions.WordSize.Height,
									LayoutDimensions.WordSize.Width,
									LayoutDimensions.WordSize.Height);


			int off = 0;
			foreach(RectangleF wordRect in LayoutDimensions.WordRects)
			{
				if(dataOffset + off < Document.Length)
				{
					str = IntToRadixString(GetWord((dataOffset + off) * 8), _DataRadix, LayoutDimensions.NumWordDigits);
					e.Graphics.DrawString(str, _Font, _Brush, wordRect.Left, rect.Top);
				}
				else
					e.Graphics.DrawString(String.Empty.PadLeft(LayoutDimensions.NumWordDigits, '.'), _Font, _GrayBrush, wordRect.Left, rect.Top);
				
				off += _BytesPerWord;
			}

			str = String.Empty;
			long i;
			for(i = 0; i < LayoutDimensions.BitsPerRow / 8 && dataOffset + i < Document.Length; ++i)
				str += AsciiChar[Document[dataOffset + i]];
			rect = new RectangleF(LayoutDimensions.AsciiRect.Left, (float)drawingOffset + line * LayoutDimensions.WordSize.Height, LayoutDimensions.AsciiRect.Width, LayoutDimensions.WordSize.Height);
			e.Graphics.DrawString(str, _Font, _Brush, rect.Location);
			
			if(i < LayoutDimensions.BitsPerRow / 8)
			{
				str = String.Empty.PadLeft((int)i, ' ');
				str = str.PadRight(LayoutDimensions.BitsPerRow / 8, '.');
				e.Graphics.DrawString(str, _Font, _GrayBrush, rect.Location);
			}

			dataOffset += LayoutDimensions.BitsPerRow / 8;
		}
	}



#if !MONO
	[StructLayout(LayoutKind.Sequential)]
	public class RECT
	{
		public Int32 left;
		public Int32 top;
		public Int32 right;
		public Int32 bottom;

		public RECT()
		{
		}

		public RECT(Int32 l, Int32 t, Int32 r, Int32 b)
		{
			left = l;
			top = t;
			right = r;
			bottom = b;
		}
	}

	[DllImport("user32.dll")]
	private static extern int ScrollWindowEx(	System.IntPtr hWnd, 
												int dx, 
												int dy, 
												[MarshalAs(UnmanagedType.LPStruct)] RECT prcScroll,
												[MarshalAs(UnmanagedType.LPStruct)]	RECT prcClip,
												System.IntPtr hrgnUpdate,
												[MarshalAs(UnmanagedType.LPStruct)] RECT prcUpdate,
												System.UInt32 flags); 
#endif
}
	