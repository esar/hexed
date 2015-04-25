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



class MultiBaseNumericTextBox : TextBox
{
	protected Exception ParseException;
	
	protected long _Value;
	public long Value
	{
		get 
		{
			if(ParseException != null)
				throw ParseException;
			return _Value; 
		}
	}
	
	public bool IsValid
	{
		get { return ParseException == null; }
	}
	
	public MultiBaseNumericTextBox()
	{
	}
	
	protected override void OnKeyUp (KeyEventArgs e)
	{
		base.OnKeyUp (e);
		
		if(Parse())
			ForeColor = SystemColors.WindowText;
		else
			ForeColor = Color.Red;		
	}

	protected bool Parse()
	{
		ParseException = null;
		
		int radix = 10;
		string text = Text;
		
		if(text.Length <= 0)
			return true;

		char lastChar = text[text.Length - 1];

		if(text.Length >= 2 && text.StartsWith("0x"))
		{
			text = text.Substring(2);
			radix = 16;
			if(text.Length <= 0)
			{
				_Value = 0;
				return true;
			}
		}
		else if(text.Length >= 1 && text.StartsWith("0") && lastChar != 'd' && lastChar != 'h')
		{
			text = text.Substring(1);
			radix = 8;
			if(text.Length <= 0)
			{
				_Value = 0;
				return true;
			}
		}
		else
		{
			switch(lastChar)
			{
				case 'b':
					text = text.Substring(0, text.Length - 1);
					radix = 2;
					break;
				case 'o':
					text = text.Substring(0, text.Length - 1);
					radix = 8;
					break;
				case 'd':
					text = text.Substring(0, text.Length - 1);
					radix = 10;
					break;
				case 'h':
					text = text.Substring(0, text.Length - 1);
					radix = 16;
					break;
			}
		}
		
		try
		{
			_Value = Convert.ToInt32(text, radix);
		}
		catch(FormatException ex)
		{
			ParseException = ex;
			return false;
		}
		catch(OverflowException ex)
		{
			ParseException = ex;
			return false;
		}
		
		return true;
	}
}

class ShiftDialog : DialogBase
{
	protected Label    ValueLabel;
	protected MultiBaseNumericTextBox  ValueTextBox;
	protected TableLayoutPanel Panel;
	
	protected int      _Value;
	public int Value
	{
		get { return _Value; }
	}
	
	public ShiftDialog() : base(DialogBase.Buttons.OK | DialogBase.Buttons.Cancel)
	{
		Panel = new TableLayoutPanel();
		Panel.BackColor = Color.Transparent;
		Panel.RowCount = 2;
		Panel.ColumnCount = 1;
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		Panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		Panel.Dock = DockStyle.Fill;
		
		ValueLabel = new Label();
		ValueLabel.Text = "Shift Distance (bits):";
		ValueLabel.AutoSize = true;
		Panel.Controls.Add(ValueLabel);
		
		ValueTextBox = new MultiBaseNumericTextBox();
		ValueTextBox.AutoSize = true;
		ValueTextBox.Dock = DockStyle.Fill;
		Panel.Controls.Add(ValueTextBox);
		
		Controls.Add(Panel);
		
		Size = new Size(320, 160);
	}

	
	protected override void OnOK(object sender, EventArgs e)
	{
		
		try
		{
			_Value = (int)ValueTextBox.Value;
		}
		catch(FormatException)
		{
			MessageBox.Show(String.Format("'{0}' is not a valid number", ValueTextBox.Text), "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		catch(OverflowException)
		{
			MessageBox.Show(String.Format("'{0}' is too large", ValueTextBox.Text), "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		

		base.OnOK(sender, e);
	}
}
