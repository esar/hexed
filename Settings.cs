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
//		BasePath = Application.StartupPath;
//		if(BasePath.EndsWith("\\bin\\debug", true, null) || BasePath.EndsWith("\\bin\\release", true, null))
//		{
//			BasePath = BasePath.Substring(0, BasePath.LastIndexOf('\\'));
//			BasePath = BasePath.Substring(0, BasePath.LastIndexOf('\\'));
//		}
		
		BasePath = "/home/stephen/projects/dotnet/hexed";
	}
	
	public System.Drawing.Image Image(string name)
	{
		return System.Drawing.Image.FromFile(string.Format("{0}/icons/{1}", BasePath, name));
	}
}
