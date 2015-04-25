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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


public class GradientPanel : Panel
{
	Size ShadowSize = new Size(4, 4);
	
	public GradientPanel()
	{
	}
	
	protected override void OnPaintBackground(PaintEventArgs e)
	{	
		base.OnPaintBackground(e);
		
		if(!TabRenderer.IsSupported)
		{
			Rectangle contentRectangle = new Rectangle(ClientRectangle.Left, ClientRectangle.Top, ClientRectangle.Width - ShadowSize.Width, ClientRectangle.Height - ShadowSize.Height);
			
			GraphicsPath bottomShadow = new GraphicsPath();
			bottomShadow.AddLines(new Point[] { new Point(contentRectangle.Left + ShadowSize.Width, contentRectangle.Bottom),
				                                new Point(contentRectangle.Right - 1, contentRectangle.Bottom),
				                                new Point(contentRectangle.Right + ShadowSize.Width - 1, contentRectangle.Bottom + ShadowSize.Height),
				                                new Point(contentRectangle.Left + ShadowSize.Width, contentRectangle.Bottom + ShadowSize.Height) });
			bottomShadow.CloseAllFigures();
			
			GraphicsPath rightShadow = new GraphicsPath();
			rightShadow.AddLines(new Point[] { new Point(contentRectangle.Right, contentRectangle.Top + ShadowSize.Height),
				                               new Point(contentRectangle.Right + ShadowSize.Width, contentRectangle.Top + ShadowSize.Height),
				                               new Point(contentRectangle.Right + ShadowSize.Width, contentRectangle.Bottom + ShadowSize.Height - 1),
				                               new Point(contentRectangle.Right, contentRectangle.Bottom - 1) });
			rightShadow.CloseAllFigures();
			
			Brush pgb = new LinearGradientBrush(new Point(0, contentRectangle.Bottom), new Point(0, contentRectangle.Bottom + ShadowSize.Height), Color.Black, Color.Transparent);
			e.Graphics.FillPath(pgb, bottomShadow);
			pgb.Dispose();
			bottomShadow.Dispose();
			
			pgb = new LinearGradientBrush(new Point(contentRectangle.Right, 0), new Point(contentRectangle.Right + ShadowSize.Width, 0), Color.Black, Color.Transparent);
			e.Graphics.FillPath(pgb, rightShadow);
			pgb.Dispose();
			rightShadow.Dispose();
			
			/*
			Rectangle r = new Rectangle(contentRectangle.Right, contentRectangle.Top + ShadowSize.Height, ShadowSize.Width, contentRectangle.Height - ShadowSize.Height);
			Brush gb = new LinearGradientBrush(r, Color.Black, Color.Transparent, LinearGradientMode.Horizontal);
			e.Graphics.FillRectangle(gb, r);
			gb.Dispose();
			
			r = new Rectangle(contentRectangle.Left + ShadowSize.Width, contentRectangle.Bottom, contentRectangle.Width - ShadowSize.Width, ShadowSize.Height);
			gb = new LinearGradientBrush(r, Color.Black, Color.Transparent, LinearGradientMode.Vertical);
			e.Graphics.FillRectangle(gb, r);
			gb.Dispose();
			
			GraphicsPath path = new GraphicsPath();
			path.AddEllipse(contentRectangle.Right - ShadowSize.Width, contentRectangle.Bottom - ShadowSize.Height, ShadowSize.Width * 2, ShadowSize.Height * 2);
			PathGradientBrush pgb = new PathGradientBrush(path);
			pgb.CenterColor = Color.Black;
			pgb.SurroundColors = new Color[] { Color.Transparent };
			e.Graphics.FillPath(pgb, path);
			pgb.Dispose();
			path.Dispose();
*/
			Brush gb = new LinearGradientBrush(contentRectangle, SystemColors.ControlLight, SystemColors.Control, LinearGradientMode.Vertical);
			e.Graphics.FillRectangle(gb, contentRectangle);
			gb.Dispose();
			
			e.Graphics.DrawRectangle(SystemPens.ControlDarkDark, contentRectangle);
		}
		else
			TabRenderer.DrawTabPage(e.Graphics, ClientRectangle);
	}

}

public class DialogBase : Form
{
	public enum Buttons
	{
		None = 0,
		OK = 1,
		Apply = 2,
		Cancel = 4,
		All = 7
	}
	
	protected FlowLayoutPanel  ButtonPanel;
	protected Button OkButton;
	protected new Button CancelButton;
	protected Button ApplyButton;
	protected GradientPanel  ContentPanel;
	
	public new Control.ControlCollection Controls
	{
		get { return ContentPanel.Controls; }
	}
	
	public DialogBase(Buttons buttons)
	{		
		ButtonPanel = new FlowLayoutPanel();
		ButtonPanel.FlowDirection = FlowDirection.RightToLeft;
		ButtonPanel.Dock = DockStyle.Bottom;
		ButtonPanel.AutoSize = true;
		ButtonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		ButtonPanel.Padding = new Padding(0, 0, 20, 0);
		ButtonPanel.BackColor = Color.Transparent;

		if((buttons & Buttons.Apply) != 0)
		{
			ApplyButton = new Button();
			ApplyButton.Text = "Apply";
			ApplyButton.Click += OnApply;
			ApplyButton.Margin = new Padding(3, 6, 3, 2);
			ButtonPanel.Controls.Add(ApplyButton);
		}
		
		if((buttons & Buttons.Cancel) != 0)
		{
			CancelButton = new Button();
			CancelButton.Text = "Cancel";
			CancelButton.Click += OnCancel;
			CancelButton.Margin = new Padding(3, 6, 3, 2);
			CancelButton.CausesValidation = false;
			ButtonPanel.Controls.Add(CancelButton);
		}

		if((buttons & Buttons.OK) != 0)
		{
			OkButton = new Button();
			OkButton.Text = "OK";
			OkButton.Click += OnOK;
			OkButton.Margin = new Padding(3, 6, 3, 2);
			ButtonPanel.Controls.Add(OkButton);
		}
		
		ContentPanel = new GradientPanel();
		ContentPanel.Padding = new Padding(5, 5, 8, 5);
		ContentPanel.Dock = DockStyle.Fill;
		
		base.Controls.Add(ContentPanel);
		base.Controls.Add(ButtonPanel);
		
		base.AcceptButton = OkButton;
		base.CancelButton = CancelButton;
		
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
