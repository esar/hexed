using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PythonConsolePlugin
{
	public class PythonConsoleFeederStream : Stream
	{
		protected PythonConsole Console;
		
		public override long Position 
		{ 
			get { return 0; }
			set {}
		}
		
		public override long Length
		{
			get { return 0; }
		}
		
		public override bool CanWrite
		{
			get { return true; }
		}
		
		public override bool CanSeek
		{
			get { return false; }
		}
		
		public override bool CanRead
		{
			get { return false; }
		}

		
		public PythonConsoleFeederStream(PythonConsole console)
		{
			Console = console;
		}
		
		public override void Write(byte[] buffer, int offset, int count)
		{
			while(count > 0)
			{
				int len = count > 4096 ? 4096 : count;
				string text = System.Text.Encoding.ASCII.GetString(buffer, offset, len);
				Console.Feed(this, text);
				offset += len;
				count -= len;
			}
		}
		
		public override void SetLength(long value)
		{
		}
		
		public override long Seek(long offset, SeekOrigin origin)
		{
			return 0;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return 0;
		}
		
		public override void Flush()
		{
		}
		
		

	}
	
	public class PythonConsole : RichTextBox
	{
		IPluginHost Host;
		IronPython.Hosting.PythonEngine Python;
		PythonConsoleFeederStream StderrStream;
		PythonConsoleFeederStream StdoutStream;
		StringBuilder CurrentExpression = new StringBuilder();
		Document CurrentDocument;
		List<string> CurrentGlobals = new List<string>();
		Font ResultFont;
		Font ErrorFont;

		
		public PythonConsole(IPluginHost host)
		{
			Host = host;
			Host.ActiveViewChanged += OnActiveViewChanged;

			Multiline = true;
			Font = new Font(FontFamily.GenericMonospace, 10);
			ResultFont = new Font(Font, FontStyle.Bold);
			ErrorFont = ResultFont;

			StdoutStream = new PythonConsoleFeederStream(this);
			StderrStream = new PythonConsoleFeederStream(this);
			
			Python = new IronPython.Hosting.PythonEngine();
			Python.AddToPath(Environment.CurrentDirectory);
			Python.Globals.Add("Host", host);
			Python.Globals.Add("View", Host.ActiveView);
			Python.Globals.Add("Doc", Host.ActiveView != null ? Host.ActiveView.Document : null);
			OnActiveViewChanged(host, EventArgs.Empty);
			Python.SetStandardOutput(StdoutStream);
			Python.SetStandardError(StderrStream);
		}

		public void Feed(PythonConsoleFeederStream sender, string text)
		{
			if(sender == StdoutStream)
				AppendText(text, ResultFont, Color.Black);
			else if(sender == StderrStream)
				AppendText(text, ErrorFont, Color.Red);
			else
				AppendText(text, Font, Color.Black);
		}
		
		public void AppendText(string text, Font font, Color color)
		{
			int start = TextLength;
			AppendText(text);
			int end = TextLength;

			// Textbox may transform chars, so (end-start) != text.Length
			Select(start, end - start);
			{
				SelectionColor = color;
				SelectionFont = font;
			}
			
			SelectionStart = TextLength;
//			SelectionLength = 0; // clear
			SelectionColor = Color.Black;
			SelectionFont = Font;
		}
		
		protected void OnActiveViewChanged(object sender, EventArgs e)
		{
			if(CurrentDocument != null)
				CurrentDocument.MetaData.ItemChanged -= OnDocumentMetaDataItemChanged;
			
			Python.Globals["View"] = Host.ActiveView;
			if(Host.ActiveView != null)
			{
				CurrentDocument = Host.ActiveView.Document;
				CurrentDocument.MetaData.ItemChanged += OnDocumentMetaDataItemChanged;
				Python.Globals["Doc"] = CurrentDocument;
			}
			else
				Python.Globals["Doc"] = null;
			
			foreach(string s in CurrentGlobals)
				Python.Globals.Remove(s);
			CurrentGlobals.Clear();
			
			if(Host.ActiveView != null)
			{
				foreach(KeyValuePair<string, object> kvp in CurrentDocument.MetaData)
				{
					CurrentGlobals.Add(kvp.Key);
					Python.Globals.Add(kvp.Key, kvp.Value);
				}
			}
		}
		
		protected void OnDocumentMetaDataItemChanged(object sender, MetaDataItemChangedEventArgs e)
		{
			if(Python.Globals.ContainsKey(e.Key))
				Python.Globals[e.Key] = e.Value;
			else
				Python.Globals.Add(e.Key, e.Value);
			
			if(!CurrentGlobals.Contains(e.Key))
				CurrentGlobals.Add(e.Key);
		}
		
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if(e.KeyCode == Keys.Enter)
			{
				try
				{
					string newText = Lines[Lines.Length - 2];
					CurrentExpression.Append(newText);
					CurrentExpression.Append("\n");
					if(Python.ParseInteractiveInput(CurrentExpression.ToString(), newText.Trim().Length != 0))
					{
						Python.ExecuteToConsole(CurrentExpression.ToString());
						CurrentExpression.Remove(0, CurrentExpression.Length);
					}
					else
					{
						int indent = IronPython.Compiler.Parser.GetNextAutoIndentSize(CurrentExpression.ToString(), 4);
						AppendText(String.Empty.PadLeft(indent, ' '), Font, Color.Black);
					}
				}
				catch(Exception exp)
				{
					AppendText(exp.Message + "\r\n", ErrorFont, Color.Red);
					CurrentExpression.Remove(0, CurrentExpression.Length);
				}
			}
			if(e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
			{
				Select(Text.Length + 1, 0);
			}
		}	
	}
	
	public class PythonConsolePanel : Panel
	{
		IPluginHost Host;
		PythonConsole Console;
		ToolStrip ToolBar;
		
		public PythonConsolePanel(IPluginHost host)
		{
			Host = host;
			Console = new PythonConsole(host);
			Console.Dock = DockStyle.Fill;
			Controls.Add(Console);
			
			ToolBar = new ToolStrip();
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolStripItem item = ToolBar.Items.Add(Host.Settings.Image("delete_16.png"));
			item.ToolTipText = "Clear Console";
			item.Click += OnClearConsole;
			Controls.Add(ToolBar);
		}
		
		protected void OnClearConsole(object sender, EventArgs e)
		{
			Console.Clear();
		}
	}

	public class PythonConsolePlugin : IPlugin
	{
		string IPlugin.Name { get { return "Python Console"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new PythonConsolePanel(host), "Python Console", host.Settings.Image("console_16.png"), DefaultWindowPosition.BottomRight, true);
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
