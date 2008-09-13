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
			if(Length <= 0 || Length > 8)
				throw new InvalidCastException();

			long a = 0;
			for(int i = 0; i < Length; ++i)
				a |= (long)View.Document.Buffer[Start + i] << (i*8);

			return a;
		}

		public string AsAscii()
		{
			if(Length > 0)
			{
				string s = "";
				
				// Limit to 256
				long end = End;
				if(end - Start > 0x100)
					end = Start + 0x100;

				for(long addr = Start; addr < end; ++addr)
					s += AsciiChar[View.Document.Buffer[addr]];

				return s;
			}
			else
				throw new InvalidCastException();
		}

		public string AsUnicode()
		{
			if(Length > 0)
			{
				long len = Length;
				if(len > 0x100)
					len = 0x100;
				
				byte[] data = new byte[len];
				for(int i = 0; i < len; ++i)
					data[i] = View.Document.Buffer[Start + i];

				return System.Text.Encoding.Unicode.GetString(data);
			}
			else
				throw new InvalidCastException();
		}

		public string AsUTF8()
		{
			if(Length > 0)
			{
				long len = Length;
				if(len > 0x100)
					len = 0x100;
				
				byte[] data = new byte[len];
				for(int i = 0; i <len; ++i)
					data[i] = View.Document.Buffer[Start + i];

				return System.Text.Encoding.UTF8.GetString(data);
			}
			else
				throw new InvalidCastException();
		}

		public double AsFloat()
		{
			if(Length == 4)
			{
				byte[] data = new Byte[4];
				for(int i = 0; i < 4; ++i)
					data[i] = View.Document.Buffer[Start + i];

				return BitConverter.ToSingle(data, 0);
 			}
			else if(Length == 8)
			{
				byte[] data = new Byte[8];
				for(int i = 0; i < 8; ++i)
					data[i] = View.Document.Buffer[Start + i];

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

		public SizeF			DataByteSize		= new SizeF(0, 0);
		public float			DataByteSpacing		= 0;

		public float			LeftGutterWidth		= 10;
		public float			CentralGutterWidth	= 10;
		public float			RightGutterWidth	= 10;

		public int				BytesPerRow			= 0;
		public int				NumAddressDigits	= 0;
		public int				NumDataByteDigits	= 0;
		public int				VisibleLines		= 0;
	}



	private class Caret
	{
		public Control		ParentControl	= null;
		public Point		_Position		= new Point(-1, -1);
		public int			_Height			= 16;

		private Timer		FlashTimer		= null;
		private Pen			ForegroundPen	= new Pen(Color.Black, 1);
		private Pen			BackgroundPen	= new Pen(Color.White, 1);
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
				UnDrawCaret();
				_Position = value;
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
				ParentControl.Invalidate(new Rectangle(Position.X, Position.Y - Height, 1, Height + 1));
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

	private VScrollBar	VScroll				= new VScrollBar();
	private	double		ScrollScaleFactor	= 1;
	private long		ScrollPosition		= 0;
	private long		ScrollHeight		= 0;

	private Point		DragStartPos		= new Point(0, 0);
	private HexViewHit	DragStartHit		= new HexViewHit(HexViewHit.HitType.Unknown);
	private bool		Dragging			= false;

	public SelectionRange		Selection;
	private Caret		InsertCaret			= null;

	public int AddressRadix
	{
		get
		{
			return _AddressRadix;
		}

		set
		{
			_AddressRadix = value;
			RecalcDimensions();
			Invalidate();
			EnsureVisible(Selection.Start);
		}
	}

	public int DataRadix
	{
		get
		{
			return _DataRadix;
		}

		set
		{
			BuildDataRadixStringTable(value);
			_DataRadix = value;
			RecalcDimensions();
			Invalidate();
			EnsureVisible(Selection.Start);
		}
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

		InsertCaret = new Caret(this, 16);
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
		long firstLine = (long)(pixelOffset / LayoutDimensions.DataByteSize.Height);
		long dataOffset = firstLine * LayoutDimensions.BytesPerRow;

		if(address < dataOffset || address >= dataOffset + LayoutDimensions.BytesPerRow * (ClientSize.Height / LayoutDimensions.DataByteSize.Height))
			ScrollToAddress(address);
	}

	public void ScrollToAddress(long address)
	{
		if(address < Document.Buffer.Length && LayoutDimensions.BytesPerRow > 0)
			ScrollToPixel((long)((double)(address / LayoutDimensions.BytesPerRow) * (double)LayoutDimensions.DataByteSize.Height));
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
		long line = (long)((double)(p.Y + ScrollPosition) / LayoutDimensions.DataByteSize.Height);
		long lineAddress = line * LayoutDimensions.BytesPerRow;

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
			double column = p.X - LayoutDimensions.DataRect.Left;
			column /= LayoutDimensions.DataByteSize.Width + LayoutDimensions.DataByteSpacing;

			int character = -1;
			string s = DataRadixString[Document.Buffer[lineAddress + (int)column]];
			Graphics g = CreateGraphics();
			for(int i = 0; i < s.Length; ++i)
			{
				double x = p.X - ((int)column * LayoutDimensions.DataByteSize.Width + LayoutDimensions.DataByteSpacing);
				x -= LayoutDimensions.DataRect.Left;
				RectangleF r = MeasureSubString(g, s, i, 1, _Font);
				if(x >= r.Left && x <= r.Right)
				{
					character = i;
					break;
				}
			}
			g.Dispose();
			
			if(lineAddress + (long)column >= Selection.Start && lineAddress + (long)column < Selection.End)
				return new HexViewHit(HexViewHit.HitType.DataSelection, lineAddress + (int)column, character, new Point(0, 0));
			else
				return new HexViewHit(HexViewHit.HitType.Data, lineAddress + (int)column, character, new Point(0, 0));
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

		LayoutDimensions.NumDataByteDigits = (int)(Math.Log(0xFF) / Math.Log(_DataRadix)) + 1;
		LayoutDimensions.DataByteSize = g.MeasureString(digits.Substring(0, LayoutDimensions.NumDataByteDigits), _Font);

		// Calculate number of visible lines
		LayoutDimensions.VisibleLines = (int)Math.Ceiling(ClientSize.Height / LayoutDimensions.DataByteSize.Height);

		// Calculate width of largest address
		LayoutDimensions.NumAddressDigits = (int)(Math.Log(Document.Buffer.Length) / Math.Log(_AddressRadix)) + 1;
		SizeF AddressSize = g.MeasureString(digits.Substring(0, LayoutDimensions.NumAddressDigits), _Font);
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

		LayoutDimensions.BytesPerRow = 0;
		float width = 0;
		do
		{
			++LayoutDimensions.BytesPerRow;
			width = (LayoutDimensions.DataByteSize.Width + LayoutDimensions.DataByteSpacing) * LayoutDimensions.BytesPerRow;
			width += LayoutDimensions.RightGutterWidth;
			width += g.MeasureString(digits.Substring(0, LayoutDimensions.BytesPerRow), _Font).Width;

		} while(width <= RemainingWidth);
		--LayoutDimensions.BytesPerRow;

		LayoutDimensions.DataRect = new RectangleF(	LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth,
													0,
													LayoutDimensions.BytesPerRow * LayoutDimensions.DataByteSize.Width,
													ClientSize.Height);

		SizeF AsciiSize = g.MeasureString(digits.Substring(0, LayoutDimensions.BytesPerRow), _Font);
		LayoutDimensions.AsciiRect = new RectangleF(	LayoutDimensions.DataRect.Right + LayoutDimensions.RightGutterWidth,
														0,
														AsciiSize.Width,
														ClientSize.Height);

		const int IntMax = 0x6FFFFFFF;
		ScrollHeight = (long)(((Document.Buffer.Length / LayoutDimensions.BytesPerRow) + 1) * LayoutDimensions.DataByteSize.Height);

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

		InsertCaret.Height = (int)LayoutDimensions.DataByteSize.Height;

		g.Dispose();
	}

	protected Point AddressToClientPoint(long address)
	{
		double y = address / LayoutDimensions.BytesPerRow;
		y *= LayoutDimensions.DataByteSize.Height;
		y -= ScrollPosition;

		address -= (address / LayoutDimensions.BytesPerRow) * LayoutDimensions.BytesPerRow;
		int x = (int)(address * (LayoutDimensions.DataByteSize.Width + LayoutDimensions.DataByteSpacing));
		x += (int)(LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth);

		return new Point(x, (int)(y + LayoutDimensions.DataByteSize.Height));
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
		if(Selection.Length > 0)
			InsertCaret.Hide();
		else
			InsertCaret.Show();
		Invalidate();
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

		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		if(((int)e.Button & (int)MouseButtons.Left) != 0)
		{
			DragStartHit = hit;
			DragStartPos = new Point(e.X, e.Y);
			if(hit.Type == HexViewHit.HitType.Data)
			{
				InsertCaret.Position = AddressToClientPoint(hit.Address);

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
			Selection.Start = hit.Address;
		}
	}

	protected void OnDrag(MouseEventArgs e)
	{
		HexViewHit hit = HitTest(new Point(e.X, e.Y));
		if(hit.Type == HexViewHit.HitType.Data || hit.Type == HexViewHit.HitType.DataSelection)
		{
			if(hit.Address < DragStartHit.Address)
				Selection.Set(hit.Address, DragStartHit.Address);
			else
				Selection.Set(DragStartHit.Address, hit.Address);
		}
	}

	protected void OnEndDrag(MouseEventArgs e)
	{
		HexViewHit hit = HitTest(new Point(e.X, e.Y));

		if(hit.Type == HexViewHit.HitType.Data)
		{
			Selection.End = hit.Address;
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

	protected GraphicsPath CreateRoundedSelectionPath(long startAddress, long endAddress, float yOffset)
	{
		--endAddress;

		yOffset = 0 - yOffset;

		long startRow = startAddress / LayoutDimensions.BytesPerRow;
		long startCol = startAddress - (startRow * LayoutDimensions.BytesPerRow);
		long endRow = endAddress / LayoutDimensions.BytesPerRow;
		long endCol = endAddress - (endRow * LayoutDimensions.BytesPerRow);
		long firstVisibleRow = (long)(ScrollPosition / LayoutDimensions.DataByteSize.Height);
		long lastVisibleRow = (long)((ScrollPosition + ClientSize.Height) / LayoutDimensions.DataByteSize.Height);

Console.WriteLine("start: " + startRow + ", " + startCol);
Console.WriteLine("end: " + endRow + ", " + endCol);
Console.WriteLine("bpr: " + LayoutDimensions.BytesPerRow);
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

			r.Location = AddressToClientPoint((startRow + 1) * LayoutDimensions.BytesPerRow);
			r.Offset(0, -LayoutDimensions.DataByteSize.Height + yOffset);
			path.AddArc(r, 180, 90);
			r.Offset(startCol * LayoutDimensions.DataByteSize.Width - 10, -10 + yOffset);
			path.AddArc(r, 90, -90);
			r.Location = AddressToClientPoint(startAddress);
			r.Offset(0, -LayoutDimensions.DataByteSize.Height + yOffset);
			path.AddArc(r, 180, 90);
			r.Location = AddressToClientPoint(startRow * LayoutDimensions.BytesPerRow + LayoutDimensions.BytesPerRow - 1);
			r.Offset(LayoutDimensions.DataByteSize.Width - 10, -LayoutDimensions.DataByteSize.Height + yOffset);
			path.AddArc(r, 270, 90);
		}
		else
		{
			// curved top
			//
			// +-----------------------+
			// |                       |

			r.Location = AddressToClientPoint(startAddress);
			r.Offset(0, -LayoutDimensions.DataByteSize.Height + yOffset);
Console.WriteLine(AddressToClientPoint(startAddress));
			path.AddArc(r, 180, 90);
			if(endRow > startRow)
				r.Location = AddressToClientPoint((startRow * LayoutDimensions.BytesPerRow) + LayoutDimensions.BytesPerRow - 1);
			else
				r.Location = AddressToClientPoint(endAddress);
			r.Offset(LayoutDimensions.DataByteSize.Width - 10, -LayoutDimensions.DataByteSize.Height + yOffset);
Console.WriteLine(AddressToClientPoint((startRow * LayoutDimensions.BytesPerRow) + LayoutDimensions.BytesPerRow - 1));
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
		else if(endCol < LayoutDimensions.BytesPerRow - 1 && endRow > startRow)
		{
			// curved bottom
			//
			// |                       |
			// |                    +--+
			// |                    |
			// +--------------------+
Console.WriteLine("partial curved bottom");

			r.Location = AddressToClientPoint((endRow - 1) * LayoutDimensions.BytesPerRow + LayoutDimensions.BytesPerRow - 1);
			r.Offset(LayoutDimensions.DataByteSize.Width-10,  - 10 + yOffset);
			path.AddArc(r, 0, 90);
			r.Location = AddressToClientPoint(endAddress);
			r.Offset(LayoutDimensions.DataByteSize.Width, -LayoutDimensions.DataByteSize.Height + yOffset);
			path.AddArc(r, 270, -90);
			r.Offset(-10, LayoutDimensions.DataByteSize.Height - 10 + yOffset);
			path.AddArc(r, 0, 90);
			r.Location = AddressToClientPoint(endRow * LayoutDimensions.BytesPerRow);
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
			r.Location = AddressToClientPoint(endAddress);
			r.Offset(LayoutDimensions.DataByteSize.Width - 10, -10 + yOffset);
			path.AddArc(r, 0, 90);
			if(endRow > startRow)
				r.Location = AddressToClientPoint(endRow * LayoutDimensions.BytesPerRow);
			else
				r.Location = AddressToClientPoint(startAddress);
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
		long firstLine = (long)(pixelOffset / LayoutDimensions.DataByteSize.Height);

		double drawingOffset = e.ClipRectangle.Top - (pixelOffset % LayoutDimensions.DataByteSize.Height);
		long dataOffset = firstLine * LayoutDimensions.BytesPerRow;
		int visibleLines = (int)Math.Ceiling((e.ClipRectangle.Bottom - drawingOffset) / LayoutDimensions.DataByteSize.Height) + 1;

		e.Graphics.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.ButtonFace)), 0, 0, LayoutDimensions.AddressRect.Width + LayoutDimensions.LeftGutterWidth / 2, LayoutDimensions.AddressRect.Height);

if(Selection.Length > 0)
{
	GraphicsPath p = CreateRoundedSelectionPath(Selection.Start, Selection.End, (float)0);
e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 200, 255, 200)), p);
e.Graphics.DrawPath(new Pen(Color.LightGreen, 1), p);
//	e.Graphics.FillPath(new SolidBrush(Color.FromKnownColor(KnownColor.Highlight)), p);
// 	e.Graphics.DrawPath(new Pen(Color.FromKnownColor(KnownColor.Highlight)), p);
}

		string str;
		for(int line = 0; line < visibleLines; ++line)
		{
			str = IntToRadixString(dataOffset, _AddressRadix, LayoutDimensions.NumAddressDigits);
			RectangleF rect = new RectangleF(LayoutDimensions.AddressRect.Left, (float)drawingOffset + line * LayoutDimensions.DataByteSize.Height, LayoutDimensions.AddressRect.Width, LayoutDimensions.DataByteSize.Height);
			e.Graphics.DrawString(str, _Font, _Brush, rect);
//			e.Graphics.DrawRectangle(new Pen(Color.Green), rect.Left, rect.Top, rect.Width, rect.Height);



			str = "";
			rect = new RectangleF(	LayoutDimensions.DataRect.Left, 
									(float)drawingOffset + line * LayoutDimensions.DataByteSize.Height,
									LayoutDimensions.DataByteSize.Width,
									LayoutDimensions.DataByteSize.Height);

//if(line == 2)
//{
//e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 200, 255, 200)), CreateRoundedRectPath(new RectangleF(rect.Left, rect.Top, LayoutDimensions.DataByteSize.Width * 10, LayoutDimensions.DataByteSize.Height*2)));
//e.Graphics.DrawPath(new Pen(Color.LightGreen, 1), CreateRoundedRectPath(new RectangleF(rect.Left, rect.Top, LayoutDimensions.DataByteSize.Width * 10, LayoutDimensions.DataByteSize.Height*2)));
//e.Graphics.DrawPath(new Pen(Color.FromArgb(255, 200, 200, 255), 2), CreateRoundedRectPath(new RectangleF(rect.Left + LayoutDimensions.DataByteSize.Width * 2, rect.Top, LayoutDimensions.DataByteSize.Width * 2, LayoutDimensions.DataByteSize.Height)));
//e.Graphics.DrawPath(new Pen(Color.FromArgb(255, 170, 170, 255), 1), CreateRoundedRectPath(new RectangleF(rect.Left + LayoutDimensions.DataByteSize.Width * 2, rect.Top, LayoutDimensions.DataByteSize.Width * 2, LayoutDimensions.DataByteSize.Height)));
//}


			for(long i = 0; i < LayoutDimensions.BytesPerRow; ++i)
			{
				str = DataRadixString[Document.Buffer[dataOffset + i]];

//				if(dataOffset + i >= Selection.Start && dataOffset + i < Selection.End && Selection.Length > 0)
//				{
////					e.Graphics.FillPath(new SolidBrush(Color.FromKnownColor(KnownColor.Highlight)), CreateRoundedRectPath(rect));
////					e.Graphics.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.Highlight)), rect);
//					e.Graphics.DrawString(str, _Font, new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText)), rect);
//				}
//				else
				{
					e.Graphics.DrawString(str, _Font, _Brush, rect);
				}
				rect.Location = new PointF(rect.Location.X + LayoutDimensions.DataByteSize.Width + LayoutDimensions.DataByteSpacing, rect.Location.Y);
			}

			str = "";
			for(long i = 0; i < LayoutDimensions.BytesPerRow; ++i)
				str += AsciiChar[Document.Buffer[dataOffset + i]];
			rect = new RectangleF(LayoutDimensions.AsciiRect.Left, (float)drawingOffset + line * LayoutDimensions.DataByteSize.Height, LayoutDimensions.AsciiRect.Width, LayoutDimensions.DataByteSize.Height);
			e.Graphics.DrawString(str, _Font, _Brush, rect);
//			e.Graphics.DrawRectangle(new Pen(Color.Red), rect.Left, rect.Top, rect.Width, rect.Height);

			dataOffset += LayoutDimensions.BytesPerRow;
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




