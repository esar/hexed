using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Crownwood.DotNetMagic.Docking;
using Crownwood.DotNetMagic.Common;


public class CommandEventArgs : EventArgs
{
	public object Arg = 0;

	public CommandEventArgs(object arg)
	{
		Arg = arg;
	}
}
public delegate void CommandEventHandler(object sender, CommandEventArgs e);

class CommandSet
{
	public class Command
	{
		public string _Name;
		public int _Enabled = -1;
		public int _Checked = -1;
		
		public EventHandler StateChanged;
		public CommandEventHandler Invoked;
		
		public string Name
		{
			get { return _Name; }
		}
		
		public bool Enabled
		{
			get { return (_Enabled != 0); }
			set 
			{ 
				if(value != Enabled || _Enabled == -1)
				{
					_Enabled = value ? 1 : 0;
					if(StateChanged != null) 
						StateChanged(this, new EventArgs());
				}
			}
		}
		
		public bool Checked
		{
			get { return (_Checked > 0); }
			set 
			{ 
				if(value != Checked || _Enabled == -1)
				{
					_Checked = value ? 1 : 0;
					if(StateChanged != null) 
						StateChanged(this, new EventArgs()); 
				}
			}
		}
		
		public Command(string name)
		{
			_Name = name;
		}
		
		public void Invoke(object arg)
		{
			if(Invoked != null)
				Invoked(this, new CommandEventArgs(arg));
		}
	}
	
	private Dictionary<string, Command> Commands = new Dictionary<string, Command>();
	private Dictionary<string, Command> MergedCommands;
	
	public Command this[string name]
	{
		get
		{
			Command c = null;
			if(MergedCommands != null)
				MergedCommands.TryGetValue(name, out c);
			else
				Commands.TryGetValue(name, out c);
			return c;
		}
	}
	
	public Command Add(string name)
	{
		Command c;
		Commands.TryGetValue(name, out c);
		if(c == null)
		{
			c = new Command(name);
			Commands.Add(name, c);
		}
		return c;
	}
	
	public Command Add(string name, CommandEventHandler invokeHandler)
	{
		Command c = Add(name);
		c.Invoked += invokeHandler;
		return c;
	}
	
	public Command Add(string name, CommandEventHandler invokeHandler, EventHandler stateChangeHandler)
	{
		Command c = Add(name);
		if(invokeHandler != null)
			c.Invoked += invokeHandler;
		if(stateChangeHandler != null)
			c.StateChanged += stateChangeHandler;
		return c;
	}
	
	public void Merge(CommandSet setToMerge)
	{
		MergedCommands = new Dictionary<string, Command>();
		foreach(KeyValuePair<string, Command> kvp in Commands)
		{
			Command command = kvp.Value;
			Command newCommand = new Command(kvp.Key);
		
			if(command.StateChanged != null)
				foreach(Delegate d in command.StateChanged.GetInvocationList())
					newCommand.StateChanged += (EventHandler)d;
			if(command.Invoked != null)
				foreach(Delegate d in command.Invoked.GetInvocationList())
					newCommand.Invoked += (CommandEventHandler)d;
			
			MergedCommands.Add(kvp.Key, newCommand);
		}
		
		foreach(KeyValuePair<string, Command> kvp in setToMerge.Commands)
		{
			Command command = kvp.Value;
			Command oldCommand = null;
			Command newCommand;
			
			MergedCommands.TryGetValue(kvp.Key, out oldCommand);
			if(oldCommand != null)
				newCommand = oldCommand;
			else
				newCommand = new Command(kvp.Key);

			if(command.Invoked != null)
				foreach(Delegate d in command.Invoked.GetInvocationList())
					newCommand.Invoked += (CommandEventHandler)d;
			if(command.StateChanged != null)
				foreach(Delegate d in command.StateChanged.GetInvocationList())
					newCommand.StateChanged += (EventHandler)d;
			
			if(oldCommand == null)
				MergedCommands.Add(kvp.Key, newCommand);
		}
	}
	
