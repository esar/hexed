using System;
using System.Windows.Forms;


class PatternDialog : Form
{
	HexView HexView;
	
	public byte[] Pattern
	{
		get
		{
			byte[] pattern = new byte[HexView.Document.Length];
			HexView.Document.GetBytes(0, pattern, pattern.Length);
			return pattern;
		}
	}
	
	public PatternDialog()
	{
		HexView = new HexView(new Document());
		HexView.WordsPerGroup = 1;
		HexView.OddColumnColor = System.Drawing.Color.LightGray;
		HexView.EditMode = EditMode.Insert;
		HexView.Dock = DockStyle.Fill;
		Controls.Add(HexView);
		
		Button OkButton = new Button();
		OkButton.Text = "OK";
		OkButton.Click += OnOK;
		OkButton.Dock = DockStyle.Bottom;
		Controls.Add(OkButton);
	}
	
	protected void OnOK(object sender, EventArgs e)
	{
		DialogResult = DialogResult.OK;
		Close();
	}
}
