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


	class TitleLabel : Label
	{
		public TitleLabel()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
			Padding = new Padding(2);
			ForeColor = SystemColors.ActiveCaptionText;
			BackColor = Color.Transparent;
		}

		protected override void OnCreateControl()
		{
			Graphics g = CreateGraphics();
			SizeF size = g.MeasureString("Hg", Font);
			g.Dispose();
			Height = (int)size.Height + 6;
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground(e);

			LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, SystemColors.ButtonShadow, Color.Transparent, 0.0F);
			e.Graphics.FillRectangle(brush, ClientRectangle);
			brush.Dispose();
		}
	}
