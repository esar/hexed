using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;



public enum Endian
{
	None = 0,
	Big,
	Little,
}

public enum EditMode
{
	OverWrite,
	Insert
}

public partial class HexView : Control
{
	
	public class SelectionRange
	{
		private	HexView	View = null;

		public event EventHandler Changed;

		private long		_Start = 0;
		private long		_End = 0;
		private PieceBuffer.Range	_Range;
		
		public int		BorderWidth = 1;
		public Color	BorderColor = Color.Blue;
		public Color	BackColor = Color.AliceBlue;

		
		public SelectionRange(HexView view)
		{
			View = view;
			_Range = new PieceBuffer.Range(view.Document.Marks.Add(_Start / 8),
			                               view.Document.Marks.Add(_End / 8));
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
					if(_Start < 0)
						_Start = 0;
					else if(_Start > View.Document.Length * 8)
						_Start = View.Document.Length * 8;
					_Range.Start.Position = _Start / 8;

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
					if(_End < 0)
						_End = 0;
					else if(_End > View.Document.Length * 8)
						_End = View.Document.Length * 8;
					_Range.End.Position = _End / 8;

					if(Changed != null)
						Changed(this, new EventArgs());
				}
			}
		}

		public PieceBuffer.Range BufferRange
		{
			get { return _Range; }
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
			if(_Start < 0)
				_Start = 0;
			else if(_Start > View.Document.Length * 8)
				_Start = View.Document.Length * 8;
			_End = end;
			if(_End < 0)
				_End = 0;
			else if(_End > View.Document.Length * 8)
				_End = View.Document.Length * 8;
			_Range.Start.Position = _Start / 8;
			_Range.End.Position = _End / 8;

			if(hasChanged)
				if(Changed != null)
					Changed(this, new EventArgs());
		}

		public long AsInteger()
		{
			if(Length <= 0 || Length > 64)
				throw new InvalidCastException();

			return (long)View.Document.GetInteger(Start, (int)Length, View.Endian);
		}

		public string AsAscii()
		{
			if(Length > 0)
			{
				string s = "";
				
				// Limit to 256
				long end = End;
				if(end - Start > 0x100 * 8)
					end = Start + (0x100 * 8);

				for(long addr = Start; addr < end; addr += 8)
					s += AsciiChar[View.Document[addr / 8]];

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
					data[i] = View.Document[Start/8 + i];

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
					data[i] = View.Document[Start/8 + i];

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
					data[i] = View.Document[Start/8 + i];

				return BitConverter.ToSingle(data, 0);
 			}
			else if(Length == 64)
			{
				byte[] data = new Byte[8];
				for(int i = 0; i < 8; ++i)
					data[i] = View.Document[Start/8 + i];

				return BitConverter.ToSingle(data, 0);
			}
			else
				throw new InvalidCastException();
		}
		
		public override string ToString()
		{
			if(Length > 0)
			{
				return String.Format("{0} -> {1}", 
				                     View.IntToRadixString((ulong)_Range.Start.Position, View.AddressRadix, 1), 
				                     View.IntToRadixString((ulong)_Range.End.Position, View.AddressRadix, 1));
			}
			else
				return View.IntToRadixString((ulong)_Range.Start.Position, View.AddressRadix, 1);
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
	public new event ContextMenuEventHandler ContextMenu;
	public event EventHandler EditModeChanged;

	
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




	private Font		_Font				= new Font(FontFamily.GenericMonospace, 10);
	private Brush		_Brush				= SystemBrushes.WindowText;
	private Brush		_GrayBrush			= SystemBrushes.GrayText;
	private Color		_EvenColumnColor	= Color.Transparent;
	private Color		_OddColumnColor		= Color.Transparent;
	private uint		_AddressRadix		= 16;
	private uint		_DataRadix			= 16;

	private int			_BytesPerWord		= 1;
	private int			_WordsPerGroup		= 8;
	private int			_WordsPerLine		= -1; //16;
	private Endian		_Endian				= Endian.Little;
	
	private EditMode	_EditMode			= EditMode.OverWrite;
	
	private VScrollBar	VScroll				= new VScrollBar();
	private	double		ScrollScaleFactor	= 1;
	private long		ScrollPosition		= 0;
	private long		ScrollHeight		= 0;

	private Point		DragStartPos		= new Point(0, 0);
	private HexViewHit	DragStartHit		= new HexViewHit(HexViewHit.HitType.Unknown);
	private bool		Dragging			= false;
	private Timer		DragScrollTimer		= new Timer();

	public SelectionRange		Selection;
	private List<SelectionRange>	Highlights = new List<SelectionRange>();
	
	private PieceBuffer.ClipboardRange	ClipboardRange = null;
	
	private ManagedCaret		InsertCaret			= null;

	
	public EditMode EditMode
	{
		get { return _EditMode; }
		set 
		{ 
			_EditMode = value;
			if(_EditMode == EditMode.Insert)
				InsertCaret.Size = new Size(2, (int)LayoutDimensions.WordSize.Height);
			else
				InsertCaret.Size = new Size((int)(LayoutDimensions.WordSize.Width / LayoutDimensions.NumWordDigits) + 1, 
		                            		(int)LayoutDimensions.WordSize.Height);
			if(EditModeChanged != null) 
				EditModeChanged(this, new EventArgs());
		}
	}
	
	public Endian Endian
	{
		get { return _Endian; }
		set { _Endian = value; Invalidate(); }
	}
	
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
	
	public Color EvenColumnColor
	{
		get { return _EvenColumnColor; }
		set { _EvenColumnColor = value; Invalidate(); }
	}
	
	public Color OddColumnColor
	{
		get { return _OddColumnColor; }
		set { _OddColumnColor = value; Invalidate(); }
	}
	
	public event EventHandler AddressRadixChanged;
	public uint AddressRadix
	{
		get { return _AddressRadix; }
		set 
		{ 
			_AddressRadix = value; 
			RecalcDimensions(); 
			Invalidate(); 
			EnsureVisible(Selection.Start);
			if(AddressRadixChanged != null)
				AddressRadixChanged(this, EventArgs.Empty);
		}
	}

	public event EventHandler DataRadixChanged;
	public uint DataRadix
	{
		get { return _DataRadix; }
		set 
		{ 
			BuildDataRadixStringTable(value); 
			_DataRadix = value; 
			RecalcDimensions(); 
			Invalidate(); 
			EnsureVisible(Selection.Start);
			if(DataRadixChanged != null)
				DataRadixChanged(this, EventArgs.Empty);
		}
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

	private long CurrentDocumentLength = 0;
	private Dimensions	LayoutDimensions = new Dimensions();



	private void BuildDataRadixStringTable(uint radix)
	{
		int maxDigits = (int)(Math.Log(0xFF) / Math.Log(radix)) + 1;

		for(uint i = 0; i <= 0xFF; ++i)
			DataRadixString[i] = IntToRadixString(i, radix, maxDigits);
	}

	protected override CreateParams CreateParams
	{
		get
		{
//			const int WS_BORDER = 0x00800000;
//			const int WS_EX_STATICEDGE = 0x00020000;

			CreateParams cp = base.CreateParams;
//			cp.ExStyle |= WS_EX_STATICEDGE;

			return cp;
		}
	}


	public HexView(Document doc)
	{
		this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		this.SetStyle(ControlStyles.DoubleBuffer, true);
		this.SetStyle(ControlStyles.UserPaint, true);
		this.SetStyle(ControlStyles.Selectable, true);

		BackColor = Color.White;
		Document = doc;
		BuildDataRadixStringTable(_DataRadix);

		VScroll.Dock = DockStyle.Right;
		VScroll.Minimum = 0;
		VScroll.Value = 0;
		VScroll.Visible = false;
		VScroll.Scroll += new ScrollEventHandler(OnScroll);
		Controls.Add(VScroll);

		DragScrollTimer.Interval = 10;
		DragScrollTimer.Tick += OnDragScrollTimer;
		
		InsertCaret = new ManagedCaret(this);
		InsertCaret.Visible = true;
		Selection = new SelectionRange(this);
		Selection.Changed += new EventHandler(OnSelectionChanged);
		Document.Changed += new PieceBuffer.BufferChangedEventHandler(OnBufferChanged);
		Document.HistoryJumped += OnHistoryJumped;
		Document.HistoryUndone += OnHistoryJumped;
		Document.HistoryRedone += OnHistoryJumped;
	}

	protected override void Dispose (bool disposing)
	{
		if(disposing)
			InsertCaret.Dispose();
		
		base.Dispose(disposing);
	}

	public void Copy()
	{
		ClipboardRange = Document.ClipboardCopy(Selection.BufferRange.Start, Selection.BufferRange.End);
	}
	
	public void Cut()
	{
		ClipboardRange = Document.ClipboardCut(Selection.BufferRange.Start, Selection.BufferRange.End);
	}
	
	public void Paste()
	{
		if(ClipboardRange != null)
			Document.ClipboardPaste(Selection.BufferRange.Start, Selection.BufferRange.End, ClipboardRange);
	}
	
	public void AddHighlight(SelectionRange s)
	{
		Highlights.Insert(0, s);
		Invalidate();
	}
	
	public void RemoveHighlight(SelectionRange s)
	{
		Highlights.Remove(s);
		Invalidate();
	}
	
	public void ClearHighlights()
	{
		Highlights.Clear();
		Invalidate();
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
		if(address < Document.Length * 8 && LayoutDimensions.BitsPerRow > 0)
			ScrollToPixel((long)((double)(address / LayoutDimensions.BitsPerRow) * (double)LayoutDimensions.WordSize.Height));
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

	// TODO: Move IntToRadixString to a utility class
	public string IntToRadixString(ulong x, uint radix, int minLength)
	{
		string str = "";
		const string digit		= "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string padding	= "00000000000000000000000000000000000000000000000000000000000000000";

		if(radix < 2 || radix > 36)
			return null;

		while(x > 0)
		{
			ulong v = x % radix;
			str = digit[(int)v] + str;
			x = (x - v) / radix;
		}

		if(str.Length < minLength)
			str = padding.Substring(0, minLength - str.Length <= 64 ? minLength - str.Length : 64) + str;

		return str;
	}

	private ulong GetWord(long address)
	{
		address /= 8;
		address = (address / _BytesPerWord) * _BytesPerWord;
		
		ulong x = 0;
		
		if(_Endian == Endian.Little)
		{
			for(int i = 0; i < _BytesPerWord; ++i)
				x |= (ulong)Document[address + i] << (i*8);		
		}
		else
		{
			for(int i = 0; i < _BytesPerWord; ++i)
			{
				x <<= 8;
				x |= (ulong)Document[address + i];
			}
		}
		
		return x;
	}
	
	private void UpdateWord(long address, int newDigitVal)
	{
		ulong word = GetWord(address);
		
		// Work out which digit of the word we're changing
		long digit = address - (address / LayoutDimensions.BitsPerRow) * LayoutDimensions.BitsPerRow;
		int wordAddr = (int)(digit / 8) / _BytesPerWord;
		digit -= wordAddr * _BytesPerWord * 8;
		digit /= (_BytesPerWord * 8) / LayoutDimensions.NumWordDigits;
		
		digit = LayoutDimensions.NumWordDigits - digit - 1;

		// Find the old value of the digit
		ulong oldDigitVal = word;
		oldDigitVal /= (ulong)Math.Pow(_DataRadix, digit);
		oldDigitVal %= _DataRadix;
		
		// Update the word to reflect the digit's new value
		word -= oldDigitVal * (ulong)Math.Pow(_DataRadix, digit);
		word += (ulong)newDigitVal * (ulong)Math.Pow(_DataRadix, digit);
		
		// Update the buffer
		byte[] data = new byte[_BytesPerWord];
		if(_Endian == Endian.Little)
		{
			for(int i = 0; i < _BytesPerWord; ++i)
			{
				data[i] = (byte)(word & 0xFF);
				word >>= 8;
			}
		}
		else
		{
			for(int i = 0; i < _BytesPerWord; ++i)
			{
				data[_BytesPerWord - i - 1] = (byte)(word & 0xFF);
				word >>= 8;
			}
		}
		
		address /= 8;
		address = (address / _BytesPerWord) * _BytesPerWord;
		
		PieceBuffer.Mark a = Document.Marks.Add(address);
		PieceBuffer.Mark b = Document.Marks.Add(address + _BytesPerWord);
		Document.Insert(a, b, data, _BytesPerWord);
		Document.Marks.Remove(a);
		Document.Marks.Remove(b);
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
		
		public override string ToString()
		{
			return String.Format("{0}: {1}", Type, Address);
		}
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
			if(DragStartHit != null && (DragStartHit.Type == HexViewHit.HitType.Ascii || DragStartHit.Type == HexViewHit.HitType.AsciiSelection))
				InsertCaret.Position = AddressToClientPointAscii(Selection.Start);
			else
				InsertCaret.Position = AddressToClientPoint(Selection.Start);
		}
	}
	
	protected void OnBufferChanged(object sender, PieceBuffer.BufferChangedEventArgs e)
	{
		if(Document.Length != CurrentDocumentLength)
		{
			CurrentDocumentLength = Document.Length;
			RecalcDimensions();
		}
		
		// TODO: Only invalidate changed region (and only if it's on screen)
		Invalidate();
	}
	
	protected void OnHistoryJumped(object sender, PieceBuffer.HistoryEventArgs e)
	{
		Selection.Set(e.NewItem.StartPosition * 8, e.NewItem.StartPosition * 8);
	}
}




