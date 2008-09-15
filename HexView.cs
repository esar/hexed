using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;



public class HexView : Control
{
	public class SelectionRange
	{
		private	HexView	View = null;

		public event EventHandler Changed;

		public long		_Start = 0;
		public long		_End = 0;

		
		
		public SelectionRange(HexView view)
		{
			View = view;
		}

		public long Start
		{
			get
			{
				return _Start;
			}

			set
			{
				if(_Start != value)
				{
					_Start = value;

					if(Changed != null)
						Changed(this, new EventArgs());
				}
			}
		}

		public long End
		{
			get
			{
				return _End;
			}

			set
			{
				if(_End != value)
				{
					_End = value;

					if(Changed != null)
						Changed(this, new EventArgs());
				}
			}
		}

		public long Length
		{
			get
			{
				return _End - _Start;
			}
		}

		public void Set(long start, long end)
		{
			bool hasChanged = false;

			if(_Start != start || _End != end)
				hasChanged = true;

			_Start = start;
			_End = end;

			if(hasChanged)
				if(Changed != null)
					Changed(this, new EventArgs());
		}

		public long AsInteger()
		{
			if(Length <= 0 || Length > 64)
				throw new InvalidCastException();

			long a = 0;
			for(int i = 0; i < Length / 8; ++i)
				a |= (long)View.Document.Buffer[Start/8 + i] << (i*8);

			return a;
		}

		public string AsAscii()
		{
			Console.WriteLine("len: " + Length);
			if(Length > 0)
			{
				string s = "";
				
				// Limit to 256
				long end = End;
				if(end - Start > 0x100 * 8)
					end = Start + (0x100 * 8);

				for(long addr = Start; addr < end; addr += 8)
					s += AsciiChar[View.Document.Buffer[addr / 8]];

				return s;
			}
			else
				throw new InvalidCastException();
		}

		public string AsUnicode()
		{
			if(Length > 0)
			{
				long len = Length / 8;
				if(len > 0x100)
					len = 0x100;
				
				byte[] data = new byte[len];
				for(int i = 0; i < len; ++i)
					data[i] = View.Document.Buffer[Start/8 + i];

				return System.Text.Encoding.Unicode.GetString(data);
			}
			else
				throw new InvalidCastException();
		}

		public string AsUTF8()
		{
			if(Length > 0)
			{
				long len = Length / 8;
				if(len > 0x100)
					len = 0x100;
				
				byte[] data = new byte[len];
				for(int i = 0; i <len; ++i)
					data[i] = View.Document.Buffer[Start/8 + i];

				return System.Text.Encoding.UTF8.GetString(data);
			}
			else
				throw new InvalidCastException();
		}

		public double AsFloat()
		{
			if(Length == 32)
			{
				byte[] data = new Byte[4];
				for(int i = 0; i < 4; ++i)
					data[i] = View.Document.Buffer[Start/8 + i];

				return BitConverter.ToSingle(data, 0);
 			}
			else if(Length == 64)
			{
				byte[] data = new Byte[8];
				for(int i = 0; i < 8; ++i)
					data[i] = View.Document.Buffer[Start/8 + i];

				return BitConverter.ToSingle(data, 0);
			}
			else
				throw new InvalidCastException();
		}
	};

	public class ContextMenuEventArgs : EventArgs
	{
		public Point Position;
		public HexViewHit Hit;

		public ContextMenuEventArgs(Point p, HexViewHit h)
		{
			Position = p;
			Hit = h;
		}
	}

	public delegate void ContextMenuEventHandler(object sender, ContextMenuEventArgs e);
	public event ContextMenuEventHandler ContextMenu;

	
	private class Dimensions
	{
		public RectangleF		AddressRect			= new RectangleF(0, 0, 0, 0);
		public RectangleF		DataRect			= new RectangleF(0, 0, 0, 0);
		public RectangleF		AsciiRect			= new RectangleF(0, 0, 0, 0);

		public RectangleF[]		WordRects;
		
