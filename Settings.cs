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
