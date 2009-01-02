/*
	This file is part of TortoiseHg

	Copyright (C) 2006, 2007  Stephen Robinson <stephen@tortoisehg.esar.org.uk>

	TortoiseHg is free software; you can redistribute it and/or modify
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


using System.Windows.Forms;

class HorzRule : Control
{
	public HorzRule()
	{
	}

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ClassName = "STATIC";
			createParams.Style |= 0x10;	// SS_ETCHEDHORZ
			return createParams;
		}
	}
}

