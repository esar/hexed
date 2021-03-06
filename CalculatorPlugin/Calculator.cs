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
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;


namespace CalculatorPlugin
{
	class CalculatorPanel : TableLayoutPanel
	{
		private IPluginHost	Host;

		protected TextBox	Result = new TextBox();
		protected Button[]	NumberButtons = new Button[16]; 
		
		public CalculatorPanel(IPluginHost host)
		{
			Host = host;
		
			BackColor = SystemColors.ButtonFace;
			
			RowCount = 5;
			ColumnCount = 6;
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			
			Result.Dock = DockStyle.Fill;
			Controls.Add(Result, 0, 0);
			SetColumnSpan(Result, 6);
			
			for(int i = 0; i < 16; ++i)
			{
				NumberButtons[i] = new Button();
				NumberButtons[i].Text = i.ToString("X");
				NumberButtons[i].AutoSize = true;
			}
			
			Controls.Add(NumberButtons[0], 0, 6);
			Controls.Add(NumberButtons[1], 0, 5);
			Controls.Add(NumberButtons[2], 1, 5);
			Controls.Add(NumberButtons[3], 2, 5);
			Controls.Add(NumberButtons[4], 0, 4);
			Controls.Add(NumberButtons[5], 1, 4);
			Controls.Add(NumberButtons[6], 2, 4);
			Controls.Add(NumberButtons[7], 0, 3);
			Controls.Add(NumberButtons[8], 1, 3);
			Controls.Add(NumberButtons[9], 2, 3);
			Controls.Add(NumberButtons[10], 0, 2);
			Controls.Add(NumberButtons[11], 1, 2);
			Controls.Add(NumberButtons[12], 2, 2);
			Controls.Add(NumberButtons[13], 0, 1);
			Controls.Add(NumberButtons[14], 1, 1);
			Controls.Add(NumberButtons[15], 2, 1);
		}
	}
	
	public class MyClass : IPlugin
	{
		public Image Image { get { return Settings.Instance.Image("icons.calculator_16.png"); } }
		public string Name { get { return "Calculator"; } }
		public string Description { get { return "Provides a simple calculator"; } }
		public string Author { get { return "Stephen Robinson"; } }
		public string Version { get { return "1.0"; } }
		public string Copyright { get { return "(c)2008 Stephen Robinson"; } }
		public string Url { get { return "http://www.esar.org.uk/"; } }

		public void Initialize(IPluginHost host)
		{
			host.AddWindow(new CalculatorPanel(host), "Calculator", host.Settings.Image("icons.calculator_16.png"), DefaultWindowPosition.BottomLeft, true);
		}
		
		public void Dispose()
		{
		}
	}
}
