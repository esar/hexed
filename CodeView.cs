using System;
using System.Drawing;
using System.Windows.Forms;


class CodeViewForm : Form
{
	public CodeViewForm()
	{
		CodeView cv = new CodeView();
		cv.Dock = DockStyle.Fill;
		Controls.Add(cv);
	}
}

class CodeView : RichTextBox
{
	public CodeView()
	{
		
	}
	
	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		if(SelectionLength == 0)
			Console.WriteLine("Inserting: " + e.KeyChar);
		else
			Console.WriteLine("Replacing: '{0}' from {1} to {2} with '{3}'", SelectedText, SelectionStart, SelectionStart + SelectionLength, e.KeyChar);
		
		base.OnKeyPress(e);
	}

	
	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		
		Console.WriteLine("change: Selected Text: '{0}', selstart: {1}", SelectedText, SelectionStart);
	}

}