		public SizeF			WordSize			= new SizeF(0, 0);
		public float			WordSpacing			= 5;
		public float			WordGroupSpacing	= 10;

		public float			LeftGutterWidth		= 10;
		public float			CentralGutterWidth	= 10;
		public float			RightGutterWidth	= 10;

		public int				BitsPerRow			= 0;
		public int				BitsPerDigit		= 0;
//		public int				WordsPerRow			= 0;
//		public int				GroupsPerRow		= 0;
		public int				NumAddressDigits	= 0;
		public int				NumWordDigits		= 0;
		public int				VisibleLines		= 0;
	}



	private class Caret
	{
		public Control		ParentControl	= null;
		public Point		_Position		= new Point(-1, -1);
		public int			_Height			= 16;

		private Timer		FlashTimer		= null;
		private Pen			ForegroundPen	= new Pen(Color.Black, 2);
		private Pen			BackgroundPen	= new Pen(Color.White, 2);
		private bool		IsOn			= false;

		public int Height
		{
			get
			{
				return _Height;
			}

			set
			{
				UnDrawCaret();
				_Height = value;
			}
		}

		public Point Position
		{
			get
			{
				return _Position;
			}

			set
			{
				Hide();
				_Position = value;
				DrawCaret();
				Show();
			}
		}

		public Caret(Control parent, int height)
		{
			ParentControl = parent;
			Height = height;

			FlashTimer = new Timer();
			FlashTimer.Interval = 500;
			FlashTimer.Tick += new EventHandler(OnFlashTimerTick);
			FlashTimer.Start();
		}

		public void Dispose()
		{
			FlashTimer.Stop();
			FlashTimer = null;
			ParentControl = null;
		}

		public void Hide()
		{
			FlashTimer.Stop();

			if(IsOn)
				UnDrawCaret();
		}

		public void Show()
		{
			FlashTimer.Start();
		}

		protected void DrawCaret()
		{
			if(Position.X != -1 && Position.Y != -1)
			{
				Graphics g = ParentControl.CreateGraphics();
				g.DrawLine(ForegroundPen, Position.X, Position.Y - Height, Position.X, Position.Y);
				g.Dispose();
				IsOn = true;
			}
		}

		protected void UnDrawCaret()
		{
			if(Position.X != -1 && Position.Y != -1)
			{
//				Graphics g = ParentControl.CreateGraphics();
//				g.DrawLine(BackgroundPen, Position.X, Position.Y - Height, Position.X, Position.Y);
//				g.Dispose();
				ParentControl.Invalidate(new Rectangle(Position.X - 1, Position.Y - Height, 3, Height + 1));
				IsOn = false;
			}
		}

		protected void OnFlashTimerTick(object sender, EventArgs e)
		{
			if(IsOn)
				UnDrawCaret();
			else
				DrawCaret();
		}
	};

	private Font		_Font				= new Font("Courier New", 10);
	private Brush		_Brush				= new SolidBrush(Color.Black);
	private int			_AddressRadix		= 16;
	private int			_DataRadix			= 16;

	private int			_BytesPerWord		= 1;
	private int			_WordsPerGroup		= 8;
	private int			_WordsPerLine		= -1; //16;
	
	private VScrollBar	VScroll				= new VScrollBar();
	private	double		ScrollScaleFactor	= 1;
	private long		ScrollPosition		= 0;
	private long		ScrollHeight		= 0;

	private Point		DragStartPos		= new Point(0, 0);
	private HexViewHit	DragStartHit		= new HexViewHit(HexViewHit.HitType.Unknown);
	private bool		Dragging			= false;

	public SelectionRange		Selection;
	private Win32Caret		InsertCaret			= null;

	
	public int BytesPerWord
	{
		get { return _BytesPerWord; }
		set { _BytesPerWord = value; RecalcDimensions(); Invalidate(); EnsureVisible(Selection.Start); }
	}
	
	public int WordsPerGroup
	{
		get { return _WordsPerGroup; }
		set { _WordsPerGroup = value; RecalcDimensions(); Invalidate(); EnsureVisible(Selection.Start); }
	}
	
