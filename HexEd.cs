using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Crownwood.DotNetMagic.Docking;
using Crownwood.DotNetMagic.Common;
using Crownwood.DotNetMagic.Controls;


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
	private TabbedGroups		_TabbedGroups = null;
	private ToolStripPanel		ToolStripPanel;
	private ToolStripMenuItem	ViewWindowsMenuItem;
	private ToolStrip			FileToolStrip = new ToolStrip();
	private ToolStrip			EditToolStrip = new ToolStrip();
	private ToolStrip			ViewToolStrip = new ToolStrip();
	private StatusStrip			StatusBar = new StatusStrip();
	private SelectionPanel		selectionPanel = new SelectionPanel();
	private StructurePanel		structurePanel;
	private HistoryPanel		HistoryPanel;
	private ImageList			WindowImageList;
	
	private Content[]			DefaultWindowPositionContent = new Content[4];
	
	private ToolStripProgressBar	ProgressBar;
	private ToolStripStatusLabel	ProgressMessage;
	private ToolStripStatusLabel	EditModeLabel;
	private ToolStripStatusLabel	AddressLabel;
	private ToolStripStatusLabel	ModifiedLabel;

	private static CommandSet	Commands = new CommandSet();
	
	protected ContextMenuStrip	SelectionContextMenu	= new ContextMenuStrip();

	public event EventHandler ActiveViewChanged;
	
	protected Timer ProgressNotificationTimer = new Timer();
	protected ProgressNotification CurrentProgressNotification;
	protected ProgressNotificationCollection _ProgressNotifications = new ProgressNotificationCollection();
	public ProgressNotificationCollection ProgressNotifications
	{
		get { return _ProgressNotifications; }
	}
	
	
	[STAThread]
	public static void Main()
	{
#if DEBUG
#if MONO
System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
#endif
#endif
		
		Application.EnableVisualStyles();
		Application.DoEvents();
		Application.Run(new HexEdApp());
	}

	public HexEdApp()
	{
		Text = "HexEd";

		CreateCommands();
		
		WindowImageList = new ImageList();
		WindowImageList.ColorDepth = ColorDepth.Depth32Bit;
		
		_TabbedGroups = new TabbedGroups(VisualStyle.Office2003);
		_TabbedGroups.AllowDrop = true;
		_TabbedGroups.AtLeastOneLeaf = true;
		_TabbedGroups.Dock = System.Windows.Forms.DockStyle.Fill;
		//_TabbedGroups.ImageList = this.groupTabs;
		_TabbedGroups.Location = new System.Drawing.Point(5, 5);
		_TabbedGroups.ResizeBarColor = System.Drawing.SystemColors.Control;
		_TabbedGroups.Size = new System.Drawing.Size(482, 304);
		//_TabbedGroups.TabControlCreated += new Crownwood.DotNetMagic.Controls.TabbedGroups.TabControlCreatedHandler(this.OnTabControlCreated);
//		_TabbedGroups.ExternalDrop += OnTabbedGroupsExternalDrop;
//		_TabbedGroups.ExternalDragEnter += OnTabbedGroupsExternalDragEnter;
		Controls.Add(_TabbedGroups);
				
		ToolStripPanel = new ToolStripPanel();
		MainMenuStrip = new MenuStrip();
		ToolStripPanel.Controls.Add(MainMenuStrip);
		ToolStripPanel.Dock = DockStyle.Top;
		Controls.Add(ToolStripPanel);

		CreateMenus();
		CreateToolBars();
		
		structurePanel = new StructurePanel(this);
		HistoryPanel = new HistoryPanel(this);
		_dockingManager = new DockingManager(this, VisualStyle.Office2003);
		_dockingManager.InnerControl = _TabbedGroups;
		_dockingManager.OuterControl = ToolStripPanel;

		((IPluginHost)this).AddWindow(selectionPanel, "Selection", Settings.Instance.Image("selection_16.png"), DefaultWindowPosition.BottomLeft, true);
		((IPluginHost)this).AddWindow(structurePanel, "Structure", Settings.Instance.Image("structure_16.png"), DefaultWindowPosition.Left, true);
		((IPluginHost)this).AddWindow(HistoryPanel, "History", Settings.Instance.Image("history_16.png"), DefaultWindowPosition.Left, true);

		
	
		StatusBar.Dock = DockStyle.Bottom;
		ProgressBar = new ToolStripProgressBar();
		ProgressBar.Minimum = 0;
		ProgressBar.Maximum = 100;
		ProgressBar.Visible = false;
		StatusBar.Items.Add(ProgressBar);
		ProgressMessage = new ToolStripStatusLabel("Ready");
		ProgressMessage.Spring = true;
		ProgressMessage.TextAlign = ContentAlignment.MiddleLeft;
		StatusBar.Items.Add(ProgressMessage);
		StatusBar.Items.Add(new ToolStripSeparator());
		AddressLabel = new ToolStripStatusLabel("Addr: ");
		StatusBar.Items.Add(AddressLabel);
		StatusBar.Items.Add(new ToolStripSeparator());
		EditModeLabel = new ToolStripStatusLabel("OVR");
		StatusBar.Items.Add(EditModeLabel);
		StatusBar.Items.Add(new ToolStripSeparator());
		ModifiedLabel = new ToolStripStatusLabel("MOD");
		ModifiedLabel.ForeColor = SystemColors.GrayText;
		StatusBar.Items.Add(ModifiedLabel);
		StatusBar.Items.Add(new ToolStripSeparator());
		
		Controls.Add(StatusBar);

		ToolStripMenuItem mi;
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("Cu&t");
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("&Copy");
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("&Paste");
		SelectionContextMenu.Items.Add("-");
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("Define Field");
		mi.Click += new EventHandler(OnSelectionContextMenuDefineField);
		mi = (ToolStripMenuItem)SelectionContextMenu.Items.Add("Go To Selection As Address");


		structurePanel.SelectionChanged += OnStructureSelectionChanged;
		_TabbedGroups.PageChanged += OnTabbedGroupsPageChanged;

		AllowDrop = true;
		this.DragEnter += OnDragEnter;
		this.DragDrop += OnDragDrop;
		
		ProgressNotificationTimer.Interval = 2500;
		ProgressNotificationTimer.Tick += OnProgressNotificationTimerTick;
		ProgressNotifications.NotificationAdded += OnProgressNotificationAdded;
		ProgressNotifications.NotificationRemoved += OnProgressNotificationRemoved;
		
		Application.Idle += OnIdle;
		
		Size = new Size(1024, 768);
	}

	
	private void CreateCommands()
	{
		Commands.Add("FileNew", OnFileNew, OnUpdateUiElement);
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
		Commands.Add("EditSelectAll", null, OnUpdateUiElement);
		Commands.Add("EditInsertFile", null, OnUpdateUiElement);
		Commands.Add("EditInsertPattern", null, OnUpdateUiElement);
		Commands.Add("EditPreferences", OnEditPreferences, OnUpdateUiElement);
		
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

		Commands.Add("HelpAbout", OnHelpAbout, OnUpdateUiElement);
	}
	
	private void CreateMenus()
	{
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
		mi.DropDownItems.Add(CreateMenuItem("Select &All", null, "EditSelectAll", Keys.Control | Keys.A));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi2 = new ToolStripMenuItem("&Insert", null, null, "EditInsert");
		mi2.DropDownItems.Add(CreateMenuItem("&File...", null, "EditInsertFile", Keys.None));
		mi2.DropDownItems.Add(CreateMenuItem("&Pattern...", null, "EditInsertPattern", Keys.None));
		mi.DropDownItems.Add(mi2);
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("&Preferences...", "options_16.png", "EditPreferences", Keys.None));
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
		mi.DropDownItems.Add(new ToolStripSeparator());
		ViewWindowsMenuItem = (ToolStripMenuItem)mi.DropDownItems.Add("Windows");
		MainMenuStrip.Items.Add(mi);
		
		mi = new ToolStripMenuItem("&Window");
		mi.DropDownItems.Add(new ToolStripMenuItem("&Split", Settings.Instance.Image("split_16.png"), OnUiCommand, "WindowSplit"));
		mi.DropDownItems.Add(new ToolStripMenuItem("&Duplicate", Settings.Instance.Image("duplicate_16.png"), OnUiCommand, "WindowDuplicate"));
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&Help");
		mi.DropDownItems.Add(new ToolStripMenuItem("&About", null, OnUiCommand, "HelpAbout"));
		MainMenuStrip.Items.Add(mi);
	}
	
	private void CreateToolBars()
	{
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
	ToolStripItem[] FindToolStripItems(ToolStripItemCollection items, string name, bool recurse, bool byText)
	{
		List<ToolStripItem> list = new List<ToolStripItem>();
		
		foreach(ToolStripItem i in items)
		{
			if((byText && i.Text == name) || (!byText && i.Name == name))
				list.Add(i);
			else if(recurse && i is ToolStripMenuItem && ((ToolStripMenuItem)i).HasDropDownItems)
			{
				ToolStripItem[] found = FindToolStripItems(((ToolStripMenuItem)i).DropDownItems, name, recurse, byText);
				if(found != null)
					list.AddRange(found);
			}
		}
		
		return list.ToArray();
	}
	
	protected void OnUpdateUiElement(object sender, EventArgs e)
	{
		CommandSet.Command cmd = (CommandSet.Command)sender;
		
		ToolStripItem[] items = FindToolStripItems(MainMenuStrip.Items, cmd.Name, true, false); // TODO: MONO: mono Find() doesn't work: MainMenuStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripMenuItem i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}
		
		items = FindToolStripItems(FileToolStrip.Items, cmd.Name, true, false); // FileToolStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}

		items = FindToolStripItems(EditToolStrip.Items, cmd.Name, true, false); //EditToolStrip.Items.Find(cmd.Name, true);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}
		
		items = FindToolStripItems(ViewToolStrip.Items, cmd.Name, true, false); //ViewToolStrip.Items.Find(cmd.Name, true);
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
		bool haveChild = (_TabbedGroups.ActiveTabPage != null);
		bool haveSelection = haveChild ? ((HexViewForm)_TabbedGroups.ActiveTabPage).View.Selection.Length > 0 : false;
		HexView view = haveChild ? ((HexViewForm)_TabbedGroups.ActiveTabPage).View : null;
		
		Commands["FileSave"].Enabled = haveChild && view.Document.IsModified;
		Commands["FileSaveAs"].Enabled = haveChild;
		Commands["FileSaveAll"].Enabled = haveChild;
		Commands["FilePrintSetup"].Enabled = haveChild;
		Commands["FilePrintPreview"].Enabled = haveChild;
		Commands["FilePrint"].Enabled = haveChild;
		
		Commands["EditUndo"].Enabled = haveChild && view.Document.CanUndo;
		Commands["EditRedo"].Enabled = haveChild && view.Document.CanRedo;
		Commands["EditCut"].Enabled = haveSelection;
		Commands["EditCopy"].Enabled = haveSelection;
		Commands["EditPaste"].Enabled = haveChild;
		Commands["EditDelete"].Enabled = haveSelection;
		Commands["EditSelectAll"].Enabled = haveChild;

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
		
		Commands["WindowSplit"].Enabled = haveChild;		
		Commands["WindowDuplicate"].Enabled = haveChild;
		
		ModifiedLabel.ForeColor = haveChild && view.Document.IsModified ? Color.Black : SystemColors.GrayText;
	}
	
	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		LoadPlugins();
	}
	
	protected void OnSelectionChanged(object sender, EventArgs e)
	{
		if(_TabbedGroups.ActiveTabPage != null)
		{
			HexView view = ((HexViewForm)_TabbedGroups.ActiveTabPage).View;
			if(view.Selection == sender)
			{
				selectionPanel.Update(view);
				if(view.Selection.Length == 0)
					AddressLabel.Text = "Addr: " + view.IntToRadixString((ulong)(view.Selection.Start / 8), view.AddressRadix, 0);
				else
					AddressLabel.Text = "Addr: " + 
										view.IntToRadixString((ulong)(view.Selection.Start / 8), view.AddressRadix, 0) + 
										" -> " + 
										view.IntToRadixString((ulong)(view.Selection.End / 8), view.AddressRadix, 0);
			}
		}
	}

	protected void OnEditModeChanged(object sender, EventArgs e)
	{
		if(_TabbedGroups.ActiveTabPage != null)
		{
			if(((HexViewForm)_TabbedGroups.ActiveTabPage).View == sender)
			{
				if(((HexViewForm)_TabbedGroups.ActiveTabPage).View.EditMode == EditMode.Insert)
					EditModeLabel.Text = "INS";
				else
					EditModeLabel.Text = "OVR";
			}
		}		
	}
	
	protected void UpdateWindowTitle(string documentTitle)
	{
		string[] parts = Text.Split(new string[] {" - "}, 2, StringSplitOptions.None);
		Text = parts[0] + " - " + documentTitle;
	}
	
	protected void OnTabbedGroupsPageChanged(object sender, Crownwood.DotNetMagic.Controls.TabPage e)
	{
		if(_TabbedGroups.ActiveTabPage != null)
		{
			UpdateWindowTitle(_TabbedGroups.ActiveTabPage.Title);
			Commands.Merge(((HexViewForm)_TabbedGroups.ActiveTabPage).Commands);
		}
		else
			Commands.RevertMerge();
		
		UpdateAllPanels();
		
		if(ActiveViewChanged != null)
			ActiveViewChanged(this, new EventArgs());
	}

	protected void UpdateAllPanels()
	{
		HexView view = null;

		if(_TabbedGroups.ActiveTabPage != null)
			view = ((HexViewForm)_TabbedGroups.ActiveTabPage).View;

		selectionPanel.Update(view);
	}

	public void Open(string filename)
	{
		string[] filenameParts = filename.Split(new char[] {Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar});
		
		try
		{
			Document doc = new Document(filename);
			HexViewForm form = new HexViewForm(doc);
			form.Title = filenameParts[filenameParts.Length - 1];
			form.Image = Settings.Instance.Image("document_16.png");
			form.View.Selection.Changed += new EventHandler(OnSelectionChanged);
			form.View.EditModeChanged += new EventHandler(OnEditModeChanged);
			_TabbedGroups.ActiveLeaf.TabPages.Add(form);
			((Crownwood.DotNetMagic.Controls.TabControl)_TabbedGroups.ActiveLeaf.GroupControl).SelectedTab = form;
		}
		catch(System.Security.SecurityException ex)
		{
			MessageBox.Show(this, ex.Message, "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		catch(UnauthorizedAccessException ex)
		{
			MessageBox.Show(this, ex.Message, "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
	
	public void New()
	{
		Document doc = new Document();
		HexViewForm form = new HexViewForm(doc);
		form.Title = "New File";
		form.Image = Settings.Instance.Image("document_16.png");
		form.View.Selection.Changed += new EventHandler(OnSelectionChanged);
		form.View.EditModeChanged += new EventHandler(OnEditModeChanged);
		_TabbedGroups.ActiveLeaf.TabPages.Add(form);
		((Crownwood.DotNetMagic.Controls.TabControl)_TabbedGroups.ActiveLeaf.GroupControl).SelectedTab = form;
	}
	
	protected void OnDragEnter(object sender, DragEventArgs e)
	{
		if(e.Data.GetDataPresent(DataFormats.FileDrop))
			e.Effect = DragDropEffects.All;
		else
			e.Effect = DragDropEffects.None;
	}

	protected void OnDragDrop(object sender, DragEventArgs e)
	{
		string[] filenames = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
		
		foreach(string filename in filenames)
			Open(filename);
	}
	
	/*
	protected void OnTabbedGroupsExternalDrop(TabbedGroups sender, TabGroupLeaf leaf, Crownwood.DotNetMagic.Controls.TabControl control, TabbedGroups.DragProvider provider)
	{
		
	}
	
	protected bool OnTabbedGroupsExternalDragEnter(TabbedGroups sender, TabGroupLeaf leaf, Crownwood.DotNetMagic.Controls.TabControl control, DragEventArgs e)
	{
		OnDragEnter(sender, e);
		return (e.AllowedEffect != DragDropEffects.None);		
	}
	*/
	
	private void OnFileNew(object sender, EventArgs e)
	{
		New();
	}
	
	private void OnFileOpen(object sender, EventArgs args)
	{
		OpenFileDialog ofd = new OpenFileDialog();
		ofd.Title = "Select File";
		ofd.Filter = "All Files (*.*)|*.*";
		
		if(ofd.ShowDialog() == DialogResult.OK)
			Open(ofd.FileName);
	}

	private void OnEditPreferences(object sender, EventArgs e)
	{
		SettingsDialog dlg = new SettingsDialog();
		dlg.ShowDialog();
	}
	
	private void OnWindowDuplicate(object sender, EventArgs e)
	{/*
		HexViewForm form = new HexViewForm(ActiveView.Document);
		form.Text = ActiveMdiChild.Text;
		form.MdiParent= this;
		form.View.Selection.Changed += new EventHandler(OnSelectionChanged);
		form.View.EditModeChanged += new EventHandler(OnEditModeChanged);
		form.Show();*/
	}
	
	private void OnWindowSplit(object sender, EventArgs e)
	{
		//((HexViewForm)ActiveMdiChild).Split();
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
		
		((HexViewForm)_TabbedGroups.ActiveTabPage).View.ClearHighlights();
		
		if(_TabbedGroups.ActiveTabPage != null)
		{
			Record record = records[0];

			int level = 0;
			while(record != null)
			{
				HexView.SelectionRange sel = new HexView.SelectionRange(((HexViewForm)_TabbedGroups.ActiveTabPage).View);
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
					
				((HexViewForm)_TabbedGroups.ActiveTabPage).View.AddHighlight(sel);
					
//				((HexViewForm)_TabbedGroups.ActiveTabPage).View.Selection.Set(	(long)record.Position,
//				                                               (long)( record.Position + (record.Length * record.ArrayLength)));
				((HexViewForm)_TabbedGroups.ActiveTabPage).View.EnsureVisible((long)record.Position);
				
				record = record.Parent;
			}
		}
	}

	private void OnFileExit(object sender, EventArgs args)
	{
		Application.Exit();
	}

	protected void OnViewWindowsMenuItemClicked(object sender, EventArgs e)
	{
		Content content = ((ToolStripItem)sender).Tag as Content;
		_dockingManager.ShowContent(content);
		content.BringToFront();
	}
	
	private void OnHelpAbout(object sender, EventArgs args)
	{
		AboutDialog dlg = new AboutDialog();
		dlg.ShowDialog();
	}
	

	protected void OnProgressNotificationAdded(object sender, ProgressNotificationEventArgs e)
	{
		// Unsubscribe from current notification
		if(CurrentProgressNotification != null)
			CurrentProgressNotification.Changed -= OnProgressNotificationChanged;
		
		// If there's now more than one, start the timer
		if(ProgressNotifications.Count > 1)
		{
			Console.WriteLine("Starting notification timer");
			//ProgressNotificationTimer.Enabled = true;
			ProgressNotificationTimer.Start();
		}
		
		// Subscribe to the new notification
		CurrentProgressNotification = e.Notification;
		OnProgressNotificationChanged(e.Notification, EventArgs.Empty);
		e.Notification.Changed += OnProgressNotificationChanged;
	}
	
	protected void OnProgressNotificationRemoved(object sender, ProgressNotificationEventArgs e)
	{
		// If we were subscribed to this notification, we need to change to a different one
		if(CurrentProgressNotification == e.Notification)
		{
			e.Notification.Changed -= OnProgressNotificationChanged;
			if(ProgressNotifications.Count > 0)
			{
				if(e.Notification.Index >= ProgressNotifications.Count)
					CurrentProgressNotification = ProgressNotifications[0];
				else
					CurrentProgressNotification = ProgressNotifications[e.Notification.Index];
				CurrentProgressNotification.Changed += OnProgressNotificationChanged;
			}
			else
				CurrentProgressNotification = null;
		}
					
		// If there's only one left, we don't need the timer anymore
		if(ProgressNotifications.Count == 1)
		{
			Console.WriteLine("Stopping notification timer");
			ProgressNotificationTimer.Stop();
			//ProgressNotificationTimer.Enabled = false;
		}
		
		// If there's none left, set status to ready, otherwise update to
		// newly subscribed notification
		if(CurrentProgressNotification == null)
		{
			ProgressBar.Visible = false;
			ProgressMessage.Text = "Ready";
		}
		else
			OnProgressNotificationChanged(CurrentProgressNotification, EventArgs.Empty);
	}
	
	protected void OnProgressNotificationTimerTick(object sender, EventArgs e)
	{
		int next = (CurrentProgressNotification.Index + 1) % ProgressNotifications.Count;
		Console.WriteLine(String.Format("Progress notification timer TICK: {0} -> {1}", CurrentProgressNotification.Index, next));
		CurrentProgressNotification.Changed -= OnProgressNotificationChanged;
		CurrentProgressNotification = ProgressNotifications[next];
		CurrentProgressNotification.Changed += OnProgressNotificationChanged;
		OnProgressNotificationChanged(CurrentProgressNotification, EventArgs.Empty);
	}
	
	protected void OnProgressNotificationChanged(object sender, EventArgs e)
	{
		ProgressBar.Visible = true;
		ProgressNotification progress = (ProgressNotification)sender;
		int percent = progress.PercentComplete;
		if(percent < 0)
			percent = 0;
		if(percent > 100)
			percent = 100;
		ProgressBar.Value = percent; 
		ProgressMessage.Text = String.Format("{0}% {1}", percent, progress.Message);
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
							Console.WriteLine("Loaded plugin: " + filename + " :: " + type);
							IPlugin p = (IPlugin)Activator.CreateInstance(type);
							p.Initialize(this);
						}
					}
				}
				else
					Console.WriteLine("Failed to load plugin assembly");
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
			if(_TabbedGroups.ActiveTabPage != null)
				return ((HexViewForm)_TabbedGroups.ActiveTabPage).View;
			else
				return null;
		}
	}
	
	Settings IPluginHost.Settings
	{
		get { return Settings.Instance; }
	}
	
	HexView IPluginHost.ActiveView
	{
		get
		{
			if(_TabbedGroups.ActiveTabPage != null)
				return ((HexViewForm)_TabbedGroups.ActiveTabPage).View;
			else
				return null;
		}
	}
	
	
	void IPluginHost.AddWindow(Control control, string name, Image image, DefaultWindowPosition defaultPosition, bool visibleByDefault)
	{	
		Content content;
		if(image != null)
		{
			WindowImageList.Images.Add(image);
			int imageIndex = WindowImageList.Images.Count - 1;
			content = _dockingManager.Contents.Add(control, name, WindowImageList, imageIndex);
			ToolStripItem item = ViewWindowsMenuItem.DropDownItems.Add(name, image);
			item.Tag = content;
			item.Click += OnViewWindowsMenuItemClicked;
		}
		else
		{
			content = _dockingManager.Contents.Add(control, name);
			ToolStripItem item = ViewWindowsMenuItem.DropDownItems.Add(name);
			item.Tag = content;
			item.Click += OnViewWindowsMenuItemClicked;
		}
			
		content.DisplaySize = new Size(400, 240);
		
		if(DefaultWindowPositionContent[(int)defaultPosition] == null)
		{
			DefaultWindowPositionContent[(int)defaultPosition] = content;
			
			if(defaultPosition == DefaultWindowPosition.BottomLeft && 
			   DefaultWindowPositionContent[(int)DefaultWindowPosition.BottomRight] != null)
			{
				_dockingManager.AddContentToZone(content, DefaultWindowPositionContent[(int)DefaultWindowPosition.BottomRight].ParentWindowContent.ParentZone, 0);
			}
			else if(defaultPosition == DefaultWindowPosition.BottomRight && 
			   DefaultWindowPositionContent[(int)DefaultWindowPosition.BottomLeft] != null)
			{
				_dockingManager.AddContentToZone(content, DefaultWindowPositionContent[(int)DefaultWindowPosition.BottomLeft].ParentWindowContent.ParentZone, 1);
			}
			else
			{
				switch(defaultPosition)
				{
					case DefaultWindowPosition.BottomLeft:
					case DefaultWindowPosition.BottomRight:
						_dockingManager.AddContentWithState(content, State.DockBottom);
						break;
					case DefaultWindowPosition.Left:
						_dockingManager.AddContentWithState(content, State.DockLeft);
						break;
					case DefaultWindowPosition.Right:
						_dockingManager.AddContentWithState(content, State.DockRight);
						break;
				}
			}
		}
		else
			_dockingManager.AddContentToWindowContent(content, DefaultWindowPositionContent[(int)defaultPosition].ParentWindowContent);
				
		if(visibleByDefault)
			_dockingManager.ShowContent(content);
	}
	
	ToolStripMenuItem IPluginHost.AddMenuItem(string path)
	{
		string[] parts = path.Split(new char[] {'/', '\\'});
		
		int i = 0;
		ToolStripMenuItem item = null;
		ToolStripItemCollection m = MainMenuStrip.Items;
		for(i = 0; i < parts.Length; ++i)
		{
			ToolStripItem[] items = FindToolStripItems(m, parts[i], false, true); //m.Find(parts[i], false);
			if(items.Length == 1)
			{
				Console.WriteLine("Found 1");
				m = ((ToolStripMenuItem)items[0]).DropDownItems;
			}
			else
			{
				Console.WriteLine("Found: " + items.Length);
				break;
			}
		}
		
		for(; i < parts.Length; ++i)
		{
			item = (ToolStripMenuItem)m.Add(parts[i]);
			m = item.DropDownItems;
		}
	
		return item;
	}
}
