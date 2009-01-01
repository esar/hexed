using System;
using System.Windows.Forms;



public interface IPlugin
{
	string Name { get; }
	string Author { get; }
	string Version { get; }
	
	void Initialize(IPluginHost host);
	void Dispose();
}


public interface IPluginHost
{
	event EventHandler ActiveViewChanged;
	
	HexView ActiveView { get; }
	
	ToolStripMenuItem AddMenuItem(string path);
	void AddWindow(Control control, string name);
}
