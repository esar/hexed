using System;
using System.Windows.Forms;


namespace SearchPlugin
{
	class SearchDialog : PatternDialog
	{
		RadioButton  DocumentRadio;
		RadioButton  SelectionRadio;
		CheckBox     CaseCheckBox;

		public bool SelectionOnly
		{
			get { return DocumentRadio.Checked == false; }
		}
		
		public bool CaseInsensitive
		{
			get { return CaseCheckBox.Checked = true; }
		}
		
		public SearchDialog()
		{
			Text = "Search";
			Icon = Settings.Instance.Icon("search.ico");
			
			Extras.RowCount = 2;
			Extras.ColumnCount = 2;
			Extras.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Extras.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			
			DocumentRadio = new RadioButton();
			DocumentRadio.Text = "Search whole document";
			DocumentRadio.AutoSize = true;
			DocumentRadio.Checked = true;
			Extras.Controls.Add(DocumentRadio);
			
			CaseCheckBox = new CheckBox();
			CaseCheckBox.Text = "Case Insensitive";
			CaseCheckBox.AutoSize = true;
			Extras.Controls.Add(CaseCheckBox);
			
			SelectionRadio = new RadioButton();
			SelectionRadio.Text = "Search selection only";
			SelectionRadio.AutoSize = true;
			Extras.Controls.Add(SelectionRadio);
		}		
	}
}
