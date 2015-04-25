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


	public enum DefaultWindowPosition
	{
		Left,
		Right,
		BottomLeft,
		BottomRight,
		Floating
	}

public interface IPlugin
{
	Image Image { get; }
	string Name { get; }
	string Description { get; }
	string Author { get; }
	string Version { get; }
	string Copyright { get; }
	string Url { get; }
	
	void Initialize(IPluginHost host);
	void Dispose();
}


public interface IPluginHost
{
	event EventHandler ActiveViewChanged;
	
	Settings Settings { get; }
	CommandSet Commands { get; }
	HexView ActiveView { get; }
	ProgressNotificationCollection ProgressNotifications { get; }
	
	ToolStripButton CreateToolButton(string commandName);
	ToolStripMenuItem CreateMenuItem(string commandName);
	void AddMenuItem(Menus menu, string path, ToolStripItem menuItem);
	void AddWindow(Control control, string name, Image image, DefaultWindowPosition defaultPosition, bool visibleByDefault);
	void BringToFront(Control control);
}
