using System;
using System.Windows.Forms;


namespace SearchPlugin
{
	class SearchDialog : DialogBase
	{
		TextBox		PatternTextBox = new TextBox();

		public string Pattern
		{
			get { return PatternTextBox.Text; }
		}
		
		public SearchDialog()
		{
			Text = "Search";
			PatternTextBox.Dock = DockStyle.Top;
			Controls.Add(PatternTextBox);
		}		
	}
}
