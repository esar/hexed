using System;
using System.Windows.Forms;
using System.Drawing;


public class ManagedCaret
{
	Control _Control;
	Size	_Size;
	Point	_Position;
	bool	_Visible;
	bool	_Drawn;
	Timer	_Timer;

	public ManagedCaret(Control control) 
	{
		_Control = control;
		_Position = Point.Empty;
		_Size = new Size(1, control.Font.Height);
		_Visible = false;
		_Timer = new Timer();
		_Timer.Interval = 500;
		_Timer.Tick += OnTimerTick;
		_Drawn = false;
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
		set 
		{
			Hide();
			_Size = value;
			Show();
		}
	}

	public Point Position 
	{
		get { return _Position; }
		set 
		{
			Hide();
			_Position = value;
			Show();
		}
	}

	public bool Visible 
	{
		get { return _Visible; }
		set	
		{ 
			_Visible = value;
			if(_Visible)
				Show();
			else
				Hide();
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
		Show();
	}

	private void OnLostFocus(object sender, EventArgs e) 
	{
		Hide();
	}
	
	private void OnTimerTick(object sender, EventArgs e)
	{
		if(_Visible)
			ToggleCaret();
	}
	
	private void Show()
	{
		if(_Visible)
		{
			ToggleCaret();
			_Timer.Enabled = true;
		}
	}
	
	private void Hide()
	{
		_Timer.Enabled = false;
		if(_Drawn)
			ToggleCaret();
	}
	
	private void ToggleCaret()
	{
		if(_Drawn)
		{
			_Control.Invalidate(new Rectangle(_Position.X, _Position.Y - _Size.Height, _Size.Width, _Size.Height));
		}
		else
		{
			Rectangle r = new Rectangle(_Position.X, _Position.Y - _Size.Height, _Size.Width, _Size.Height);
			r.Intersect(_Control.ClientRectangle);
			ControlPaint.FillReversibleRectangle(_Control.RectangleToScreen(r), Color.Black);
		}

		_Drawn = !_Drawn;
	}
}
