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


public class Settings
{
	public string BasePath;
	
	static Settings _Instance;
	public static Settings Instance
	{
		get 
		{ 
			if(_Instance == null)
				_Instance = new Settings();
			return _Instance; 
		}
	}
	
	public Settings()
	{
		BasePath = Application.StartupPath;
		if(	BasePath.EndsWith(String.Format("{0}bin{0}debug", System.IO.Path.DirectorySeparatorChar), true, null) ||
			BasePath.EndsWith(String.Format("{0}bin{0}release", System.IO.Path.DirectorySeparatorChar), true, null))
		{
			BasePath = BasePath.Substring(0, BasePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
			BasePath = BasePath.Substring(0, BasePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
		}
	}
	
	public System.Drawing.Image Image(string name)
	{
		return System.Drawing.Image.FromFile(string.Format("{0}/icons/{1}", BasePath, name));
	}
	
	public System.Drawing.Icon Icon(string name)
	{
		return new Icon(string.Format("{0}/icons/{1}", BasePath, name));
	}
}
