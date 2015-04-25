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
		control.Paint += new PaintEventHandler(OnPaint);
		control.HandleDestroyed += new EventHandler(OnHandleDestroyed);
		
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
			if(_Visible && Control.Focused)
				Show();
			else
				Hide();
		}
	}

	public void Dispose() 
	{
		_Timer.Stop();
		_Timer.Dispose();
		
		if(_Control.Focused)
			OnLostFocus(_Control, new EventArgs());
		_Control.GotFocus -= new EventHandler(OnGotFocus);
		_Control.LostFocus -= new EventHandler(OnLostFocus);
	}

	protected void OnPaint(object sender, PaintEventArgs e)
	{
		// Schedule repainting of the caret after the control has finished painting
		// i.e. next time we go around the event loop.
		_Control.BeginInvoke((MethodInvoker)delegate() { Repaint(); } );
	}
	
	private void OnHandleDestroyed(object sender, EventArgs e)
	{
		Hide();
	}
	
	private void Repaint()
	{
		if(_Visible && _Drawn)
		{
			_Drawn = false;
			ToggleCaret();
		}
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
		if(!Control.Focused)
		{
			Hide();
			return;
		}
		
		if(_Visible)
			ToggleCaret();
	}
	
	private void Show()
	{
		if(_Visible)
		{
			if(!_Drawn)
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
