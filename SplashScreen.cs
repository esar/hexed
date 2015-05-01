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
using System.Drawing;


class SplashScreen : Form
{
	private static SplashScreen _Instance;
	protected static SplashScreen Instance
	{
		get 
		{
			if(_Instance == null)
				_Instance = new SplashScreen();
			return _Instance;
		}
	}
	
	Label    StatusValue = new Label();
	
	public static string Status
	{
		set
		{
			Instance.StatusValue.Text = value;
			Application.DoEvents();
		}
	}
	
	public SplashScreen()
	{
		FormBorderStyle = FormBorderStyle.None;
		BackgroundImage = Settings.Instance.Image("icons.splash.jpg");
		//BackColor = Color.White;
		ForeColor = Color.White;
		Font = new Font(Font.SystemFontName, 10, FontStyle.Bold);
		
//		Panel.ColumnCount = 1;
//		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
//		Panel.BackColor = Color.Transparent;
//		Panel.RowCount = 1;
		
		StatusValue.Text = "Loading...";
		StatusValue.Dock = DockStyle.Fill;
		StatusValue.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
		StatusValue.BackColor = Color.Transparent;
		StatusValue.Padding = new Padding(0, 0, 0, 10);
		Controls.Add(StatusValue);
//		Panel.Controls.Add(StatusValue);
		
//		Panel.Dock = DockStyle.Fill;
//		Controls.Add(Panel);
		
		StartPosition = FormStartPosition.CenterScreen;
		Size = new System.Drawing.Size(320, 240);
	}
	
	public new static void Show()
	{
		Instance.Show(null);
		Application.DoEvents();
	}
	
	public new static void Hide()
	{
		((Form)Instance).Hide();
		Application.DoEvents();
	}
}
