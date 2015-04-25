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
using System.Reflection;
using System.Windows.Forms;
using System.IO;


public class PluginDictionary : Dictionary<string, IPlugin> {}

public class PluginManager
{
	protected IPluginHost Host;
	
	protected PluginDictionary _ActivePlugins = new PluginDictionary();
	public PluginDictionary ActivePlugins { get { return _ActivePlugins; } }
	
	protected PluginDictionary _InactivePlugins = new PluginDictionary();
	public PluginDictionary InactivePlugins { get { return _InactivePlugins; } }

	
	public PluginManager(IPluginHost host)
	{
		Host = host;
	}
	
	public void LoadPlugins()
	{
		SplashScreen.Status = "Searching for plugins...";
		
		string path = Application.StartupPath;
		string[] filenames = Directory.GetFiles(path, "*.dll");

		foreach(string filename in filenames)
		{
			try
			{
				Assembly asm = Assembly.LoadFile(filename);
				if(asm != null)
				{
					Type[] types = asm.GetTypes();
					foreach(Type type in types)
					{
						if(typeof(IPlugin).IsAssignableFrom(type))
						{
							Console.WriteLine("Loaded plugin: " + filename + " :: " + type);
							IPlugin p = (IPlugin)Activator.CreateInstance(type);
							SplashScreen.Status = String.Format("Loading plugin: {0}...", p.Name);
							p.Initialize(Host);
							_ActivePlugins.Add(filename, p);
						}
					}
				}
				else
					Console.WriteLine("Failed to load plugin assembly");
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
			}		
        }
	}
}
