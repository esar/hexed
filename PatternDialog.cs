using System;
using System.Windows.Forms;


class PatternDialog : DialogBase
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
		Panel panel = new Panel();
		panel.BorderStyle = BorderStyle.Fixed3D;
		panel.Dock = DockStyle.Fill;
		panel.Controls.Add(HexView);
		Controls.Add(panel);
	}
	
	protected void OnOK(object sender, EventArgs e)
	{
		DialogResult = DialogResult.OK;
		Close();
	}
}
