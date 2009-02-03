using System;
using System.Windows.Forms;


class PluginInfoDialog : Form
{
	Label NameLabel = new Label();
	
	public PluginInfoDialog(IPlugin plugin)
	{
		NameLabel.Text = plugin.Name;
		NameLabel.Dock = DockStyle.Fill;
		Controls.Add(NameLabel);
	}
}

class PluginsSettingsPage : SettingsPage
{
	ListView List = new ListView();

	FlowLayoutPanel ButtonPanel = new FlowLayoutPanel();
	Button EnableButton = new Button();
	Button DisableButton = new Button();
	Button InfoButton = new Button();
	
	public PluginsSettingsPage(HexEdApp app)
	{
		Text = "Plugins";
		
		Padding = new Padding(0, 5, 0, 0);
		
		List.View = View.Details;
		List.Columns.Add("Name");
		List.Columns.Add("Version");
		List.Columns.Add("State");
		List.Dock = DockStyle.Fill;
		List.SmallImageList = new ImageList();
		List.SmallImageList.ColorDepth = ColorDepth.Depth24Bit;
		List.SmallImageList.ImageSize = new System.Drawing.Size(15, 15);
		List.SmallImageList.TransparentColor = System.Drawing.Color.Magenta;
		List.SmallImageList.Images.Add(Settings.Instance.Image("enabled_16.bmp"));
		List.SmallImageList.Images.Add(Settings.Instance.Image("disabled_16.bmp"));
		foreach(IPlugin plugin in app.PluginManager.ActivePlugins.Values)
		{
			ListViewItem item = List.Items.Add(plugin.Name);
			item.Tag = plugin;
			item.ImageIndex = 0;
			item.SubItems.Add(plugin.Version);
			item.SubItems.Add("Enabled");
		}
		List.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		List.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
		List.Columns[2].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		Controls.Add(List);
		
		ButtonPanel.FlowDirection = FlowDirection.TopDown;
		EnableButton.Text = "Enable";
		EnableButton.Enabled = false;
		ButtonPanel.Controls.Add(EnableButton);
		DisableButton.Text = "Disable";
		DisableButton.Enabled = false;
		ButtonPanel.Controls.Add(DisableButton);
		InfoButton.Text = "Information";
		InfoButton.Enabled = false;
		ButtonPanel.Controls.Add(InfoButton);
		ButtonPanel.AutoSize = true;
		ButtonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		
		ButtonPanel.Dock = DockStyle.Right;
		Controls.Add(ButtonPanel);
		
		List.SelectedIndexChanged += OnListSelectedIndexChanged;
		InfoButton.Click += OnInfo;
	}
	
	protected void OnListSelectedIndexChanged(object sender, EventArgs e)
	{
		if(List.SelectedIndices.Count == 1)
		{
			DisableButton.Enabled = true;
			InfoButton.Enabled = true;
		}
		else
		{
			EnableButton.Enabled = false;
			DisableButton.Enabled = false;
			InfoButton.Enabled = false;
		}
	}
	
	protected void OnInfo(object sender, EventArgs e)
	{
		PluginInfoDialog dlg = new PluginInfoDialog((IPlugin)List.SelectedItems[0].Tag);
		dlg.ShowDialog();
	}
}
