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
using System.CodeDom.Compiler;



class CompileErrorDialog : DialogBase
{
	protected RichTextBox TextBox = new RichTextBox();
	
	public CompilerErrorCollection Errors
	{
		set
		{
			foreach(CompilerError err in value)
			{
				TextBox.AppendText(String.Format("({0},{1}): {2}: {3}: {4}\r\n", err.Line, err.Column, err.IsWarning ? "Warning" : "Error", err.ErrorNumber, err.ErrorText));
			}
		}
	}
	
	public CompileErrorDialog() : base(DialogBase.Buttons.OK)
	{
		Text = "Compile Error";
		
		TextBox.Dock = DockStyle.Fill;
		Controls.Add(TextBox);
		
		Size = new System.Drawing.Size(480, 320);
	}
}
