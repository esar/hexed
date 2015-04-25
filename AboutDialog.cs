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


class AboutDialog : Form
{
	public AboutDialog()
	{
		Label label = new Label();

		label.Text =	"Performance Viewer\n" +
						"\n" +
						"0.04  -  Fixed exception when counter goes away\n" +
						"0.03  -  Added auto reduce to Y scale\n" +
						"            Added reset toolbar button\n" +
						"0.02  -  Fixed '% disk...' counters\n" +
						"0.01  -  Initial release";
		label.Dock = DockStyle.Fill;

		Controls.Add(label);
	}
}
