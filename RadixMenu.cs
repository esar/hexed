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



class RadixMenu : ToolStripMenuItem
{
	private ToolStripMenuItem	BinaryItem;
	private ToolStripMenuItem	OctalItem;
	private ToolStripMenuItem	DecimalItem;
	private ToolStripMenuItem	HexItem;
	private ToolStripMenuItem[]	RadixItems = new ToolStripMenuItem[37];
	private EventHandler		Handler;

	public RadixMenu(string text, string name, EventHandler handler) : base(text, null, null, name)
	{
		BinaryItem = new ToolStripMenuItem("&Binary", null, OnSelectItem, name);
		BinaryItem.Tag = 2;
		DropDownItems.Add(BinaryItem);
		OctalItem = new ToolStripMenuItem("&Octal", null, OnSelectItem, name);
		OctalItem.Tag = 8;
		DropDownItems.Add(OctalItem);
		DecimalItem = new ToolStripMenuItem("&Decimal", null, OnSelectItem, name);
		DecimalItem.Tag = 10;
		DropDownItems.Add(DecimalItem);
		HexItem = new ToolStripMenuItem("&Hexadecimal", null, OnSelectItem, name);
		HexItem.Tag = 16;
		DropDownItems.Add(HexItem);
		
		DropDownItems.Add("-");
		
		ToolStripMenuItem others = (ToolStripMenuItem)DropDownItems.Add("Others");

		for(int i = 2; i <= 36; ++i)
		{
			RadixItems[i] = new ToolStripMenuItem(i.ToString(), null, OnSelectItem, name);
			RadixItems[i].Tag = i;
			others.DropDownItems.Add(RadixItems[i]);
		}
		
		Handler = handler;
	}

	public int SelectedRadix
	{
		get
		{
			for(int i = 2; i <= 36; ++i)
				if(RadixItems[i].Checked)
					return i;

			return 0;
		}

		set
		{
			if(SelectedRadix == value)
				return;
			
			ToolStripMenuItem selectedItem = null;
			
			for(int i = 2; i <= 36; ++i)
			{
				if(i == value)
				{
					RadixItems[i].Checked = true;
					selectedItem = RadixItems[i];
				}
				else
					RadixItems[i].Checked = false;
			}

			BinaryItem.Checked = false;
			OctalItem.Checked = false;
			DecimalItem.Checked = false;
			HexItem.Checked = false;

			switch(value)
			{
				case 2:
					BinaryItem.Checked = true;
					break;
				case 8:
					OctalItem.Checked = true;
					break;
				case 10:
					DecimalItem.Checked = true;
					break;
				case 16:
					HexItem.Checked = true;
					break;
			}

			if(selectedItem != null)	
				Handler(selectedItem, new EventArgs());
		}
	}
	
	private void OnSelectItem(object sender, EventArgs args)
	{
		ToolStripMenuItem selectedItem = null;
		
		if(sender == BinaryItem)
			SelectedRadix = 2;
		else if(sender == OctalItem)
			SelectedRadix = 8;
		else if(sender == DecimalItem)
			SelectedRadix = 10;
		else if(sender == HexItem)
			SelectedRadix = 16;
		else
		{
			for(int i = 2; i <= 36; ++i)
			{
				if(sender == RadixItems[i])
				{
					SelectedRadix = i;
					selectedItem = RadixItems[i];
					break;
				}
			}
		}
		
		if(selectedItem != null)
			Handler(selectedItem, new EventArgs());
	}
}
