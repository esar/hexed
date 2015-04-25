using System;
using System.Windows.Forms;
using System.CodeDom.Compiler;



class CompileErrorDialog : DialogBase
{
	protected RichTextBox TextBox = new RichTextBox();
	
	public CompilerErrorCollection Errors
	{
		set
		{
			foreach(CompilerError err in value)
			{
				TextBox.AppendText(String.Format("({0},{1}): {2}: {3}: {4}\r\n", err.Line, err.Column, err.IsWarning ? "Warning" : "Error", err.ErrorNumber, err.ErrorText));
			}
		}
	}
	
	public CompileErrorDialog() : base(DialogBase.Buttons.OK)
	{
		Text = "Compile Error";
		
		TextBox.Dock = DockStyle.Fill;
		Controls.Add(TextBox);
		
		Size = new System.Drawing.Size(480, 320);
	}
}
