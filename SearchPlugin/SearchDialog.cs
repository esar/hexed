/*
	This file is part of HexEd

	Copyright (C) 2008-2015  Stephen Robinson <hacks@esar.org.uk>

	HexEd is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License version 2 as 
	published by the Free Software Foundation.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

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
