using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;


public class Win32Caret
{
	[DllImport("user32.dll")]
	public static extern int CreateCaret(IntPtr hwnd, IntPtr hbm, int cx, int cy);
	[DllImport("user32.dll")]
	public static extern int DestroyCaret();
	[DllImport("user32.dll")]
	public static extern int SetCaretPos(int x, int y);
	[DllImport("user32.dll")]
	public static extern int ShowCaret(IntPtr hwnd);
	[DllImport("user32.dll")]
	public static extern int HideCaret(IntPtr hwnd);

	Control _Control;
	Size	_Size;
	Point	_Position;
	bool	_Visible;

	public Win32Caret(Control control) 
	{
		_Control = control;
		_Position = Point.Empty;
		_Size = new Size(1, control.Font.Height);
		_Visible = false;
		control.GotFocus += new EventHandler(OnGotFocus);
		control.LostFocus += new EventHandler(OnLostFocus);
		
		if(control.Focused)
			OnGotFocus(control, new EventArgs());
	}

	public Control Control 
	{
		get { return _Control; }
	}

	public Size Size 
	{
		get { return _Size; }
		set { _Size = value; }
	}

	public Point Position 
	{
		get { return _Position; }
		set { _Position = value; /*SetCaretPos(_Position.X, _Position.Y - _Size.Height);*/ }
	}

	public bool Visible 
	{
		get { return _Visible; }
		set 
		{
			if(value != _Visible)
			{
				_Visible = value; 
//				if(_Visible)
//					ShowCaret(Control.Handle);
//				else
//					HideCaret(Control.Handle);
			}
		}
	}

	public void Dispose() 
	{
		if(_Control.Focused)
			OnLostFocus(_Control, new EventArgs());
		_Control.GotFocus -= new EventHandler(OnGotFocus);
		_Control.LostFocus -= new EventHandler(OnLostFocus);
	}

	private void OnGotFocus(object sender, EventArgs e) 
	{
//		CreateCaret(_Control.Handle, IntPtr.Zero, _Size.Width, _Size.Height);
//		SetCaretPos(_Position.X, _Position.Y);
		Visible = true;
	}

	private void OnLostFocus(object sender, EventArgs e) 
	{
		Visible = false;
//		DestroyCaret();
	}
}
