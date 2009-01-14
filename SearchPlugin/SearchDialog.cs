using System;
using System.Windows.Forms;


namespace SearchPlugin
{
	class SearchDialog : Form
	{
		TextBox		PatternTextBox = new TextBox();
		Panel		ButtonPanel = new Panel();
		Button		OkButton = new Button();
		Button		CancelButton = new Button();

		public string Pattern
		{
			get { return PatternTextBox.Text; }
		}
		
		public SearchDialog()
		{
			OkButton.Text = "OK";
			OkButton.Dock = DockStyle.Right;
			CancelButton.Text = "Cancel";
			CancelButton.Dock = DockStyle.Right;
			ButtonPanel.Controls.Add(CancelButton);
			ButtonPanel.Controls.Add(OkButton);
			ButtonPanel.Dock = DockStyle.Bottom;
			Controls.Add(ButtonPanel);
			PatternTextBox.Dock = DockStyle.Top;
			Controls.Add(PatternTextBox);
			
			OkButton.Click += OnOK;
			CancelButton.Click += OnCancel;
		}
		
		protected void OnOK(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}
		
		protected void OnCancel(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
