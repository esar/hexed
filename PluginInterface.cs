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