	public void RevertMerge()
	{
		foreach(KeyValuePair<string, Command> kvp in MergedCommands)
		{
			Command cmd = null;
			Commands.TryGetValue(kvp.Key, out cmd);
			if(cmd != null)
			{
				cmd.Enabled = kvp.Value.Enabled;
				cmd.Checked = kvp.Value.Checked;
			}
		}
		MergedCommands = null;
	}
}

class BookmarkPanel : TreeView
{
	public BookmarkPanel()
	{
		Nodes.Add("First bad node");
	}
}

class RadixMenu : ToolStripMenuItem
{
	private ToolStripMenuItem	BinaryItem;
	private ToolStripMenuItem	OctalItem;
	private ToolStripMenuItem	DecimalItem;
	private ToolStripMenuItem	HexItem;
	private ToolStripMenuItem[]	RadixItems = new ToolStripMenuItem[37];
	private EventHandler		Handler;

	public RadixMenu(string text, string name, EventHandler handler) : base(text, null, null, name)
	{
		BinaryItem = new ToolStripMenuItem("&Binary", null, OnSelectItem, name);
		BinaryItem.Tag = 2;
		DropDownItems.Add(BinaryItem);
		OctalItem = new ToolStripMenuItem("&Octal", null, OnSelectItem, name);
		OctalItem.Tag = 8;
		DropDownItems.Add(OctalItem);
		DecimalItem = new ToolStripMenuItem("&Decimal", null, OnSelectItem, name);
		DecimalItem.Tag = 10;
		DropDownItems.Add(DecimalItem);
		HexItem = new ToolStripMenuItem("&Hexadecimal", null, OnSelectItem, name);
		HexItem.Tag = 16;
		DropDownItems.Add(HexItem);
		
		DropDownItems.Add("-");

		for(int i = 2; i <= 36; ++i)
		{
			RadixItems[i] = new ToolStripMenuItem(i.ToString(), null, OnSelectItem, name);
			RadixItems[i].Tag = i;
			DropDownItems.Add(RadixItems[i]);
		}
		
		Handler = handler;
	}

	public int SelectedRadix
	{
		get
		{
			for(int i = 2; i <= 36; ++i)
				if(RadixItems[i].Checked)
					return i;

			return 0;
		}

		set
		{
			ToolStripMenuItem selectedItem = null;
			
			for(int i = 2; i <= 36; ++i)
			{
				if(i == value)
				{
					RadixItems[i].Checked = true;
					selectedItem = RadixItems[i];
				}
				else
					RadixItems[i].Checked = false;
			}

			BinaryItem.Checked = false;
			OctalItem.Checked = false;
			DecimalItem.Checked = false;
			HexItem.Checked = false;

			switch(value)
			{
				case 2:
					BinaryItem.Checked = true;
					break;
				case 8:
					OctalItem.Checked = true;
					break;
				case 10:
					DecimalItem.Checked = true;
					break;
				case 16:
					HexItem.Checked = true;
					break;
			}

			if(selectedItem != null)	
				Handler(selectedItem, new EventArgs());
		}
	}
	
	private void OnSelectItem(object sender, EventArgs args)
	{
		ToolStripMenuItem selectedItem = null;
		
		if(sender == BinaryItem)
			SelectedRadix = 2;
		else if(sender == OctalItem)
			SelectedRadix = 8;
		else if(sender == DecimalItem)
			SelectedRadix = 10;
		else if(sender == HexItem)
			SelectedRadix = 16;
		else
		{
			for(int i = 2; i <= 36; ++i)
			{
				if(sender == RadixItems[i])
				{
					SelectedRadix = i;
					selectedItem = RadixItems[i];
					break;
				}
			}
		}
		
		if(selectedItem != null)
			Handler(selectedItem, new EventArgs());
	}
}

