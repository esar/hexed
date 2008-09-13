using System;
using System.Drawing;
using System.Windows.Forms;


class SelectionPanel : ListView
{
	private ListViewItem.ListViewSubItem	integerItem = null;
	private ListViewItem.ListViewSubItem	floatItem = null;
	private ListViewItem.ListViewSubItem	asciiItem = null;
	private ListViewItem.ListViewSubItem	unicodeItem = null;
	private ListViewItem.ListViewSubItem	utf8Item = null;

	public SelectionPanel()
	{
		View = View.Details;
		AllowColumnReorder = true;
		FullRowSelect = true;
		GridLines = true;

		Font = new Font("Courier New", 10);

		Columns.Add("Type", -2, HorizontalAlignment.Left);
		Columns.Add("Value", -2, HorizontalAlignment.Left);

		ListViewItem i;

		i = Items.Add("Integer");
		i.UseItemStyleForSubItems = false;
		integerItem = i.SubItems.Add("");

		i = Items.Add("Floating Point");
		i.UseItemStyleForSubItems = false;
		floatItem = i.SubItems.Add("");

		i = Items.Add("ASCII");
		i.UseItemStyleForSubItems = false;
		asciiItem = i.SubItems.Add("");

		i = Items.Add("Unicode");
		i.UseItemStyleForSubItems = false;
		unicodeItem = i.SubItems.Add("");

		i = Items.Add("UTF-8");
		i.UseItemStyleForSubItems = false;
		utf8Item = i.SubItems.Add("");
	}

	public void Update(HexView view)
	{
		try
		{
			integerItem.Text = view.Selection.AsInteger().ToString();
			integerItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(Exception)
		{
			integerItem.Text = "[Invalid Selection]";
			integerItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			floatItem.Text = view.Selection.AsFloat().ToString();
			floatItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(Exception)
		{
			floatItem.Text = "[Invalid Selection]";
			floatItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			asciiItem.Text = view.Selection.AsAscii();
			asciiItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(Exception)
		{
			asciiItem.Text = "[Invalid Selection]";
			asciiItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			unicodeItem.Text = view.Selection.AsUnicode();
			unicodeItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(Exception)
		{
			unicodeItem.Text = "[Invalid Selection]";
			unicodeItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			utf8Item.Text = view.Selection.AsUTF8();
			utf8Item.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(Exception)
		{
			utf8Item.Text = "[Invalid Selection]";
			utf8Item.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}
	}
}
