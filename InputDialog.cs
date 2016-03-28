
using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

class InputDialog
{
	public static DialogResult Show(string title, string prompt, ref string value)
	{
		DialogBase form = new DialogBase(DialogBase.Buttons.OK | DialogBase.Buttons.Cancel);
		Label promptLabel = new Label();
		TextBox textBox = new TextBox();

		form.Text = title;

		textBox.Text = value;
		textBox.Visible = true;
		textBox.Dock = DockStyle.Fill;
		form.Controls.Add(textBox);

		promptLabel.BackColor = Color.Transparent;
		promptLabel.Text = prompt;
		promptLabel.Dock = DockStyle.Top;
		form.Controls.Add(promptLabel);

		form.StartPosition = FormStartPosition.CenterParent;
		form.MinimumSize = new Size(320, 160);
		form.Size = form.MinimumSize;

		DialogResult result = form.ShowDialog();
		value = textBox.Text;
		return result;
	}
}
