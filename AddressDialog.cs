using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;


class AddressDialog : Form
{
	public long Address;

	private TextBox		textBox = new TextBox();
	private Label		label = new Label();
	private Button		okButton = new Button();
	private Button		cancelButton = new Button();
	private Panel		buttonPanel = new Panel();

	public AddressDialog()
	{
		okButton.Text = "OK";
		okButton.Click += new EventHandler(OnOK);
		okButton.Dock = DockStyle.Right;
		cancelButton.Text = "Cancel";
		cancelButton.Click += new EventHandler(OnCancel);
		cancelButton.Dock = DockStyle.Right;
		buttonPanel.Controls.Add(okButton);
		buttonPanel.Controls.Add(cancelButton);
		

		textBox.Text = "0";
		textBox.Dock = DockStyle.Fill;
		textBox.Visible = true;
		textBox.Multiline = true;
		Controls.Add(textBox);

		buttonPanel.Height = 25;
		buttonPanel.Dock = DockStyle.Bottom;
		Controls.Add(buttonPanel);

		label.Text = "Enter address:";
		label.Dock = DockStyle.Top;
		Controls.Add(label);

		textBox.Focus();

		MaximumSize = new Size(200, 100);
		MinimumSize = new Size(200, 100);

		
	}

	private void OnOK(object sender, EventArgs e)
	{
		Address = Convert.ToInt64(textBox.Text);
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnCancel(object sender, EventArgs e)
	{
		Address = -1;
		DialogResult = DialogResult.Cancel;
		Close();
	}
}
