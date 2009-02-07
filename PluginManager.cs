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
