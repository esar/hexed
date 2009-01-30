using System;
using System.Windows.Forms;


class PatternDialog : Form
{
	HexView HexView;
	
	public PatternDialog()
	{
		HexView = new HexView(new Document());
		HexView.WordsPerGroup = 1;
		HexView.OddColumnColor = System.Drawing.Color.LightGray;
		HexView.EditMode = EditMode.Insert;
		HexView.Dock = DockStyle.Fill;
		Controls.Add(HexView);
	}
}
