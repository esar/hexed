using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;


class ShortcutKeyDialog : DialogBase
{
	protected Label Label = new Label();
	protected TextBox TextBox = new TextBox();
	
	protected Keys _Keys = Keys.None;
	public Keys Keys { get { return _Keys; } }
	
	public ShortcutKeyDialog() : base(DialogBase.Buttons.OK | DialogBase.Buttons.Cancel)
	{
		TextBox.Dock = DockStyle.Top;
		Controls.Add(TextBox);
		Label.Text = "Press the keys for the new shortcut:";
		Label.BackColor = Color.Transparent; 
		Label.Dock = DockStyle.Top;
		Controls.Add(Label);
		
		TextBox.KeyDown += OnTextBoxKeyDown;
		TextBox.KeyPress += OnTextBoxKeyPress;
		TextBox.KeyUp += OnTextBoxKeyUp;
		
		Size = new System.Drawing.Size(320, 160);
	}
	
	protected void OnTextBoxKeyDown(object sender, KeyEventArgs e)
	{
		_Keys = e.KeyData;
		TextBox.Text = ShortcutsSettingsPage.ShortcutLabel(e.KeyData);
		e.Handled = true;
	}
	
	protected void OnTextBoxKeyPress(object sender, KeyPressEventArgs e)
	{
		e.Handled = true;
	}
	
	protected void OnTextBoxKeyUp(object sender, KeyEventArgs e)
	{
		e.Handled = true;
	}
}

class ShortcutsSettingsPage : SettingsPage
{
	protected ListView List = new ListView();
	protected FlowLayoutPanel ButtonPanel = new FlowLayoutPanel();
	protected Button ChangeButton = new Button();
	
	public ShortcutsSettingsPage(HexEdApp app)
	{
		Text = "Shortcuts";
		
		Padding = new Padding(0, 5, 0, 0);
		
		List.View = View.Details;
		List.Columns.Add("Name");
		List.Columns.Add("Shortcuts");
		List.Columns.Add("Description");
		List.SmallImageList = new ImageList();
		List.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
		
		SortedDictionary<string, Command> commands = new SortedDictionary<string, Command>();
		foreach(KeyValuePair<string, Command> kvp in HexEdApp.Instance.Commands)
			commands.Add(kvp.Key, kvp.Value);
		
		int imgIndex = 0;
		int groupIndex = -1;
		string lastGroup = String.Empty;
		foreach(KeyValuePair<string, Command> kvp in commands)
		{
			string[] nameParts = kvp.Key.Split(new char[] {'/'}, 2);
			
			
			if(nameParts[0] != lastGroup)
			{
				List.Groups.Add(new ListViewGroup(nameParts[0]));
				lastGroup = nameParts[0];
				groupIndex++;
			}
				                
			ListViewItem i = List.Items.Add(nameParts[1]);
			i.Group = List.Groups[groupIndex];
			System.Text.StringBuilder shortcutText = new System.Text.StringBuilder();
			if(kvp.Value.Shortcuts != null)
			{
				for(int x = 0; x < kvp.Value.Shortcuts.Length; ++x)
				{
					if(x != 0)
						shortcutText.Append(", ");
					shortcutText.Append(ShortcutLabel(kvp.Value.Shortcuts[x]));
				}
			}
			i.SubItems.Add(shortcutText.ToString());
			i.SubItems.Add(kvp.Value.Description);
			if(kvp.Value.Image != null)
			{
				List.SmallImageList.Images.Add(kvp.Value.Image);
				i.ImageIndex = imgIndex++;
			}
		}
		
		List.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		List.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		List.Columns[2].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		
		List.Dock = DockStyle.Fill;
		Controls.Add(List);
		
		
		ChangeButton.Text = "Edit";
		ChangeButton.Margin = new Padding(5, 0, 0, 0);
		ChangeButton.Enabled = false;
		ButtonPanel.Controls.Add(ChangeButton);
		ButtonPanel.FlowDirection = FlowDirection.TopDown;
		ButtonPanel.AutoSize = true;
		ButtonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		ButtonPanel.Dock = DockStyle.Right;
		Controls.Add(ButtonPanel);
		
		List.SelectedIndexChanged += OnListSelectedIndexChanged;
		ChangeButton.Click += OnEditShortcut;
	}
	
	protected void OnListSelectedIndexChanged(object sender, EventArgs e)
	{
		if(List.SelectedIndices.Count > 0)
			ChangeButton.Enabled = true;
		else
			ChangeButton.Enabled = false;
	}
	
	protected void OnEditShortcut(object sender, EventArgs e)
	{
		ShortcutKeyDialog dlg = new ShortcutKeyDialog();
		dlg.StartPosition = FormStartPosition.CenterParent;
		dlg.ShowDialog();
	}
	
	public static string ShortcutLabel(Keys keys)
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		// Modifiers first
		if((keys & Keys.Control) != 0)
			sb.Append("Ctrl+");
		if((keys & Keys.Alt) != 0)
			sb.Append("Alt+");
		if((keys & Keys.Shift) != 0)
			sb.Append("Shift+");

		if((keys & Keys.KeyCode) != Keys.None && (keys & Keys.KeyCode) != Keys.ControlKey && (keys & Keys.KeyCode) != Keys.Menu)
			sb.Append(Enum.GetName(typeof(Keys), keys & Keys.KeyCode));
		return sb.ToString();
	}
}
