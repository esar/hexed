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
using System.Windows.Forms;


class CodeViewForm : Form
{
	public CodeViewForm()
	{
		CodeView cv = new CodeView();
		cv.Dock = DockStyle.Fill;
		Controls.Add(cv);
	}
}

class CodeView : RichTextBox
{
	public CodeView()
	{
		
	}
	
	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		if(SelectionLength == 0)
			Console.WriteLine("Inserting: " + e.KeyChar);
		else
			Console.WriteLine("Replacing: '{0}' from {1} to {2} with '{3}'", SelectedText, SelectionStart, SelectionStart + SelectionLength, e.KeyChar);
		
		base.OnKeyPress(e);
	}

	
	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		
		Console.WriteLine("change: Selected Text: '{0}', selstart: {1}", SelectedText, SelectionStart);
	}

}
