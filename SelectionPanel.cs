using System;
using System.Drawing;
using System.Windows.Forms;


class SelectionPanel : Panel
{
	protected IPluginHost Host;
	protected HexView CurrentView;
	protected ToolStrip ToolBar = new ToolStrip();
	protected ListView  List = new ListView();
	protected ListViewItem.ListViewSubItem	lengthItem = null;
	protected ListViewItem.ListViewSubItem	integerItem = null;
	protected ListViewItem.ListViewSubItem	floatItem = null;
	protected ListViewItem.ListViewSubItem	asciiItem = null;
	protected ListViewItem.ListViewSubItem	unicodeItem = null;
	protected ListViewItem.ListViewSubItem	utf8Item = null;

	public SelectionPanel(IPluginHost host)
	{
		Host = host;

		Host.Commands.Add("Selection/Byte Swap", "Reverses the bytes in the current selection", "Byte Swap",
		                  Host.Settings.Image("byteswap_16.png"),
		                  null,
		                  OnByteSwap);
		Host.Commands.Add("Selection/Invert", "Inverts the bits in the current selection", "Invert",
		                  Host.Settings.Image("invert_16.png"),
		                  null,
		                  OnInvert);
		Host.Commands.Add("Selection/Shift Left", "Shifts the selected data left by the specified number of bits", "Shift Left",
		                  Host.Settings.Image("shiftleft_16.png"),
		                  null,
		                  OnShiftLeft);
		Host.Commands.Add("Selection/Shift Right", "Shifts the selected data right by the specified number of bits", "Shift Right",
		                  Host.Settings.Image("shiftright_16.png"),
		                  null,
		                  OnShiftRight);
		Host.Commands.Add("Selection/Rotate Left", "Rotates the selected data left by the specified number of bits", "Rotate Left",
		                  Host.Settings.Image("rotateleft_16.png"),
		                  null);
		Host.Commands.Add("Selection/Rotate Right", "Rotates the selected data right by the specified number of bits", "Rotate Right",
		                  Host.Settings.Image("rotateright_16.png"),
		                  null);
		Host.Commands.Add("Selection/AND", "AND's the selected data with the specified pattern", "AND",
		                  Host.Settings.Image("and_16.png"),
		                  null,
		                  OnAnd);
		Host.Commands.Add("Selection/OR", "OR's the selected data with the specified pattern", "OR",
		                  Host.Settings.Image("or_16.png"),
		                  null,
		                  OnOr);
		Host.Commands.Add("Selection/XOR", "XOR's the selected data with the specified pattern", "XOR",
		                  Host.Settings.Image("xor_16.png"),
		                  null,
		                  OnXor);
		
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/Byte Swap"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/Invert"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", new ToolStripSeparator());
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/Shift Left"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/Shift Right"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/Rotate Left"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/Rotate Right"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", new ToolStripSeparator());
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/AND"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/OR"));
		Host.AddMenuItem(Menus.SelectedDataContext, "Selection", Host.CreateMenuItem("Selection/XOR"));
		
		List.View = View.Details;
		List.AllowColumnReorder = true;
		List.FullRowSelect = true;
		List.GridLines = true;
		List.Columns.Add("Type", -2, HorizontalAlignment.Left);
		List.Columns.Add("Value", -2, HorizontalAlignment.Left);

		ListViewItem i;
		i = List.Items.Add("Length");
		i.UseItemStyleForSubItems = false;
		lengthItem = i.SubItems.Add(String.Empty);
		
		i = List.Items.Add("Integer");
		i.UseItemStyleForSubItems = false;
		integerItem = i.SubItems.Add(String.Empty);

		i = List.Items.Add("Floating Point");
		i.UseItemStyleForSubItems = false;
		floatItem = i.SubItems.Add(String.Empty);

		i = List.Items.Add("ASCII");
		i.UseItemStyleForSubItems = false;
		asciiItem = i.SubItems.Add(String.Empty);

		i = List.Items.Add("Unicode");
		i.UseItemStyleForSubItems = false;
		unicodeItem = i.SubItems.Add(String.Empty);

		i = List.Items.Add("UTF-8");
		i.UseItemStyleForSubItems = false;
		utf8Item = i.SubItems.Add(String.Empty);

		List.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
		List.Columns[1].Width = -2;
		List.Dock = DockStyle.Fill;
		Controls.Add(List);
		
		ToolBar.GripStyle = ToolStripGripStyle.Hidden;
		ToolBar.Items.Add(Host.CreateToolButton("Selection/Byte Swap"));
		ToolBar.Items.Add(Host.CreateToolButton("Selection/Invert"));
		ToolBar.Items.Add(new ToolStripSeparator());
		ToolBar.Items.Add(Host.CreateToolButton("Selection/Shift Left"));
		ToolBar.Items.Add(Host.CreateToolButton("Selection/Shift Right"));
		ToolBar.Items.Add(Host.CreateToolButton("Selection/Rotate Left"));
		ToolBar.Items.Add(Host.CreateToolButton("Selection/Rotate Right"));
		ToolBar.Items.Add(new ToolStripSeparator());
		ToolBar.Items.Add(Host.CreateToolButton("Selection/AND"));
		ToolBar.Items.Add(Host.CreateToolButton("Selection/OR"));
		ToolBar.Items.Add(Host.CreateToolButton("Selection/XOR"));
		foreach(ToolStripItem x in ToolBar.Items)
			x.Enabled = false;
		Controls.Add(ToolBar);
		
		
		Host.ActiveViewChanged += OnActiveViewChanged;
	}

	protected void OnActiveViewChanged(object sender, EventArgs e)
	{
		if(CurrentView != null)
			CurrentView.Selection.Changed -= OnSelectionChanged;
		CurrentView = Host.ActiveView;
		if(CurrentView != null)
			CurrentView.Selection.Changed += OnSelectionChanged;
		
		UpdateFields();
	}
	
	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		UpdateFields();
	}
	
	protected void OnInvert(object sender, EventArgs e)
	{
		CurrentView.Document.Invert(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End);
	}
	
	protected void OnByteSwap(object sender, EventArgs e)
	{
		CurrentView.Document.Reverse(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End);
	}
	
	protected void OnShiftLeft(object sender, EventArgs e)
	{
		ShiftDialog dlg = new ShiftDialog();
		dlg.StartPosition = FormStartPosition.CenterParent;
		dlg.Text = "Shift Left";
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.Shift(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, 0 - dlg.Value);
	}
	
	protected void OnShiftRight(object sender, EventArgs e)
	{
		ShiftDialog dlg = new ShiftDialog();
		dlg.StartPosition = FormStartPosition.CenterParent;
		dlg.Text = "Shift Right";
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.Shift(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Value);
	}
	
	protected void OnAnd(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		dlg.Text = "AND Pattern";
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.And(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Pattern);
	}
	
	protected void OnOr(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		dlg.Text = "OR Pattern";
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.Or(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Pattern);
	}
	
	protected void OnXor(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		dlg.Text = "XOR Pattern";
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.Xor(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Pattern);
	}
	
	protected void UpdateFields()
	{
		HexView view = Host.ActiveView;

		if(view == null || view.Selection.BufferRange.Length == 0)
		{
			foreach(ToolStripItem i in ToolBar.Items)
				i.Enabled = false;
		}
		else
		{
			foreach(ToolStripItem i in ToolBar.Items)
				i.Enabled = true;
		}
		
		if(view == null)
		{
			foreach(ListViewItem i in List.Items)
				i.SubItems[1].Text = String.Empty;
			return;
		}
		
		
		lengthItem.Text = (view.Selection.Length / 8).ToString() + "." + (view.Selection.Length % 8).ToString();
		
		try
		{
			integerItem.Text = view.IntToRadixString((ulong)view.Selection.AsInteger(), view.DataRadix, 1);
			integerItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(InvalidCastException)
		{
			integerItem.Text = "[Invalid Selection]";
			integerItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			floatItem.Text = view.Selection.AsFloat().ToString();
			floatItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(InvalidCastException)
		{
			floatItem.Text = "[Invalid Selection]";
			floatItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			asciiItem.Text = view.Selection.AsAscii();
			asciiItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(InvalidCastException)
		{
			asciiItem.Text = "[Invalid Selection]";
			asciiItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

#if !MONO
		try
		{
			unicodeItem.Text = view.Selection.AsUnicode();
			unicodeItem.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(InvalidCastException)
		{
			unicodeItem.Text = "[Invalid Selection]";
			unicodeItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}

		try
		{
			utf8Item.Text = view.Selection.AsUTF8();
			utf8Item.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
		}
		catch(InvalidCastException)
		{
			utf8Item.Text = "[Invalid Selection]";
			utf8Item.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		}
#else
		unicodeItem.Text = "[Disabled due to mono bug]";
		unicodeItem.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
		utf8Item.Text = "[Disabled due to mono bug]";
		utf8Item.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
#endif
	}
}
