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

		public void ExecuteFile(string filename)
		{
			PieceBuffer.HistoryItem historyGroup = null;

			if(CurrentDocument != null)
				historyGroup = CurrentDocument.BeginHistoryGroup("Python: " + filename);

			try
			{
				Python.ExecuteFile(filename);
			}
			catch(Exception exp)
			{
				AppendText(exp.Message + "\r\n", ErrorFont, Color.Red);
				CurrentExpression.Remove(0, CurrentExpression.Length);
			}

			if(CurrentDocument != null && historyGroup != null)
				CurrentDocument.EndHistoryGroup(historyGroup);
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
						PieceBuffer.HistoryItem historyGroup = null;

						if(CurrentDocument != null)
							historyGroup = CurrentDocument.BeginHistoryGroup("Python: Interactive");

						try
						{
							Python.ExecuteToConsole(CurrentExpression.ToString());
							CurrentExpression.Remove(0, CurrentExpression.Length);
						}
						finally
						{
							if(CurrentDocument != null && historyGroup != null)
								CurrentDocument.EndHistoryGroup(historyGroup);
						}
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

			Host.Commands.Add("Python Console/Clear Console", "Clears the contents of the console", "Clear Console",
			                  Host.Settings.Image("icons.delete_16.png"),
			                  null,
			                  OnClearConsole);
			Host.Commands.Add("Python Console/Run Script", "Runs a python script from a file", "Run Script",
			                  Host.Settings.Image("icons.open_16.png"),
			                  null,
			                  OnRunScript);
			
			Console = new PythonConsole(host);
			Console.Dock = DockStyle.Fill;
			Controls.Add(Console);
			
			ToolBar = new ToolStrip();
			ToolBar.GripStyle = ToolStripGripStyle.Hidden;
			ToolBar.Items.Add(Host.CreateToolButton("Python Console/Clear Console"));
			ToolBar.Items.Add(Host.CreateToolButton("Python Console/Run Script"));
			Controls.Add(ToolBar);
		}
		
		protected void OnClearConsole(object sender, EventArgs e)
		{
			Console.Clear();
		}

		protected void OnRunScript(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select File";
			ofd.Filter = "All Files (*.*)|*.*";

			if(ofd.ShowDialog() == DialogResult.OK)
				Console.ExecuteFile(ofd.FileName);
		}
	}

	public class PythonConsolePlugin : IPlugin
	{
		public Image Image { get { return Settings.Instance.Image("icons.console_16.png"); } }
		public string Name { get { return "Python Console"; } }
		public string Description { get { return "Provides an interactive python console"; } }
		public string Author { get { return "Stephen Robinson"; } }
		public string Version { get { return "1.0"; } }
		public string Copyright { get { return "(c)2008 Stephen Robinson"; } }
		public string Url { get { return "http://www.esar.org.uk/"; } }

		public void Initialize(IPluginHost host)
		{
			host.AddWindow(new PythonConsolePanel(host), "Python Console", host.Settings.Image("icons.console_16.png"), DefaultWindowPosition.BottomRight, true);
		}
		
		public void Dispose()
		{
		}
	}
}