class HexEdApp : Form, IPluginHost
{
	private DockingManager		_dockingManager = null;
	private ToolStripPanel		ToolStripPanel;
	private ToolStrip			FileToolStrip = new ToolStrip();
	private ToolStrip			EditToolStrip = new ToolStrip();
	private ToolStrip			ViewToolStrip = new ToolStrip();
	private StatusStrip			StatusBar = new StatusStrip();
	private SelectionPanel		selectionPanel = new SelectionPanel();
	private StructurePanel		structurePanel;
	private BookmarkPanel		bookmarkPanel = new BookmarkPanel();

	private static CommandSet	Commands = new CommandSet();
	
	protected ContextMenuStrip	SelectionContextMenu	= new ContextMenuStrip();

	
	[STAThread]
	public static void Main()
	{
		Application.EnableVisualStyles();
		Application.DoEvents();
		Application.Run(new HexEdApp());
	}

	public HexEdApp()
	{
		IsMdiContainer = true;

		structurePanel = new StructurePanel(this);
		_dockingManager = new DockingManager(this, VisualStyle.Office2003);
		_dockingManager.Contents.Add(selectionPanel, "Selection");
		_dockingManager.Contents.Add(structurePanel, "Structure");
		_dockingManager.Contents.Add(bookmarkPanel, "Bookmarks");
		_dockingManager.AddContentWithState(_dockingManager.Contents["Selection"], State.DockBottom);
		WindowContent wc = _dockingManager.AddContentWithState(_dockingManager.Contents["Structure"], State.DockLeft);
		_dockingManager.AddContentToWindowContent(_dockingManager.Contents["Bookmarks"], wc);
		_dockingManager.ShowContent(_dockingManager.Contents["Selection"]);
		_dockingManager.ShowContent(_dockingManager.Contents["Structure"]);

		
		Commands.Add("FileNew", null, OnUpdateUiElement);
		Commands.Add("FileOpen", OnFileOpen, OnUpdateUiElement);
		Commands.Add("FileSave", null, OnUpdateUiElement);
		Commands.Add("FileSaveAs", null, OnUpdateUiElement);
		Commands.Add("FileSaveAll", null, OnUpdateUiElement);
		Commands.Add("FilePrintSetup", null, OnUpdateUiElement);
		Commands.Add("FilePrintPreview", null, OnUpdateUiElement);
		Commands.Add("FilePrint", null, OnUpdateUiElement);
		Commands.Add("FileExit", OnFileExit, OnUpdateUiElement);
		
		Commands.Add("EditUndo", null, OnUpdateUiElement);
		Commands.Add("EditRedo", null, OnUpdateUiElement);
		Commands.Add("EditCut", null, OnUpdateUiElement);
		Commands.Add("EditCopy", null, OnUpdateUiElement);
		Commands.Add("EditPaste", null, OnUpdateUiElement);
		Commands.Add("EditDelete", null, OnUpdateUiElement);
		Commands.Add("EditOptions", null, OnUpdateUiElement);
		
		Commands.Add("ViewAddressRadix", null, OnUpdateUiElement);
		Commands.Add("ViewDataRadix", null, OnUpdateUiElement);
		Commands.Add("ViewGoTo", null, OnUpdateUiElement);
		Commands.Add("ViewGoToTop", null, OnUpdateUiElement);
		Commands.Add("ViewGoToBottom", null, OnUpdateUiElement);
		Commands.Add("ViewGoToSelectionStart", null, OnUpdateUiElement);
		Commands.Add("ViewGoToSelectionEnd", null, OnUpdateUiElement);
		Commands.Add("ViewGoToAddress", null, OnUpdateUiElement);
		Commands.Add("ViewGoToSelectionAsAddress", null, OnUpdateUiElement);
		Commands.Add("ViewBytes", null, OnUpdateUiElement);
		Commands.Add("ViewWords", null, OnUpdateUiElement);
		Commands.Add("ViewDwords", null, OnUpdateUiElement);
		Commands.Add("ViewQwords", null, OnUpdateUiElement);
		Commands.Add("ViewLittleEndian", null, OnUpdateUiElement);
		Commands.Add("ViewBigEndian", null, OnUpdateUiElement);
		
		Commands.Add("WindowSplit", OnWindowSplit, OnUpdateUiElement);
		Commands.Add("WindowDuplicate", OnWindowDuplicate, OnUpdateUiElement);
		Commands.Add("WindowTileHorizontally", OnWindowTileHorizontally, OnUpdateUiElement);
		Commands.Add("WindowTileVertically", OnWindowTileVertically, OnUpdateUiElement);

		Commands.Add("HelpAbout", OnHelpAbout, OnUpdateUiElement);
		
		ToolStripPanel = new ToolStripPanel();
		MainMenuStrip = new MenuStrip();
		ToolStripPanel.Controls.Add(MainMenuStrip);
		ToolStripPanel.Dock = DockStyle.Top;
		Controls.Add(ToolStripPanel);
		
		FileToolStrip.Items.Add(CreateToolButton(null, "new_16.png", "FileNew", "New"));
		FileToolStrip.Items.Add(CreateToolButton(null, "open_16.png", "FileOpen", "Open"));
		FileToolStrip.Items.Add(new ToolStripSeparator());
		FileToolStrip.Items.Add(CreateToolButton(null, "save_16.png", "FileSave", "Save"));
		FileToolStrip.Items.Add(CreateToolButton(null, "saveall_16.png", "FileSaveAll", "Save All"));
		
		EditToolStrip.Items.Add(CreateToolButton(null, "undo_16.png", "EditUndo", "Undo"));
		EditToolStrip.Items.Add(CreateToolButton(null, "redo_16.png", "EditRedo", "Redo"));
		EditToolStrip.Items.Add(new ToolStripSeparator());
		EditToolStrip.Items.Add(CreateToolButton(null, "cut_16.png", "EditCut", "Cut"));
		EditToolStrip.Items.Add(CreateToolButton(null, "copy_16.png", "EditCopy", "Copy"));
		EditToolStrip.Items.Add(CreateToolButton(null, "paste_16.png", "EditPaste", "Paste"));
		EditToolStrip.Items.Add(CreateToolButton(null, "delete_16.png", "EditDelete", "Delete"));
		
		ViewToolStrip.Items.Add(CreateToolButton("B", null, "ViewBytes", "Bytes"));
		ViewToolStrip.Items.Add(CreateToolButton("W", null, "ViewWords", "Words"));
		ViewToolStrip.Items.Add(CreateToolButton("D", null, "ViewDwords", "Double Words"));
		ViewToolStrip.Items.Add(CreateToolButton("Q", null, "ViewQwords", "Quad Words"));
		ViewToolStrip.Items.Add(new ToolStripSeparator());
		ViewToolStrip.Items.Add(CreateToolButton("LE", null, "ViewLittleEndian", "Little Endian"));
		ViewToolStrip.Items.Add(CreateToolButton("BE", null, "ViewBigEndian", "Big Endian"));
		
		ToolStripPanel.Join(ViewToolStrip, 1);
		ToolStripPanel.Join(EditToolStrip, 1);
		ToolStripPanel.Join(FileToolStrip, 1);

	
		StatusBar.Dock = DockStyle.Bottom;
		ToolStripStatusLabel label = new ToolStripStatusLabel("Ready");
		label.Spring = true;
		label.TextAlign = ContentAlignment.MiddleLeft;
		StatusBar.Items.Add(label);
		label = new ToolStripStatusLabel("Addr: 00000000");
		StatusBar.Items.Add(label);
		label = new ToolStripStatusLabel("OVR");
		StatusBar.Items.Add(label);
		
		Controls.Add(StatusBar);

		ToolStripMenuItem mi;
		ToolStripMenuItem mi2;
		
		mi = new ToolStripMenuItem("&File");
		mi.DropDownItems.Add(CreateMenuItem("&New", "new_16.png", "FileNew", Keys.Control | Keys.N));
		mi.DropDownItems.Add(CreateMenuItem("&Open", "open_16.png", "FileOpen", Keys.Control | Keys.O));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("&Save", "save_16.png", "FileSave", Keys.Control | Keys.S));
		mi.DropDownItems.Add(CreateMenuItem("Save &As", null, "FileSaveAs", Keys.None));
		mi.DropDownItems.Add(CreateMenuItem("Save All", "saveall_16.png", "FileSaveAll", Keys.Control | Keys.Shift | Keys.S));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("Print Setup...", "printsetup_16.png", "FilePrintSetup", Keys.None));
		mi.DropDownItems.Add(CreateMenuItem("P&rint Preview...", "printpreview_16.png", "FilePrintPreview", Keys.None));
		mi.DropDownItems.Add(CreateMenuItem("&Print", "print_16.png", "FilePrint", Keys.Control | Keys.P));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("E&xit", null, "FileExit", Keys.None));
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&Edit");
		mi.DropDownItems.Add(CreateMenuItem("&Undo", "undo_16.png", "EditUndo", Keys.Control | Keys.Z));
		mi.DropDownItems.Add(CreateMenuItem("&Redo", "redo_16.png", "EditRedo", Keys.Control | Keys.Y));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("Cu&t", "cut_16.png", "EditCut", Keys.Control | Keys.X));
		mi.DropDownItems.Add(CreateMenuItem("&Copy", "copy_16.png", "EditCopy", Keys.Control | Keys.C));
		mi.DropDownItems.Add(CreateMenuItem("&Paste", "paste_16.png", "EditPaste", Keys.Control | Keys.V));
		mi.DropDownItems.Add(CreateMenuItem("&Delete", "delete_16.png", "EditDelete", Keys.Delete));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("&Options", "options_16.png", "EditOptions", Keys.None));
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&View");
		mi.DropDownItems.Add(new RadixMenu("Address Radix", "ViewAddressRadix", OnUiCommand));
		mi.DropDownItems.Add(new RadixMenu("Data Radix", "ViewDataRadix", OnUiCommand));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi2 = new ToolStripMenuItem("Go To", null, null, "ViewGoTo");
		mi2.DropDownItems.Add(CreateMenuItem("Top", null, "ViewGoToTop", Keys.Control | Keys.Home));
		mi2.DropDownItems.Add(CreateMenuItem("Bottom", null, "ViewGoToBottom", Keys.Control | Keys.End));
		mi2.DropDownItems.Add(new ToolStripMenuItem("Selection Start", null, OnUiCommand, "ViewGoToSelectionStart"));
		mi2.DropDownItems.Add(new ToolStripMenuItem("Selection End", null, OnUiCommand, "ViewGoToSelectionEnd"));
		mi2.DropDownItems.Add(new ToolStripMenuItem("Address...", null, OnUiCommand, "ViewGoToAddress"));
		mi2.DropDownItems.Add(new ToolStripMenuItem("Selection As Address", null, OnUiCommand, "ViewGoToSelectionAsAddress"));
		mi.DropDownItems.Add(mi2);
		MainMenuStrip.Items.Add(mi);
		
		mi = new ToolStripMenuItem("&Window");
		mi.DropDownItems.Add(new ToolStripMenuItem("&Split", Settings.Instance.Image("split_16.png"), OnUiCommand, "WindowSplit"));
		mi.DropDownItems.Add(new ToolStripMenuItem("&Duplicate", Settings.Instance.Image("duplicate_16.png"), OnUiCommand, "WindowDuplicate"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(new ToolStripMenuItem("Tile Horizontally", null, OnUiCommand, "WindowTileHorizontally"));
		mi.DropDownItems.Add(new ToolStripMenuItem("Tile Vertically", null, OnUiCommand, "WindowTileVertically"));
		MainMenuStrip.MdiWindowListItem = new ToolStripMenuItem("Window", null, null, "WindowWindows");
		mi.DropDownItems.Add(MainMenuStrip.MdiWindowListItem);
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&Help");
		mi.DropDownItems.Add(new ToolStripMenuItem("&About", null, OnUiCommand, "HelpAbout"));
		MainMenuStrip.Items.Add(mi);
		

		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("Cu&t");
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("&Copy");
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("&Paste");
		SelectionContextMenu.Items.Add("-");
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("Define Field");
		mi.Click += new EventHandler(OnSelectionContextMenuDefineField);
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("Go To Selection As Address");


		structurePanel.SelectionChanged += OnStructureSelectionChanged;

		Application.Idle += OnIdle;
		
		Size = new Size(1024, 768);
	}
	
	public static ToolStripMenuItem CreateMenuItem(string text, string image, string name, Keys shortcut)
	{
		ToolStripMenuItem i = new ToolStripMenuItem(text);
		if(image != null)
			i.Image = Settings.Instance.Image(image);
		i.Click += OnUiCommand;
		i.Name = name;
		i.ShortcutKeys = shortcut;
		return i;
	}
	
	public static ToolStripButton CreateToolButton(string text, string image, string name, string tooltip)
	{
		ToolStripButton i = new ToolStripButton(text);
		if(image != null)
			i.Image = Settings.Instance.Image(image);
		i.Click += OnUiCommand;
		i.Name = name;
		i.ToolTipText = tooltip;
		return i;
	}
	
	// TODO: MONO: mono ToolStripItemCollection.Find() is currently broken
	//             this function is a replacement
	ToolStripItem[] FindToolStripItems(ToolStripItemCollection items, string name, bool recurse)
	{
		List<ToolStripItem> list = new List<ToolStripItem>();
		
		foreach(ToolStripItem i in items)
		{
			if(i.Name == name)
				list.Add(i);
			else if(recurse && i is ToolStripMenuItem && ((ToolStripMenuItem)i).HasDropDownItems)
			{
				ToolStripItem[] found = FindToolStripItems(((ToolStripMenuItem)i).DropDownItems, name, recurse);
				if(found != null)
					list.AddRange(found);
			}
		}
		
		return list.ToArray();
	}
	
	protected void OnUpdateUiElement(object sender, EventArgs e)
	{
		CommandSet.Command cmd = (CommandSet.Command)sender;
		
		ToolStripItem[] items = FindToolStripItems(MainMenuStrip.Items, cmd.Name, true); // TODO: MONO: mono Find() doesn't work: MainMenuStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripMenuItem i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}
		
		items = FindToolStripItems(FileToolStrip.Items, cmd.Name, true); // FileToolStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}

		items = FindToolStripItems(EditToolStrip.Items, cmd.Name, true); //EditToolStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}
		
		items = FindToolStripItems(ViewToolStrip.Items, cmd.Name, true); //ViewToolStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}
	}
	
	public static void OnUiCommand(object sender, EventArgs e)
	{
		ToolStripItem item = (ToolStripItem)sender;
		Commands[item.Name].Invoke(item.Tag);
	}
	
	protected void OnIdle(object sender, EventArgs e)
	{
		bool haveChild = (ActiveMdiChild != null);
		
		Commands["FileSave"].Enabled = haveChild;
		Commands["FileSaveAs"].Enabled = haveChild;
		Commands["FileSaveAll"].Enabled = haveChild;
		Commands["FilePrintSetup"].Enabled = haveChild;
		Commands["FilePrintPreview"].Enabled = haveChild;
		Commands["FilePrint"].Enabled = haveChild;
		
		Commands["EditUndo"].Enabled = haveChild;
		Commands["EditRedo"].Enabled = haveChild;
		Commands["EditCut"].Enabled = haveChild;
		Commands["EditCopy"].Enabled = haveChild;
		Commands["EditPaste"].Enabled = haveChild;
		Commands["EditDelete"].Enabled = haveChild;

		Commands["ViewAddressRadix"].Enabled = haveChild;
		Commands["ViewDataRadix"].Enabled = haveChild;
		Commands["ViewGoTo"].Enabled = haveChild;
		Commands["ViewGoToTop"].Enabled = haveChild;
		Commands["ViewGoToBottom"].Enabled = haveChild;
		Commands["ViewGoToSelectionStart"].Enabled = haveChild;
		Commands["ViewGoToSelectionEnd"].Enabled = haveChild;
		Commands["ViewGoToAddress"].Enabled = haveChild;
		Commands["ViewGoToSelectionAsAddress"].Enabled = haveChild;
		Commands["ViewBytes"].Enabled = haveChild;
		Commands["ViewBytes"].Checked = (haveChild && ActiveView.BytesPerWord == 1);
		Commands["ViewWords"].Enabled = haveChild;
		Commands["ViewWords"].Checked = (haveChild && ActiveView.BytesPerWord == 2);
		Commands["ViewDwords"].Enabled = haveChild;
		Commands["ViewDwords"].Checked = (haveChild && ActiveView.BytesPerWord == 4);
		Commands["ViewQwords"].Enabled = haveChild;
		Commands["ViewQwords"].Checked = (haveChild && ActiveView.BytesPerWord == 8);
		Commands["ViewLittleEndian"].Enabled = haveChild;
		Commands["ViewLittleEndian"].Checked = (haveChild && ActiveView.Endian == Endian.Little);
		Commands["ViewBigEndian"].Enabled = haveChild;
		Commands["ViewBigEndian"].Checked = (haveChild && ActiveView.Endian == Endian.Big);
		
		Commands["WindowSplit"].Enabled = (ActiveMdiChild != null);		
		Commands["WindowDuplicate"].Enabled = (ActiveMdiChild != null);
		Commands["WindowTileHorizontally"].Enabled = (MdiChildren.Length > 1);
		Commands["WindowTileVertically"].Enabled = (MdiChildren.Length > 1);
	}
	
	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		LoadPlugins();
	}
	
	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		if(ActiveMdiChild != null)
			if(((HexViewForm)ActiveMdiChild).View.Selection == sender)
				selectionPanel.Update(((HexViewForm)ActiveMdiChild).View);
	}

	protected override void OnMdiChildActivate(EventArgs e)
	{
		base.OnMdiChildActivate(e);
		
		if(ActiveMdiChild != null)
			Commands.Merge(((HexViewForm)ActiveMdiChild).Commands);
		else
			Commands.RevertMerge();
		
		UpdateAllPanels();
	}

	protected void UpdateAllPanels()
	{
		HexView view = null;

		if(ActiveMdiChild != null)
			view = ((HexViewForm)ActiveMdiChild).View;

		selectionPanel.Update(view);
	}

	private void OnFileOpen(object sender, EventArgs args)
	{
		OpenFileDialog ofd = new OpenFileDialog();
		ofd.Title = "Select File";
		ofd.Filter = "All Files (*.*)|*.*";
		
		if(ofd.ShowDialog() == DialogResult.OK)
		{
			Document doc = new Document();
			doc.Open(ofd.FileName);

			HexViewForm form = new HexViewForm(doc);
			form.Text = ofd.FileName;
			form.MdiParent = this;
			form.View.Selection.Changed += new EventHandler(OnSelectionChanged);
			form.Show();
		}
	}

	private void OnWindowDuplicate(object sender, EventArgs e)
	{
		HexViewForm form = new HexViewForm(ActiveView.Document);
		form.Text = ActiveMdiChild.Text;
		form.MdiParent= this;
		form.View.Selection.Changed += new EventHandler(OnSelectionChanged);
		form.Show();
	}
	
	private void OnWindowSplit(object sender, EventArgs e)
	{
		((HexViewForm)ActiveMdiChild).Split();
	}
	

	protected void OnSelectionContextMenuDefineField(object sender, EventArgs e)
	{
		// TODO: StructureTree:
		
//		TreeListViewItem n = structurePanel.Tree.Items.Add("New Field");
//		n.Tag = new Record("New Field", (ulong)((HexViewForm)ActiveMdiChild).View.Selection.Start * 8,
//		                   (ulong)(((HexViewForm)ActiveMdiChild).View.Selection.End - ((HexViewForm)ActiveMdiChild).View.Selection.Start) / 8, 1);
//		n.BeginEdit();
	}

	protected void OnStructureSelectionChanged(object sender, EventArgs e)
	{
		List<Record> records = structurePanel.SelectedRecords;
		if(records.Count == 0)
			return;
		
		((HexViewForm)ActiveMdiChild).View.ClearHighlights();
		
		if(ActiveMdiChild != null)
		{
			Record record = records[0];

			int level = 0;
			while(record != null)
			{
				HexView.SelectionRange sel = new HexView.SelectionRange(((HexViewForm)ActiveMdiChild).View);
				sel.Set((long)record.Position, (long)(record.Position + (record.Length * record.ArrayLength)));
					
				if(level++ == 0)
				{
					sel.BackColor = Color.FromArgb(255, 200, 255, 200);
					sel.BorderColor = Color.LightGreen;
					sel.BorderWidth = 1;
				}
				else
				{
					sel.BackColor = Color.FromArgb(64, 192,192,192);
					sel.BorderColor = Color.FromArgb(128, 192,192,192);
					sel.BorderWidth = 1;
				}
					
				((HexViewForm)ActiveMdiChild).View.AddHighlight(sel);
					
//				((HexViewForm)ActiveMdiChild).View.Selection.Set(	(long)record.Position,
//				                                               (long)( record.Position + (record.Length * record.ArrayLength)));
				((HexViewForm)ActiveMdiChild).View.EnsureVisible((long)record.Position);
				
				record = record.Parent;
			}
		}
	}

	private void OnFileExit(object sender, EventArgs args)
	{
		Application.Exit();
	}

	private void OnWindowTileVertically(object sender, EventArgs args)
	{
		LayoutMdi(MdiLayout.TileVertical);
	}

	private void OnWindowTileHorizontally(object sender, EventArgs args)
	{
		LayoutMdi(MdiLayout.TileHorizontal);
	}

	private void OnHelpAbout(object sender, EventArgs args)
	{
		AboutDialog dlg = new AboutDialog();
		dlg.ShowDialog();
	}
	
	
	
	
	private void LoadPlugins()
	{
		string path = Application.StartupPath;
		string[] filenames = Directory.GetFiles(path, "*.dll");

		foreach(string filename in filenames)
		{
			try
			{
				Assembly asm = Assembly.LoadFile(filename);
				if(asm != null)
				{
					Type[] types = asm.GetTypes();
					foreach(Type type in types)
					{
						if(typeof(IPlugin).IsAssignableFrom(type))
						{
							IPlugin p = (IPlugin)Activator.CreateInstance(type);
							p.Initialize(this);
						}
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
			}		
        }
    }
	
	public HexView ActiveView 
	{
		get
		{
			if(ActiveMdiChild != null)
				return ((HexViewForm)ActiveMdiChild).View;
			else
				return null;
		}
	}
	
	HexView IPluginHost.ActiveView
	{
		get
		{
			if(ActiveMdiChild != null)
				return ((HexViewForm)ActiveMdiChild).View;
			else
				return null;
		}
	}
	
	void IPluginHost.AddWindow(Control control, string name)
	{
		_dockingManager.Contents.Add(control, name);
		_dockingManager.ShowContent(_dockingManager.Contents[name]);
	}
	
	ToolStripMenuItem IPluginHost.AddMenuItem(string path)
	{
		string[] parts = path.Split(new char[] {'/', '\\'});
		
		int i = 0;
		ToolStripMenuItem item = null;
		ToolStripItemCollection m = MainMenuStrip.Items;
		for(i = 0; i < parts.Length; ++i)
		{
			ToolStripItem[] items = FindToolStripItems(m, parts[i], false); //m.Find(parts[i], false);
			if(items.Length == 1)
				m = ((ToolStripMenuItem)items[0]).DropDownItems;
			else
				break;
		}
		
		for(; i < parts.Length; ++i)
		{
			item = (ToolStripMenuItem)m.Add(parts[i]);
			m = item.DropDownItems;
		}
	
		return item;
	}
}
