using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


class GradientPanel : Panel
{
	public GradientPanel()
	{
	}
	
	protected override void OnPaintBackground(PaintEventArgs e)
	{
		base.OnPaintBackground(e);
		
		//Brush brush = new LinearGradientBrush(ClientRectangle, SystemColors.ControlLight, SystemColors.Control, LinearGradientMode.Vertical);
		//e.Graphics.FillRectangle(brush, ClientRectangle);
		if(TabRenderer.IsSupported)
		{
			TabRenderer.DrawTabPage(e.Graphics, ClientRectangle);
		}
		else
		{
			Brush brush = new LinearGradientBrush(ClientRectangle, SystemColors.ControlLight, SystemColors.Control, LinearGradientMode.Vertical);
			e.Graphics.FillRectangle(brush, ClientRectangle);
			ControlPaint.DrawVisualStyleBorder(e.Graphics, ClientRectangle);
		}
			
		
		
	}

}

class DialogBase : Form
{
	FlowLayoutPanel  ButtonPanel;
	Button OkButton;
	Button CancelButton;
	Button ApplyButton;
	GradientPanel  ContentPanel;
	
	public new Control.ControlCollection Controls
	{
		get { return ContentPanel.Controls; }
	}
	
	public DialogBase()
	{
		ButtonPanel = new FlowLayoutPanel();
		ButtonPanel.FlowDirection = FlowDirection.RightToLeft;
		ButtonPanel.Dock = DockStyle.Bottom;
		ButtonPanel.AutoSize = true;
		ButtonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		ButtonPanel.Padding = new Padding(0, 0, 20, 0);
		//ButtonPanel.BackColor = Color.Transparent;
		
		OkButton = new Button();
		OkButton.Text = "OK";
		OkButton.Click += OnOK;
		CancelButton = new Button();
		CancelButton.Text = "Cancel";
		CancelButton.Click += OnCancel;
		ApplyButton = new Button();
		ApplyButton.Text = "Apply";
		ApplyButton.Click += OnApply;
		
		ButtonPanel.Controls.Add(OkButton);
		ButtonPanel.Controls.Add(CancelButton);
		ButtonPanel.Controls.Add(ApplyButton);
		
		ContentPanel = new GradientPanel();
		ContentPanel.Dock = DockStyle.Fill;
		
		base.Controls.Add(ContentPanel);
		base.Controls.Add(ButtonPanel);
		
		SizeGripStyle = SizeGripStyle.Show;
		DockPadding.All = 5;
	}
	
	protected virtual void OnOK(object sender, EventArgs e)
	{
		DialogResult = DialogResult.OK;
		Close();
	}
	
	protected virtual void OnCancel(object sender, EventArgs e)
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}
	
	protected virtual void OnApply(object sender, EventArgs e)
	{
	}
}
