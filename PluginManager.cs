using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.IO;


public class PluginCollection : Dictionary<string, IPlugin>
{
}

public class PluginManager
{
	protected IPluginHost Host;
	
	protected PluginCollection _ActivePlugins = new PluginCollection();
	public PluginCollection ActivePlugins { get { return _ActivePlugins; } }
	
	protected PluginCollection _InactivePlugins = new PluginCollection();
	public PluginCollection InactivePlugins { get { return _InactivePlugins; } }

	
	public PluginManager(IPluginHost host)
	{
		Host = host;
	}
	
	public void LoadPlugins()
	{
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
