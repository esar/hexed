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
		
		ToolStripItem item;
		ToolBar.GripStyle = ToolStripGripStyle.Hidden;
		item = ToolBar.Items.Add(Settings.Instance.Image("byteswap_16.png"));
		item.ToolTipText = "Byte Swap";
		item.Click += OnByteSwap;
		item = ToolBar.Items.Add(Settings.Instance.Image("invert_16.png"));
		item.ToolTipText = "Invert";
		item.Click += OnInvert;
		ToolBar.Items.Add(new ToolStripSeparator());
		item = ToolBar.Items.Add(Settings.Instance.Image("shiftleft_16.png"));
		item.ToolTipText = "Shift Left";
		ToolBar.Items.Add(Settings.Instance.Image("shiftright_16.png")).ToolTipText = "Shift Right";
		ToolBar.Items.Add(Settings.Instance.Image("rotateleft_16.png")).ToolTipText = "Rotate Left";
		ToolBar.Items.Add(Settings.Instance.Image("rotateright_16.png")).ToolTipText = "Rotate Right";
		ToolBar.Items.Add(new ToolStripSeparator());
		item = ToolBar.Items.Add("&&");
		item.ToolTipText = "AND";
		item.Click += OnAnd;
		item = ToolBar.Items.Add("|");
		item.ToolTipText = "OR";
		item.Click += OnOr;
		item = ToolBar.Items.Add("^");
		item.ToolTipText = "XOR";
		item.Click += OnXor;
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
		
		Update();
	}
	
	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		Update();
	}
	
	protected void OnInvert(object sender, EventArgs e)
	{
		CurrentView.Document.Invert(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End);
	}
	
	protected void OnByteSwap(object sender, EventArgs e)
	{
		CurrentView.Document.Reverse(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End);
	}
	
	protected void OnAnd(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.And(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Pattern);
	}
	
	protected void OnOr(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.Or(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Pattern);
	}
	
	protected void OnXor(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		if(dlg.ShowDialog() == DialogResult.OK)
			CurrentView.Document.Xor(CurrentView.Selection.BufferRange.Start, CurrentView.Selection.BufferRange.End, dlg.Pattern);
	}
	
	protected void Update()
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
