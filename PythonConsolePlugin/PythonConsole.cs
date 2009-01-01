using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace PythonConsolePlugin
{
	public class PythonConsole : RichTextBox
	{
		IPluginHost Host;
		IronPython.Hosting.PythonEngine Python;
		MemoryStream StderrStream;
		MemoryStream StdoutStream;
		StreamReader StdoutReader;
		string LastExpression;
		
		public PythonConsole(IPluginHost host)
		{
			Host = host;
			Host.ActiveViewChanged += OnActiveViewChanged;

			Multiline = true;
			Font = new Font("Courier New", 10);

			StdoutStream = new MemoryStream();
			StdoutReader = new StreamReader(StdoutStream);
			
			Python = new IronPython.Hosting.PythonEngine();
			Python.AddToPath(Environment.CurrentDirectory);
			Python.Globals.Add("Host", host);
			Python.Globals.Add("View", Host.ActiveView);
			Python.Globals.Add("Doc", Host.ActiveView != null ? Host.ActiveView.Document : null);
			Python.Globals.Add("Struct", Host.ActiveView != null ? Host.ActiveView.Document.Structure : null);
			Python.Globals["View"] = Host.ActiveView;
			Python.SetStandardError(StdoutStream);
			Python.SetStandardOutput(StdoutStream);
		}

		protected void OnActiveViewChanged(object sender, EventArgs e)
		{
			Python.Globals["View"] = Host.ActiveView;
			Python.Globals["Doc"] = Host.ActiveView.Document;
			Python.Globals["Struct"] = Host.ActiveView.Document.Structure;
		}
		
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if(e.KeyCode == Keys.Enter)
			{
				try
				{
					SelectionFont = new Font("Courier New", 10, FontStyle.Bold);
					LastExpression = Lines[Lines.Length - 2];
					
					Python.ExecuteToConsole(LastExpression);
					StdoutStream.Position = 0;
					SelectedText = StdoutReader.ReadToEnd();
					StdoutStream.Position = 0;
				}
				catch(Exception exp)
				{
					AppendText(exp.Message + "\r\n");
				}
				SelectionStart = Text.Length;
			}
			if(e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
			{
				Select(Text.Length + 1, 0);
			}
		}
		
	}

	public class PythonConsolePlugin : IPlugin
	{
		string IPlugin.Name { get { return "Python Console"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new PythonConsole(host), "Python Console");
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
