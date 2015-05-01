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
using System.Drawing;
using System.Windows.Forms;


class PluginInfoDialog : DialogBase
{
	PictureBox Icon = new PictureBox();
	Label NameLabel = new Label();
	Label DescriptionLabel = new Label();
	Label VersionLabel = new Label();
	Label VersionValue = new Label();
	Label AuthorLabel = new Label();
	Label AuthorValue = new Label();
	Label CopyrightLabel = new Label();
	Label CopyrightValue = new Label();
	Label UrlLabel = new Label();
	LinkLabel UrlValue = new LinkLabel();
	
	TableLayoutPanel Panel = new TableLayoutPanel();
	
	public PluginInfoDialog(IPlugin plugin) : base(DialogBase.Buttons.OK)
	{
		Font boldFont = new Font(NameLabel.Font, FontStyle.Bold);

		Text = "Plugin Info";
		
		Panel.BackColor = Color.Transparent;
		Panel.RowCount = 10;
		Panel.ColumnCount = 2;
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		Panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		Panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
		
		Icon.Image = plugin.Image;
		Icon.AutoSize = true;
		Panel.Controls.Add(Icon);
		Panel.SetRowSpan(Icon, 10);
		
		NameLabel.Font = new Font(NameLabel.Font.FontFamily.Name, 12, FontStyle.Bold);
		NameLabel.Text = plugin.Name;
		NameLabel.AutoSize = true;
		Panel.Controls.Add(NameLabel);
		
		DescriptionLabel.Text = plugin.Description;
		DescriptionLabel.Dock = DockStyle.Fill;
		Panel.Controls.Add(DescriptionLabel);

		VersionLabel.Font = boldFont;
		VersionLabel.Text = "Version:";
		VersionLabel.AutoSize = true;
		Panel.Controls.Add(VersionLabel);
		
		VersionValue.Text = plugin.Version;
		VersionValue.AutoSize = true;
		Panel.Controls.Add(VersionValue);

		AuthorLabel.Padding = new Padding(0, 5, 0, 0);
		AuthorLabel.Font = boldFont;
		AuthorLabel.Text = "Author:";
		AuthorLabel.AutoSize = true;
		Panel.Controls.Add(AuthorLabel);
		
		AuthorValue.Text = plugin.Author;
		AuthorValue.AutoSize = true;
		Panel.Controls.Add(AuthorValue);
		
		CopyrightLabel.Padding = new Padding(0, 5, 0, 0);
		CopyrightLabel.Font = boldFont;
		CopyrightLabel.Text = "Copyright:";
		CopyrightLabel.AutoSize = true;
		Panel.Controls.Add(CopyrightLabel);
		
		CopyrightValue.Text = plugin.Copyright;
		CopyrightValue.AutoSize = true;
		Panel.Controls.Add(CopyrightValue);

		UrlLabel.Padding = new Padding(0, 5, 0, 0);
		UrlLabel.Font = boldFont;
		UrlLabel.Text = "URL:";
		UrlLabel.AutoSize = true;
		Panel.Controls.Add(UrlLabel);
		
		UrlValue.Text = plugin.Url;
		UrlValue.Links.Add(0, plugin.Url.Length, plugin.Url);
		UrlValue.AutoSize = true;
		UrlValue.LinkClicked += OnLinkClicked;
		Panel.Controls.Add(UrlValue);
		
		Panel.Dock = DockStyle.Fill;
		Controls.Add(Panel);
		
		Size = new Size(320, 320);
	}
	
	protected void OnLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
		e.Link.Visited = true;
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
		List.SmallImageList.Images.Add(Settings.Instance.Image("icons.enabled_16.bmp"));
		List.SmallImageList.Images.Add(Settings.Instance.Image("icons.disabled_16.bmp"));
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
		EnableButton.Margin = new Padding(5, 0, 0, 0);
		ButtonPanel.Controls.Add(EnableButton);
		DisableButton.Text = "Disable";
		DisableButton.Enabled = false;
		DisableButton.Margin = new Padding(5, 5, 0, 0);
		ButtonPanel.Controls.Add(DisableButton);
		InfoButton.Text = "Information";
		InfoButton.Enabled = false;
		InfoButton.Margin = new Padding(5, 5, 0, 0);
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
		dlg.StartPosition = FormStartPosition.CenterParent;
		dlg.ShowDialog();
	}
}
