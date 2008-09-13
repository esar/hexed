using System;
using System.Windows.Forms;


class AboutDialog : Form
{
	public AboutDialog()
	{
		Label label = new Label();

		label.Text =	"Performance Viewer\n" +
						"\n" +
						"0.04  -  Fixed exception when counter goes away\n" +
						"0.03  -  Added auto reduce to Y scale\n" +
						"            Added reset toolbar button\n" +
						"0.02  -  Fixed '% disk...' counters\n" +
						"0.01  -  Initial release";
		label.Dock = DockStyle.Fill;

		Controls.Add(label);
	}
}