	public int WordsPerLine
	{
		get { return _WordsPerLine; }
		set { _WordsPerLine = value; RecalcDimensions(); Invalidate(); EnsureVisible(Selection.Start); }
	}
	
	public int AddressRadix
	{
		get { return _AddressRadix; }
		set { _AddressRadix = value; RecalcDimensions(); Invalidate(); EnsureVisible(Selection.Start); }
	}

	public int DataRadix
	{
		get { return _DataRadix; }
		set { BuildDataRadixStringTable(value); _DataRadix = value; RecalcDimensions(); Invalidate(); EnsureVisible(Selection.Start); }
	}
	
	public override Color ForeColor
	{
		get { return base.ForeColor; }
		set { base.ForeColor = value; _Brush.Dispose(); _Brush = new SolidBrush(value); }
	}

	public Document Document = null;
	private string[]	DataRadixString = new string[0x100];

	private static char[]		AsciiChar = {	'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '!', '\"', '#', '$', '%', '&', '\'',
										'(', ')', '*', '+', ',', '-', '.', '/',
										'0', '1', '2', '3', '4', '5', '6', '7',
										'8', '9', ':', ';', '<', '=', '>', '?',
										'@', 'A', 'B', 'C', 'D', 'E', 'F', 'G',
										'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
										'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W',
										'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
										'`', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
										'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
										'p', 'q', 'r', 's', 't', 'u', 'v', 'w',
										'x', 'y', 'z', '{', '|', '}', '~', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.' };

	private Dimensions	LayoutDimensions = new Dimensions();



	private void BuildDataRadixStringTable(int radix)
	{
		int maxDigits = (int)(Math.Log(0xFF) / Math.Log(radix)) + 1;

		for(int i = 0; i <= 0xFF; ++i)
			DataRadixString[i] = IntToRadixString(i, radix, maxDigits);
	}

	protected override CreateParams CreateParams
	{
		get
		{
			const int WS_BORDER = 0x00800000;
			const int WS_EX_STATICEDGE = 0x00020000;

			CreateParams cp = base.CreateParams;
//			cp.ExStyle |= WS_EX_STATICEDGE;

			return cp;
		}
	}


	public HexView(Document doc)
	{
		this.SetStyle ( ControlStyles.AllPaintingInWmPaint, true);
		this.SetStyle ( ControlStyles.DoubleBuffer, true);
		this.SetStyle ( ControlStyles.UserPaint, true);
		BackColor = Color.White;
		Document = doc;
		BuildDataRadixStringTable(_DataRadix);

		VScroll.Dock = DockStyle.Right;
		VScroll.Minimum = 0;
		VScroll.Value = 0;
		VScroll.Visible = false;
		VScroll.Scroll += new ScrollEventHandler(OnScroll);
		Controls.Add(VScroll);

		InsertCaret = new Win32Caret(this);
		Selection = new SelectionRange(this);
		Selection.Changed += new EventHandler(OnSelectionChanged);
		Document.Buffer.Changed += new PieceBuffer.BufferChangedEventHandler(OnBufferChanged);
	}

	protected override void Dispose (bool disposing)
	{
		if(disposing)
			InsertCaret.Dispose();
		
		base.Dispose(disposing);
	}

	public void EnsureVisible(long address)
	{
		double pixelOffset = ScrollPosition;
		long firstLine = (long)(pixelOffset / LayoutDimensions.WordSize.Height);
		long dataOffset = firstLine * LayoutDimensions.BitsPerRow;

		if(address < dataOffset)
			ScrollToAddress(address);
		else if(address >= dataOffset + (LayoutDimensions.BitsPerRow * (LayoutDimensions.VisibleLines - 2)))
			ScrollToAddress(address - (LayoutDimensions.BitsPerRow * (LayoutDimensions.VisibleLines - 2)));
	}

	public void ScrollToAddress(long address)
	{
		if(address < Document.Buffer.Length && LayoutDimensions.BitsPerRow > 0)
			ScrollToPixel((long)((double)(address / LayoutDimensions.BitsPerRow) * (double)LayoutDimensions.WordSize.Height));
	}

	protected void ScrollToPixel(long NewPosition)
	{
		if(NewPosition < 0)
			NewPosition = 0;
		else if(NewPosition > ScrollHeight)
			NewPosition = ScrollHeight;

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

		ScrollPosition = NewPosition;
		VScroll.Value = (int)((double)ScrollPosition / ScrollScaleFactor);

		InsertCaret.Position = AddressToClientPoint(Selection.Start);
	}

	protected void OnScroll(object sender, ScrollEventArgs args)
	{
		long NewPosition = 0;

		switch(args.Type)
		{
			case ScrollEventType.SmallDecrement:
				NewPosition = ScrollPosition - VScroll.SmallChange;
				break;
			case ScrollEventType.SmallIncrement:
				NewPosition = ScrollPosition + VScroll.SmallChange;
				break;
			case ScrollEventType.LargeDecrement:
				NewPosition = ScrollPosition - VScroll.LargeChange;
				break;
			case ScrollEventType.LargeIncrement:
				NewPosition = ScrollPosition + VScroll.LargeChange;
				break;
			case ScrollEventType.ThumbTrack:
			case ScrollEventType.ThumbPosition:
				NewPosition = (long)((double)args.NewValue * ScrollScaleFactor);
				break;
			default:
				NewPosition = ScrollPosition;
				break;
		}

		ScrollToPixel(NewPosition);
	}

	protected string IntToRadixString(long x, int radix, int minLength)
	{
		string str = "";
		const string digit		= "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string padding	= "000000000000000000000000000000000000";

		if(radix < 2 || radix > 36)
			return null;

		while(x > 0)
		{
			long v = x % radix;
			str = digit[(int)v] + str;
			x = (x - v) / radix;
		}

		if(str.Length < minLength)
			str = padding.Substring(0, minLength - str.Length < 36 ? minLength - str.Length : 36) + str;

		return str;
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


	public class HexViewHit
	{
		public enum HitType
		{
			Unknown,
			Address,
			Data,
			DataSelection,
			Ascii,
			AsciiSelection
		}

		public HitType	Type;
		public long		Address;
		public int		Character;
		public Point	CharacterOrigin;

		public HexViewHit(HitType type)
		{
			Type = type;
		}

		public HexViewHit(HitType type, long address)
		{
			Type = type;
			Address = address;
		}

		public HexViewHit(HitType type, long address, int character, Point origin)
		{
			Type = type;
			Address = address;
			Character = character;
			CharacterOrigin = origin;
		}
	}

	public HexViewHit HitTest(Point p)
	{
		long line = (long)((double)(p.Y + ScrollPosition) / LayoutDimensions.WordSize.Height);
		long lineAddress = line * LayoutDimensions.BitsPerRow;

		if(	p.X >= LayoutDimensions.AddressRect.Left &&
			p.X <= LayoutDimensions.AddressRect.Right &&
			p.Y >= LayoutDimensions.AddressRect.Top &&
			p.Y <= LayoutDimensions.AddressRect.Bottom)
		{
			return new HexViewHit(HexViewHit.HitType.Address, lineAddress);
		}
		else if(	p.X >= LayoutDimensions.DataRect.Left &&
					p.X <= LayoutDimensions.DataRect.Right &&
					p.Y >= LayoutDimensions.DataRect.Top &&
					p.Y <= LayoutDimensions.DataRect.Bottom)
		{
			float x = p.X;// - LayoutDimensions.DataRect.Left;
			int word = 0;
			while(word < LayoutDimensions.WordRects.Length && x > LayoutDimensions.WordRects[word].Right)
				++word;
			if(x < LayoutDimensions.WordRects[word].Left)
				--word;
			x -= LayoutDimensions.WordRects[word].Left;
			int digitAddress = word * _BytesPerWord * 8;
			
			Graphics g = CreateGraphics();
			int i;
			for(i = 1; i < LayoutDimensions.NumWordDigits; ++i)
			{
				RectangleF r = MeasureSubString(g, "00000000000000000000000000000000000000000000000000000000000000000", 0, i, _Font);
				if(x <= r.Width)
					break;
			}
			g.Dispose();
			digitAddress += (i - 1) * ((_BytesPerWord * 8) / LayoutDimensions.NumWordDigits);
			
			if(lineAddress + (long)digitAddress >= Selection.Start && lineAddress + (long)digitAddress < Selection.End)
				return new HexViewHit(HexViewHit.HitType.DataSelection, lineAddress + (int)digitAddress, i, new Point(0, 0));
			else
				return new HexViewHit(HexViewHit.HitType.Data, lineAddress + (int)digitAddress, i, new Point(0, 0));
		}
		else if(	p.X >= LayoutDimensions.AsciiRect.Left &&
					p.X <= LayoutDimensions.AsciiRect.Right &&
					p.Y >= LayoutDimensions.AsciiRect.Top &&
					p.Y <= LayoutDimensions.AsciiRect.Bottom)
		{
			

			return new HexViewHit(HexViewHit.HitType.Ascii);
		}

		return new HexViewHit(HexViewHit.HitType.Unknown);
	}


	protected void RecalcDimensions()
	{
		Graphics g = CreateGraphics();
		const string digits = "00000000000000000000000000000000000000000000000000000000000000000";

		ulong maxWordValue = ((ulong)1 << (_BytesPerWord * 8)) - 1;
		LayoutDimensions.NumWordDigits = (int)(Math.Log(maxWordValue) / Math.Log(_DataRadix)) + 1;
		LayoutDimensions.BitsPerDigit = (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits;
		LayoutDimensions.WordSize = MeasureSubString(g, digits, 0, LayoutDimensions.NumWordDigits, _Font).Size; //g.MeasureString(digits.Substring(0, LayoutDimensions.NumWordDigits), _Font);

		// Calculate number of visible lines
		LayoutDimensions.VisibleLines = (int)Math.Ceiling(ClientSize.Height / LayoutDimensions.WordSize.Height);

		// Calculate width of largest address
		LayoutDimensions.NumAddressDigits = (int)(Math.Log(Document.Buffer.Length) / Math.Log(_AddressRadix)) + 1;
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
		if(_WordsPerLine < 0)
		{
			float width = 0;
			
			do
			{
				++GroupsPerRow;
				WordsPerRow = GroupsPerRow * _WordsPerGroup;
				LayoutDimensions.BitsPerRow = WordsPerRow * _BytesPerWord * 8;
				width = (((LayoutDimensions.WordSize.Width + LayoutDimensions.WordSpacing) * _WordsPerGroup) + LayoutDimensions.WordGroupSpacing) * GroupsPerRow;
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
			x -= LayoutDimensions.WordSpacing;
			x += LayoutDimensions.WordGroupSpacing;
		}
		x -= LayoutDimensions.WordGroupSpacing;
			
		
		
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
		ScrollHeight = (long)((((Document.Buffer.Length * 8) / LayoutDimensions.BitsPerRow) + 1) * LayoutDimensions.WordSize.Height);

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

		InsertCaret.Size = new Size((int)(LayoutDimensions.WordSize.Width / LayoutDimensions.NumWordDigits) + 1, 
		                            (int)LayoutDimensions.WordSize.Height); //new Size(1, (int)LayoutDimensions.WordSize.Height);

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
		Console.WriteLine("address: " + address);
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

	protected override void OnResize(EventArgs e)
	{
		RecalcDimensions();
		base.OnResize(e);
		Invalidate();
		ScrollToAddress(Selection.Start);
		InsertCaret.Position = AddressToClientPoint(Selection.Start);
	}

	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		Invalidate();
		if(Selection.Length != 0)
		{
			InsertCaret.Visible = false;
		}
		else
		{
			InsertCaret.Visible = true;
			InsertCaret.Position = AddressToClientPoint(Selection.End);
		}
	}
	
	protected void OnBufferChanged(object sender, PieceBuffer.BufferChangedEventArgs e)
	{
		// TODO: Only invalidate changed region (and only if it's on screen)
		Invalidate();
	}

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
			if(hit.Type == HexViewHit.HitType.Data)
			{
				if((ModifierKeys & Keys.Shift) == Keys.Shift)
					Selection.End = hit.Address;
				else
					Selection.Set(hit.Address, hit.Address);
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
			//Selection.Start = hit.Address;
			Capture = true;
			Selection.Set(hit.Address, hit.Address);
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
			if(hit.Type == HexViewHit.HitType.Data || hit.Type == HexViewHit.HitType.DataSelection)
				Selection.Set(DragStartHit.Address, hit.Address + (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits);
		}
	}

	protected void OnEndDrag(MouseEventArgs e)
	{
		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		if(hit.Type == HexViewHit.HitType.Data)
		{
			Selection.End = hit.Address;
		}
		
		Capture = false;
	}

	protected override bool IsInputKey(Keys keys)
	{
		return true;
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
			default:
				break;
		}
	}
	
	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		int x = -1;
		
		if(e.KeyChar >= '0' && e.KeyChar <= '9')
			x = e.KeyChar - '0';
		else if(e.KeyChar >= 'a' && e.KeyChar <= 'z')
			x = e.KeyChar - 'a' + 10;
		else if(e.KeyChar >= 'A' && e.KeyChar <= 'Z')
			x = e.KeyChar - 'A' + 10;
		
		if(x >= 0 && x < _DataRadix)
		{
			Console.WriteLine("Digit: " + x);
			Document.Buffer.Insert((byte)x);
		}
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

		Console.WriteLine("SA: " + startAddress + ", EA: " + endAddress);
Console.WriteLine("start: " + startRow + ", " + startCol);
Console.WriteLine("end: " + endRow + ", " + endCol);
Console.WriteLine("bpr: " + LayoutDimensions.BitsPerRow);
Console.WriteLine("");

		RectangleF r = new RectangleF(0, 0, 10, 10);
		GraphicsPath path = new GraphicsPath();

		if(startRow < firstVisibleRow)
		{
			// square top
			//
			// +-----------------------+
			// |                       |

			// -10 to hide any outline drawn around the path
			path.AddLine(LayoutDimensions.DataRect.Left, -10 + yOffset, LayoutDimensions.DataRect.Right, -10 + yOffset);
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
Console.WriteLine("curved top: " + addrToPoint(startAddress));
			path.AddArc(r, 180, 90);
			if(endRow > startRow)
				r.Location = addrToPoint((startRow * LayoutDimensions.BitsPerRow) + LayoutDimensions.BitsPerRow - 1);
			else
			{
				r.Location = addrToPoint(endAddress);
Console.WriteLine("curved top: " + addrToPoint(endAddress));
			}
			r.Offset(0, -LayoutDimensions.WordSize.Height + yOffset);
			path.AddArc(r, 270, 90);
		}

		if(endRow > lastVisibleRow)
		{
			// square bottom
			//
			// |                       |
			// +-----------------------+
Console.WriteLine("square bottom");

			// +10 to hide any outline drawn around the path
			path.AddLine(LayoutDimensions.DataRect.Right, ClientSize.Height + 10 + yOffset, LayoutDimensions.DataRect.Left, ClientSize.Height + 10 + yOffset);
		}
		else if(endCol < LayoutDimensions.BitsPerRow && endRow > startRow)
		{
			// curved bottom
			//
			// |                       |
			// |                    +--+
			// |                    |
			// +--------------------+
Console.WriteLine("partial curved bottom, endcol: " + endCol + ", bpr: " + LayoutDimensions.BitsPerRow);

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
Console.WriteLine("full curved bottom");
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

		e.Graphics.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.ButtonFace)), 0, 0, LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, LayoutDimensions.AddressRect.Height);
		e.Graphics.DrawLine(new Pen(Color.FromKnownColor(KnownColor.ButtonShadow)), LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, 0, LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, LayoutDimensions.AddressRect.Height);

		if(Selection.Length != 0)
		{
			GraphicsPath p = CreateRoundedSelectionPath(Selection.Start, Selection.End, (float)0, AddressToClientPoint);
			e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 200, 255, 200)), p);
			e.Graphics.DrawPath(new Pen(Color.LightGreen, 1), p);

			p = CreateRoundedSelectionPath(Selection.Start, Selection.End, (float)0, AddressToClientPointAscii);
			e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 200, 255, 200)), p);
			e.Graphics.DrawPath(new Pen(Color.LightGreen, 1), p);
			
		//	e.Graphics.FillPath(new SolidBrush(Color.FromKnownColor(KnownColor.Highlight)), p);
		// 	e.Graphics.DrawPath(new Pen(Color.FromKnownColor(KnownColor.Highlight)), p);
		}

		string str;
		for(int line = 0; line < visibleLines; ++line)
		{
			str = IntToRadixString(dataOffset, _AddressRadix, LayoutDimensions.NumAddressDigits);
			RectangleF rect = new RectangleF(LayoutDimensions.AddressRect.Left, (float)drawingOffset + line * LayoutDimensions.WordSize.Height, LayoutDimensions.AddressRect.Width, LayoutDimensions.WordSize.Height);
			e.Graphics.DrawString(str, _Font, _Brush, rect.Location); //rect);


			str = "";
			rect = new RectangleF(	LayoutDimensions.DataRect.Left, 
									(float)drawingOffset + line * LayoutDimensions.WordSize.Height,
									LayoutDimensions.WordSize.Width,
									LayoutDimensions.WordSize.Height);


			int off = 0;
			foreach(RectangleF wordRect in LayoutDimensions.WordRects)
			{
				long x = 0;
				for(int j = 0; j < _BytesPerWord; ++j)
					x |= (long)Document.Buffer[dataOffset + off + j] << (j*8);

				str = IntToRadixString(x, _DataRadix, LayoutDimensions.NumWordDigits);

				e.Graphics.DrawString(str, _Font, _Brush, wordRect.Left, rect.Top);
				off += _BytesPerWord;
			}
				
/*			int off = 0;
			for(int group = 0; group < LayoutDimensions.GroupsPerRow; ++group)
			{
				for(int i = 0; i < _WordsPerGroup; ++i)
				{
					long x = 0;
					for(int j = 0; j < _BytesPerWord; ++j)
						x |= (long)Document.Buffer[dataOffset + off + (j * _BytesPerWord)] << (j*8);

					//str = DataRadixString[Document.Buffer[dataOffset + i]];
					str = IntToRadixString(x, _DataRadix, LayoutDimensions.NumWordDigits);

					e.Graphics.DrawString(str, _Font, _Brush, rect.Location);
					rect.Location = new PointF(rect.Location.X + LayoutDimensions.WordSize.Width + LayoutDimensions.WordSpacing, rect.Location.Y);
					off += _BytesPerWord;
				}
				
				rect.Location = new PointF(rect.Location.X + LayoutDimensions.WordGroupSpacing, rect.Location.Y);
			}*/

			str = "";
			for(long i = 0; i < LayoutDimensions.BitsPerRow / 8; ++i)
				str += AsciiChar[Document.Buffer[dataOffset + i]];
			rect = new RectangleF(LayoutDimensions.AsciiRect.Left, (float)drawingOffset + line * LayoutDimensions.WordSize.Height, LayoutDimensions.AsciiRect.Width, LayoutDimensions.WordSize.Height);
			e.Graphics.DrawString(str, _Font, _Brush, rect.Location);
//			e.Graphics.DrawRectangle(new Pen(Color.Red), rect.Left, rect.Top, rect.Width, rect.Height);

			dataOffset += LayoutDimensions.BitsPerRow / 8;
		}
	}



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
}




