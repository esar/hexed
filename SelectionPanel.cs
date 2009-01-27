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
		
		List.View = View.Details;
		List.AllowColumnReorder = true;
		List.FullRowSelect = true;
		List.GridLines = true;
		List.Columns.Add("Type", -2, HorizontalAlignment.Left);
		List.Columns.Add("Value", -2, HorizontalAlignment.Left);

		ListViewItem i;
		i = List.Items.Add("Length");
		i.UseItemStyleForSubItems = false;
		lengthItem = i.SubItems.Add("");
		
		i = List.Items.Add("Integer");
		i.UseItemStyleForSubItems = false;
		integerItem = i.SubItems.Add("");

		i = List.Items.Add("Floating Point");
		i.UseItemStyleForSubItems = false;
		floatItem = i.SubItems.Add("");

		i = List.Items.Add("ASCII");
		i.UseItemStyleForSubItems = false;
		asciiItem = i.SubItems.Add("");

		i = List.Items.Add("Unicode");
		i.UseItemStyleForSubItems = false;
		unicodeItem = i.SubItems.Add("");

		i = List.Items.Add("UTF-8");
		i.UseItemStyleForSubItems = false;
		utf8Item = i.SubItems.Add("");

		List.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
		List.Columns[1].Width = -2;
		List.Dock = DockStyle.Fill;
		Controls.Add(List);
		
		ToolBar.GripStyle = ToolStripGripStyle.Hidden;
		ToolBar.Items.Add(Settings.Instance.Image("byteswap_16.png")).ToolTipText = "Byte Swap";
		ToolBar.Items.Add(Settings.Instance.Image("invert_16.png")).ToolTipText = "Invert";
		ToolBar.Items.Add(new ToolStripSeparator());
		ToolBar.Items.Add(Settings.Instance.Image("shiftleft_16.png")).ToolTipText = "Shift Left";
		ToolBar.Items.Add(Settings.Instance.Image("shiftright_16.png")).ToolTipText = "Shift Right";
		ToolBar.Items.Add(Settings.Instance.Image("rotateleft_16.png")).ToolTipText = "Rotate Left";
		ToolBar.Items.Add(Settings.Instance.Image("rotateright_16.png")).ToolTipText = "Rotate Right";
		ToolBar.Items.Add(new ToolStripSeparator());
		ToolBar.Items.Add("&&").ToolTipText = "AND";
		ToolBar.Items.Add("|").ToolTipText = "OR";
		ToolBar.Items.Add("^").ToolTipText = "XOR";
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
		
		Update();
	}
	
	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		Update();
	}
	
	protected void Update()
	{
		HexView view = Host.ActiveView;
		
		if(view == null)
		{
			foreach(ListViewItem i in List.Items)
				i.SubItems[1].Text = String.Empty;
			return;
		}
		
		
		lengthItem.Text = (view.Selection.Length / 8).ToString() + "." + (view.Selection.Length % 8).ToString();
		
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
