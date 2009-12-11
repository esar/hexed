using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Crownwood.DotNetMagic.Docking;
using Crownwood.DotNetMagic.Common;
using Crownwood.DotNetMagic.Controls;



public enum Menus
{
	Main,
	AddressContext,
	DataContext,
	SelectedDataContext
}

class ViewClosedEventArgs : EventArgs
{
	public HexViewForm Form;
	public ViewClosedEventArgs(HexViewForm form)
	{
		Form = form;
	}
}

class OpenWindowMenu : ToolStripMenuItem
{
	class OpenWindowMenuItem : ToolStripMenuItem 
	{
		public OpenWindowMenuItem(string text) : base(text) {}
	}
	
	protected bool FixedOwner = false;
	protected ToolStripMenuItem CurrentOwner;
	protected List<ToolStripMenuItem> Items = new List<ToolStripMenuItem>();
	protected EventHandler Handler;

	protected bool _IsPath;
	public bool IsPath
	{
		get { return _IsPath; }
		set { _IsPath = value; }
	}
	
	protected ToolStripMenuItem _SelectedItem;
	public ToolStripMenuItem SelectedItem 
	{ 
		get	{ return _SelectedItem; }
		set
		{
			if(_SelectedItem != value)
			{
				if(_SelectedItem != null)
					_SelectedItem.Checked = false;
				_SelectedItem = value;
				if(_SelectedItem != null)
					_SelectedItem.Checked = true;
				
				if(_SelectedItem != null && Handler != null)
					Handler(_SelectedItem, EventArgs.Empty);
			}
		}
	}
	
	public OpenWindowMenu()
	{
		Text = "No Open Windows";
		Enabled = false;		
	}
	
	public OpenWindowMenu(ToolStripMenuItem owner, EventHandler handler) : this()
	{
		CurrentOwner = owner;
		CurrentOwner.DropDownOpening += OnOwnerDropDownOpening;
		FixedOwner = true;
		Handler = handler;
	}
	
	public ToolStripMenuItem Add(string name, object tag)
	{
		ToolStripMenuItem item;
		if(_IsPath)
			item = new OpenWindowMenuItem(EllipsisPath(name, 32));
		else
			item = new OpenWindowMenuItem(name);
		item.Click += OnItemClick;
		item.Name = Name;
		item.Tag = tag;
		Items.Add(item);
		return item;
	}
	
	public void Remove(object tag)
	{
		foreach(ToolStripMenuItem i in Items)
		{
			if(i.Tag == tag)
			{
				Items.Remove(i);
				break;
			}
		}
	}
	
	public void SetSelectedItem(object tag)
	{
		foreach(ToolStripMenuItem i in Items)
		{
			if(i.Tag == tag)
			{
				SelectedItem = i;
				break;
			}
		}
	}

	protected static string EllipsisPath(string path, int maxLength)
	{
		// If whole path fits, return whole path
		if(path.Length < maxLength)
			return path;
		
		string[] parts = path.Split(new char[] { System.IO.Path.DirectorySeparatorChar, 
		                                         System.IO.Path.AltDirectorySeparatorChar});
		
		// If first part is empty, remove it and add '/' to front of new first entry
		if(parts[0].Length == 0)
		{
			string[] tmp = new string[parts.Length - 1];
			Array.Copy(parts, 1, tmp, 0, tmp.Length);
			tmp[0] = String.Format("{0}{1}", System.IO.Path.DirectorySeparatorChar, tmp[0]); 
			parts = tmp;
		}
		
		// If the filename on its own doesn't fit, return "filename..."
		string filename = parts[parts.Length - 1];
		if(filename.Length + 5 > maxLength)
			return filename.Substring(0, maxLength - 3) + "...";

		// If the first directory doesn't fit, return "firstDir.../filename"
		maxLength -= filename.Length + 5; // 5 = "/.../"
		if(parts[0].Length >= maxLength)
			return String.Format("{0}...{1}{2}", parts[0].Substring(0, maxLength), 
			                     System.IO.Path.DirectorySeparatorChar, filename);
		
		// Return as many of the first directories as possible, "dir/dir/.../filename"
		System.Text.StringBuilder result = new System.Text.StringBuilder();
		
		result.Append(parts[0]);
		result.Append(System.IO.Path.DirectorySeparatorChar);
		maxLength -= parts[0].Length + 1;
		
		for(int i = 1; i < parts.Length - 1; ++i)
		{
			if(parts[i].Length < maxLength)
			{
				result.Append(parts[i]);
				result.Append(System.IO.Path.DirectorySeparatorChar);
				maxLength -= parts[i].Length + 1; 
			}
			else
				break;
		}
		
		result.Append("...");
		result.Append(System.IO.Path.DirectorySeparatorChar);
		result.Append(filename);
		
		return result.ToString();
	}
	
	protected override void OnOwnerChanged(EventArgs e)
	{
		base.OnOwnerChanged(e);
		
		if(FixedOwner)
			return;
		
		if(CurrentOwner != null)
			CurrentOwner.DropDownOpening -= OnOwnerDropDownOpening;
		CurrentOwner = OwnerItem as ToolStripMenuItem;
		if(CurrentOwner != null)
			CurrentOwner.DropDownOpening += OnOwnerDropDownOpening;
	}

	protected void OnItemClick(object sender, EventArgs e)
	{
		SelectedItem = sender as ToolStripMenuItem;
	}
	
	protected void OnOwnerDropDownOpening(object sender, EventArgs e)
	{
		if(CurrentOwner != null)
		{
			List<ToolStripMenuItem> itemsToRemove = new List<ToolStripMenuItem>();
			foreach(ToolStripItem item in CurrentOwner.DropDownItems)
				if(item is OpenWindowMenuItem)
					itemsToRemove.Add((ToolStripMenuItem)item);
			foreach(ToolStripMenuItem item in itemsToRemove)
				CurrentOwner.DropDownItems.Remove(item);
		}

		if(CurrentOwner != null && Items.Count > 0)
		{
			int index = CurrentOwner.DropDownItems.IndexOf(this);
			foreach(ToolStripMenuItem item in Items)
				CurrentOwner.DropDownItems.Insert(++index, item);
			this.Visible = false;
		}
		else
		{
			this.Visible = true;
			this.Enabled = false;
		}
	}	
}

class HexEdApp : Form, IPluginHost, IEnumerable<Document>
{
	private static HexEdApp _Instance;
	public static HexEdApp Instance 
	{ 
		get 
		{ 
			if(_Instance == null) 
				_Instance = new HexEdApp(); 
			return _Instance; 
		} 
	}

	private Dictionary< Document, List< HexViewForm > > Documents = new Dictionary<Document,List<HexViewForm>>();
	
	private DockingManager          _dockingManager = null;
	private TabbedGroups            _TabbedGroups = null;
	private ToolStripPanel          ToolStripPanel;
	private ToolStrip               FileToolStrip = new ToolStrip();
	private ToolStrip               EditToolStrip = new ToolStrip();
	private ToolStrip               ViewToolStrip = new ToolStrip();
	private StatusStrip             StatusBar = new StatusStrip();
	private SelectionPanel          selectionPanel;
	private HistoryPanel            HistoryPanel;
	private ImageList               WindowImageList;
	
	private Content[]               DefaultWindowPositionContent = new Content[4];
	
	private RadixMenu               AddressRadixMenu;
	private RadixMenu               DataRadixMenu;
	private OpenWindowMenu          OpenWindowMenu;

	private ContextMenuStrip[]      ContextMenus;
	
	private ToolStripProgressBar    ProgressBar;
	private ToolStripStatusLabel    ProgressMessage;
	private ToolStripStatusLabel    EditModeLabel;
	private ToolStripStatusLabel    AddressLabel;
	private ToolStripStatusLabel    ModifiedLabel;

	protected PluginManager         _PluginManager;
	public PluginManager            PluginManager { get { return _PluginManager; } }

	private static CommandSet       _Commands = new CommandSet();
	public CommandSet               Commands { get { return _Commands; } }

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
		SplashScreen.Show();
		Application.Run(HexEdApp.Instance);
	}

	public HexEdApp()
	{
		Text = "HexEd";

		_PluginManager = new PluginManager(this);
		
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
		CreateContextMenus();
		CreateToolBars();
		
		selectionPanel = new SelectionPanel(this);
		HistoryPanel = new HistoryPanel(this);
		_dockingManager = new DockingManager(this, VisualStyle.Office2003);
		_dockingManager.InnerControl = _TabbedGroups;
		_dockingManager.OuterControl = ToolStripPanel;

		((IPluginHost)this).AddWindow(selectionPanel, "Selection", 
		                              Settings.Instance.Image("selection_16.png"), 
		                              DefaultWindowPosition.BottomLeft, true);
		((IPluginHost)this).AddWindow(HistoryPanel, "History", 
		                              Settings.Instance.Image("history_16.png"), 
		                              DefaultWindowPosition.Left, true);


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
		ModifiedLabel.Enabled = false;
		StatusBar.Items.Add(ModifiedLabel);
		StatusBar.Items.Add(new ToolStripSeparator());

		Controls.Add(StatusBar);

		_TabbedGroups.PageChanged += OnTabbedGroupsPageChanged;
		_TabbedGroups.PageCloseRequest += OnTabbedGroupsPageCloseRequest;

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
		Commands.Add("File/New", "Creates a new document", "&New",     
		             Settings.Instance.Image("new_16.png"),  
		             new Keys[] { Keys.Control | Keys.N }, 
		             OnFileNew, OnUpdateUiElement);
		Commands.Add("File/Open", "Opens an existing document", "&Open",    
		             Settings.Instance.Image("open_16.png"), 
		             new Keys[] { Keys.Control | Keys.O },  
		             OnFileOpen, OnUpdateUiElement);
		Commands.Add("File/Close", "Closes the current document", "&Close",
		             null,
		             null,
		             OnFileClose, OnUpdateUiElement);
		Commands.Add("File/Close All", "Closes all open documents", "Close All",
		             null,
		             null,
		             OnFileCloseAll, OnUpdateUiElement);
		Commands.Add("File/Save", "Saves the current document", "&Save",    
		             Settings.Instance.Image("save_16.png"), 
		             new Keys[] { Keys.Control | Keys.S }, 
		             OnFileSave, OnUpdateUiElement);
		Commands.Add("File/Save As", "Saves the current document with the specified file name", "Save &As", 
		             null,                                   
		             null,             
		             OnFileSaveAs, OnUpdateUiElement);
		Commands.Add("File/Save All", "Saves all open documents", "Save All", 
		             Settings.Instance.Image("saveall_16.png"), 
		             new Keys[] { Keys.Control | Keys.Shift | Keys.S }, 
		             null, OnUpdateUiElement);
		Commands.Add("File/Print Setup", "Configures printing", "Print Setup...", 
		             Settings.Instance.Image("printsetup_16.png"), 
		             null, 
		             null, OnUpdateUiElement);
		Commands.Add("File/Print Preview", "Creates a print preview for the current document", "P&rint Preview...", 
		             Settings.Instance.Image("printpreview_16.png"), 
		             null, 
		             null, OnUpdateUiElement);
		Commands.Add("File/Print", "Prints the current document", "&Print", 
		             Settings.Instance.Image("print_16.png"), 
		             new Keys[] { Keys.Control | Keys.P }, 
		             null, OnUpdateUiElement);
		Commands.Add("File/File Properties", "Displays the current document's file properties", "File Properties...", 
		             null, 
		             null, 
		             OnFileFileProperties, OnUpdateUiElement);
		Commands.Add("File/Exit", "Exits the application", "E&xit", 
		             null, 
		             null, 
		             OnFileExit, OnUpdateUiElement);

		Commands.Add("Edit/Undo", "Undoes the last operation", "&Undo",
		             Settings.Instance.Image("undo_16.png"),
		             new Keys[] { Keys.Control | Keys.Z }, 
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Redo", "Redoes the last operation", "&Redo",
		             Settings.Instance.Image("redo_16.png"),
		             new Keys[] { Keys.Control | Keys.Y },
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Cut", "Cuts the current selection to the clipboard", "Cu&t",
		             Settings.Instance.Image("cut_16.png"),
		             new Keys[] { Keys.Control | Keys.X },
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Copy", "Copies the current selection to the clipboard", "&Copy",
		             Settings.Instance.Image("copy_16.png"),
		             new Keys[] { Keys.Control | Keys.C },
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Paste", "Pastes the clipboard to the current document", "&Paste",
		             Settings.Instance.Image("paste_16.png"),
		             new Keys[] { Keys.Control | Keys.V },
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Delete", "Deletes the current selection", "&Delete",
		             Settings.Instance.Image("delete_16.png"),
		             new Keys[] { Keys.Delete },
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Select All", "Selects all of the current document", "Select &All",
		             null,
		             new Keys[] { Keys.Control | Keys.A },
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Insert File", "Inserts the specified file into the current document", "&File",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Insert Pattern", "Inserts the specified pattern into the current document", "&Pattern",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("Edit/Preferences", "Displayes the preferences dialog", "&Preferences...",
		             Settings.Instance.Image("options_16.png"),
		             null,
		             OnEditPreferences, OnUpdateUiElement);

		Commands.Add("View/Address Radix", null, null,
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Data Radix", null, null,
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To", null, null,
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To Top", "Moves the insert caret to the top of the document", "Top",
		             null,
		             new Keys[] { Keys.Control | Keys.Home },
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To Bottom", "Moves the insert caret to the bottom of the document", "Bottom",
		             null,
		             new Keys[] { Keys.Control | Keys.End },
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To Selection Start", "Moves the insert caret to the beginning of the selection", "Selection Start",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To Selection End", "Moves the insert caret to the end of the selection", "Selection End",
		             null, 
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To Address", "Moves the insert caret to the specified address", "Address",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Go To Selection As Address", "Moves the insert caret to the address in teh current selection", "Selection As Address",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Bytes", "Views the document as bytes", "Bytes",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Words", "Views the document as words", "Words",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Double Words", "Views the document as double words", "Double Words",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Quad Words", "Views the document as quad words", "Quad Words",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Little Endian", "Views the document as little endian", "Little Endian",
		             null,
		             null,
		             null, OnUpdateUiElement);
		Commands.Add("View/Big Endian", "Views the document as big endian", "Big Endian",
		             null,
		             null,
		             null, OnUpdateUiElement);
		

		Commands.Add("Window/Split", "Splits the current view", "&Split",
		             Settings.Instance.Image("split_16.png"),
		             null,
		             OnWindowSplit, OnUpdateUiElement);
		Commands.Add("Window/Duplicate", "Duplicates the current view", "&Duplicate",
		             Settings.Instance.Image("duplicate_16.png"),
		             null,
		             OnWindowDuplicate, OnUpdateUiElement);
		Commands.Add("Window/Open Windows", null, null,
		             null,
		             null,
		             OnWindowOpenWindows, OnUpdateUiElement);

		Commands.Add("Help/About", "Displays the about box", "About",
		             null,
		             null,
		             OnHelpAbout, OnUpdateUiElement);
	}
	
	private void CreateMenus()
	{
		ToolStripMenuItem mi;
		ToolStripMenuItem mi2;

		mi = new ToolStripMenuItem("&File");
		mi.DropDownItems.Add(CreateMenuItem("File/New"));
		mi.DropDownItems.Add(CreateMenuItem("File/Open"));
		mi.DropDownItems.Add(CreateMenuItem("File/Close"));
		mi.DropDownItems.Add(CreateMenuItem("File/Close All"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("File/Save"));
		mi.DropDownItems.Add(CreateMenuItem("File/Save As"));
		mi.DropDownItems.Add(CreateMenuItem("File/Save All"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("File/Print Setup"));
		mi.DropDownItems.Add(CreateMenuItem("File/Print Preview"));
		mi.DropDownItems.Add(CreateMenuItem("File/Print"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("File/File Properties"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("File/Exit"));
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&Edit");
		mi.DropDownItems.Add(CreateMenuItem("Edit/Undo"));
		mi.DropDownItems.Add(CreateMenuItem("Edit/Redo"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("Edit/Cut"));
		mi.DropDownItems.Add(CreateMenuItem("Edit/Copy"));
		mi.DropDownItems.Add(CreateMenuItem("Edit/Paste"));
		mi.DropDownItems.Add(CreateMenuItem("Edit/Delete"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("Edit/Select All"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi2 = new ToolStripMenuItem("Insert");
		mi2.DropDownItems.Add(CreateMenuItem("Edit/Insert File"));
		mi2.DropDownItems.Add(CreateMenuItem("Edit/Insert Pattern"));
		mi.DropDownItems.Add(mi2);
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi.DropDownItems.Add(CreateMenuItem("Edit/Preferences"));
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&View");
		AddressRadixMenu = new RadixMenu("Address Radix", "View/Address Radix", OnUiCommand);
		mi.DropDownItems.Add(AddressRadixMenu);
		DataRadixMenu = new RadixMenu("Data Radix", "View/Data Radix", OnUiCommand);
		mi.DropDownItems.Add(DataRadixMenu);
		mi.DropDownItems.Add(new ToolStripSeparator());
		mi2 = new ToolStripMenuItem("Go To", null, null, "View/Go To");
		mi2.DropDownItems.Add(CreateMenuItem("View/Go To Top"));
		mi2.DropDownItems.Add(CreateMenuItem("View/Go To Bottom"));
		mi2.DropDownItems.Add(CreateMenuItem("View/Go To Selection Start"));
		mi2.DropDownItems.Add(CreateMenuItem("View/Go To Selection End"));
		mi2.DropDownItems.Add(CreateMenuItem("View/Go To Address"));
		mi2.DropDownItems.Add(CreateMenuItem("View/Go To Selection As Address"));
		mi.DropDownItems.Add(mi2);
		mi.DropDownItems.Add(new ToolStripSeparator());
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&Window");
		mi.DropDownItems.Add(CreateMenuItem("Window/Split"));
		mi.DropDownItems.Add(CreateMenuItem("Window/Duplicate"));
		mi.DropDownItems.Add(new ToolStripSeparator());
		OpenWindowMenu = new OpenWindowMenu(mi, OnUiCommand);
		OpenWindowMenu.Name = "Window/Open Windows";
		OpenWindowMenu.IsPath = true;
		mi.DropDownItems.Add(OpenWindowMenu);
		MainMenuStrip.Items.Add(mi);

		mi = new ToolStripMenuItem("&Help");
		mi.DropDownItems.Add(CreateMenuItem("Help/About"));
		MainMenuStrip.Items.Add(mi);
	}
	
	private void CreateContextMenus()
	{
		ContextMenus = new ContextMenuStrip[Enum.GetValues(typeof(Menus)).Length];
		for(int i = 0; i < ContextMenus.Length; ++i)
			ContextMenus[i] = new ContextMenuStrip();
		
		ContextMenus[(int)Menus.SelectedDataContext].Items.Add(CreateMenuItem("Edit/Cut"));
		ContextMenus[(int)Menus.SelectedDataContext].Items.Add(CreateMenuItem("Edit/Copy"));
		ContextMenus[(int)Menus.SelectedDataContext].Items.Add(CreateMenuItem("Edit/Paste"));
		ContextMenus[(int)Menus.SelectedDataContext].Items.Add(new ToolStripSeparator());
		AddMenuItem(Menus.SelectedDataContext, "Go To", CreateMenuItem("View/Go To Top"));
		AddMenuItem(Menus.SelectedDataContext, "Go To", CreateMenuItem("View/Go To Bottom"));
		AddMenuItem(Menus.SelectedDataContext, "Go To", CreateMenuItem("View/Go To Selection As Address"));
		
		AddMenuItem(Menus.DataContext, String.Empty, CreateMenuItem("Edit/Select All"));
		AddMenuItem(Menus.DataContext, String.Empty, new ToolStripSeparator());
		AddMenuItem(Menus.DataContext, "Go To", CreateMenuItem("View/Go To Top"));
		AddMenuItem(Menus.DataContext, "Go To", CreateMenuItem("View/Go To Bottom"));
		AddMenuItem(Menus.DataContext, "Go To", CreateMenuItem("View/Go To Selection As Address"));
		
	}
	
	private void CreateToolBars()
	{
		FileToolStrip.Items.Add(CreateToolButton("File/New"));
		FileToolStrip.Items.Add(CreateToolButton("File/Open"));
		FileToolStrip.Items.Add(new ToolStripSeparator());
		FileToolStrip.Items.Add(CreateToolButton("File/Save"));
		FileToolStrip.Items.Add(CreateToolButton("File/Save All"));

		EditToolStrip.Items.Add(CreateToolButton("Edit/Undo"));
		EditToolStrip.Items.Add(CreateToolButton("Edit/Redo"));
		EditToolStrip.Items.Add(new ToolStripSeparator());
		EditToolStrip.Items.Add(CreateToolButton("Edit/Cut"));
		EditToolStrip.Items.Add(CreateToolButton("Edit/Copy"));
		EditToolStrip.Items.Add(CreateToolButton("Edit/Paste"));
		EditToolStrip.Items.Add(CreateToolButton("Edit/Delete"));

		ViewToolStrip.Items.Add(CreateToolButton("B", null, "View/Bytes", "Bytes"));
		ViewToolStrip.Items.Add(CreateToolButton("W", null, "View/Words", "Words"));
		ViewToolStrip.Items.Add(CreateToolButton("D", null, "View/Double Words", "Double Words"));
		ViewToolStrip.Items.Add(CreateToolButton("Q", null, "View/Quad Words", "Quad Words"));
		ViewToolStrip.Items.Add(new ToolStripSeparator());
		ViewToolStrip.Items.Add(CreateToolButton("LE", null, "View/Little Endian", "Little Endian"));
		ViewToolStrip.Items.Add(CreateToolButton("BE", null, "View/Big Endian", "Big Endian"));

		ToolStripPanel.Join(ViewToolStrip, 1);
		ToolStripPanel.Join(EditToolStrip, 1);
		ToolStripPanel.Join(FileToolStrip, 1);
	}
	
	public ToolStripMenuItem CreateMenuItem(string commandName)
	{
		Command cmd = Commands.Find(commandName);
		if(cmd == null)
			return null;
		
		ToolStripMenuItem i = new ToolStripMenuItem(cmd.Label);
		if(cmd.Image != null)
			i.Image = cmd.Image;
		i.Click += OnUiCommand;
		i.Name = cmd.Name;
		if(cmd.Shortcuts != null)
			i.ShortcutKeys = cmd.Shortcuts[0];
		return i;
	}

	public ToolStripMenuItem CreateMenuItem(string text, string image, string name, Keys shortcut)
	{
		ToolStripMenuItem i = new ToolStripMenuItem(text);
		if(image != null)
			i.Image = Settings.Instance.Image(image);
		i.Click += OnUiCommand;
		i.Name = name;
		i.ShortcutKeys = shortcut;
		return i;
	}
	
	public ToolStripButton CreateToolButton(string commandName)
	{
		Command cmd = Commands.Find(commandName);
		if(cmd == null)
			return null;
		ToolStripButton i = new ToolStripButton();
		if(cmd.Image != null)
			i.Image = cmd.Image;
		i.Click += OnUiCommand;
		i.Name = cmd.Name;
		i.ToolTipText = cmd.Label;
		return i;
	}

	public ToolStripButton CreateToolButton(string text, string image, string name, string tooltip)
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
			if((byText && i.Text.Replace("&", String.Empty) == name) || (!byText && i.Name == name))
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
		Command cmd = (Command)sender;

		ToolStripItem[] items = FindToolStripItems(MainMenuStrip.Items, cmd.Name, true, false);
		foreach(ToolStripMenuItem i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}

		items = FindToolStripItems(FileToolStrip.Items, cmd.Name, true, false);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}

		items = FindToolStripItems(EditToolStrip.Items, cmd.Name, true, false);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}

		items = FindToolStripItems(ViewToolStrip.Items, cmd.Name, true, false);
		foreach(ToolStripButton i in items)
		{
			i.Enabled = cmd.Enabled;
			i.Checked = cmd.Checked;
		}
	}

	public void OnUiCommand(object sender, EventArgs e)
	{
		ToolStripItem item = (ToolStripItem)sender;
		Commands[item.Name].Invoke(item.Tag);
	}
	
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if(base.ProcessCmdKey(ref msg, keyData))
		   return true;
		
		Command cmd = Commands.FindShortcut(keyData);
		if(cmd != null)
		{
			cmd.Invoke(null);
			return true;
		}
		
		return false;
	}
	

	protected void OnIdle(object sender, EventArgs e)
	{
		bool haveChild = (_TabbedGroups.ActiveTabPage != null);
		bool haveSelection = haveChild ? ((HexViewForm)_TabbedGroups.ActiveTabPage).View.Selection.Length > 0 : false;
		HexView view = haveChild ? ((HexViewForm)_TabbedGroups.ActiveTabPage).View : null;
		
		Commands["File/Close"].Enabled = haveChild;
		Commands["File/Close All"].Enabled = haveChild;
		Commands["File/Save"].Enabled = haveChild && view.Document.IsModified;
		Commands["File/Save As"].Enabled = haveChild;
		Commands["File/Save All"].Enabled = haveChild;
		Commands["File/Print Setup"].Enabled = haveChild;
		Commands["File/Print Preview"].Enabled = haveChild;
		Commands["File/Print"].Enabled = haveChild;
		Commands["File/File Properties"].Enabled = haveChild;

		Commands["Edit/Undo"].Enabled = haveChild && view.Document.CanUndo;
		Commands["Edit/Redo"].Enabled = haveChild && view.Document.CanRedo;
		Commands["Edit/Cut"].Enabled = haveSelection;
		Commands["Edit/Copy"].Enabled = haveSelection;
		Commands["Edit/Paste"].Enabled = haveChild;
		Commands["Edit/Delete"].Enabled = haveSelection;
		Commands["Edit/Select All"].Enabled = haveChild;
		Commands["Edit/Insert File"].Enabled = haveChild;
		Commands["Edit/Insert Pattern"].Enabled = haveChild;

		Commands["View/Address Radix"].Enabled = haveChild;
		Commands["View/Data Radix"].Enabled = haveChild;
		Commands["View/Go To"].Enabled = haveChild;
		Commands["View/Go To Top"].Enabled = haveChild;
		Commands["View/Go To Bottom"].Enabled = haveChild;
		Commands["View/Go To Selection Start"].Enabled = haveChild;
		Commands["View/Go To Selection End"].Enabled = haveChild;
		Commands["View/Go To Address"].Enabled = haveChild;
		Commands["View/Go To Selection As Address"].Enabled = haveChild;
		Commands["View/Bytes"].Enabled = haveChild;
		Commands["View/Bytes"].Checked = (haveChild && ActiveView.BytesPerWord == 1);
		Commands["View/Words"].Enabled = haveChild;
		Commands["View/Words"].Checked = (haveChild && ActiveView.BytesPerWord == 2);
		Commands["View/Double Words"].Enabled = haveChild;
		Commands["View/Double Words"].Checked = (haveChild && ActiveView.BytesPerWord == 4);
		Commands["View/Quad Words"].Enabled = haveChild;
		Commands["View/Quad Words"].Checked = (haveChild && ActiveView.BytesPerWord == 8);
		Commands["View/Little Endian"].Enabled = haveChild;
		Commands["View/Little Endian"].Checked = (haveChild && ActiveView.Endian == Endian.Little);
		Commands["View/Big Endian"].Enabled = haveChild;
		Commands["View/Big Endian"].Checked = (haveChild && ActiveView.Endian == Endian.Big);

		Commands["Window/Split"].Enabled = haveChild;
		Commands["Window/Duplicate"].Enabled = haveChild;
		
		if(view != null)
		{
			AddressRadixMenu.SelectedRadix = (int)view.AddressRadix;
			DataRadixMenu.SelectedRadix = (int)view.DataRadix;
			AddressLabel.Text = String.Format("Addr: {0}", view.Selection.ToString());
			if(view.EditMode == EditMode.Insert)
				EditModeLabel.Text = "INS";
			else
				EditModeLabel.Text = "OVR";
		}
		else
			AddressLabel.Text = "Addr:";
		
		EditModeLabel.Enabled = haveChild;
		ModifiedLabel.Enabled = haveChild && view.Document.IsModified;
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		PluginManager.LoadPlugins();
	}
	
	protected override void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		SplashScreen.Hide();
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
			OpenWindowMenu.SetSelectedItem(_TabbedGroups.ActiveTabPage);
			UpdateWindowTitle(_TabbedGroups.ActiveTabPage.Title);
			Commands.Merge(((HexViewForm)_TabbedGroups.ActiveTabPage).Commands);
		}
		else
		{
			OpenWindowMenu.SelectedItem = null;
			Commands.RevertMerge();
		}
		
		if(ActiveViewChanged != null)
			ActiveViewChanged(this, new EventArgs());
	}

	protected void OnTabbedGroupsPageCloseRequest(object sender, Crownwood.DotNetMagic.Controls.TGCloseRequestEventArgs e)
	{
		HexViewForm form = (HexViewForm)e.TabPage;
		
		if(Documents[form.View.Document].Count <= 1 && form.View.Document.IsModified)
		{
			if(MessageBox.Show("Do you want to close the document without saving changes?", 
			                   "Document Modified", 
			                   MessageBoxButtons.YesNo, 
			                   MessageBoxIcon.Question) == DialogResult.No)
			{
				e.Cancel = true;
				return;
			}
		}

		OnViewClosed(new ViewClosedEventArgs(form));
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
		{
			Document doc = OpenDocument(filename);
			if(doc != null)
				CreateView(doc);
		}
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
		Document doc = CreateDocument();
		if(doc != null)
			CreateView(doc);
	}

	private void OnFileOpen(object sender, EventArgs args)
	{
		OpenFileDialog ofd = new OpenFileDialog();
		ofd.Title = "Select File";
		ofd.Filter = "All Files (*.*)|*.*";

		if(ofd.ShowDialog() == DialogResult.OK)
		{
			Document doc = OpenDocument(ofd.FileName);
			if(doc != null)
				CreateView(doc);
		}
	}

	protected void OnFileSave(object sender, EventArgs e)
	{
		if(ActiveView != null)
		{
			PieceBuffer.SavePlan plan = ActiveView.Document.BuildSavePlan();
			ConfirmSaveDialog dlg = new ConfirmSaveDialog(ActiveView.Document, plan,  null);
			dlg.ShowDialog();
		}
	}

	protected void OnFileSaveAs(object sender, EventArgs e)
	{
		if(ActiveView != null)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Title = "Select File";
			sfd.Filter = "All Files (*.*)|*.*";

			if(sfd.ShowDialog() == DialogResult.OK)
			{
				PieceBuffer.SavePlan plan = ActiveView.Document.BuildSavePlan();
				ConfirmSaveDialog dlg = new ConfirmSaveDialog(ActiveView.Document, plan, sfd.FileName);
				dlg.ShowDialog();
			}
		}
	}

	protected void OnFileClose(object sender, EventArgs e)
	{
		if(_TabbedGroups.ActiveTabPage != null)
			CloseView((HexViewForm)_TabbedGroups.ActiveTabPage);
	}
	
	protected void OnFileCloseAll(object sender, EventArgs e)
	{
		CloseAllViews();
	}
	
	protected void OnFileFileProperties(object sender, EventArgs e)
	{
		if(ActiveView != null && ActiveView.Document.FileName != null)
		{
			FilePropertiesDialog dlg = new FilePropertiesDialog(ActiveView.Document.FileName);
			dlg.StartPosition = FormStartPosition.CenterParent;
			dlg.ShowDialog();
		}
	}
	
	private void OnEditPreferences(object sender, EventArgs e)
	{
		SettingsDialog dlg = new SettingsDialog(this);
		dlg.StartPosition = FormStartPosition.CenterParent;
		dlg.ShowDialog();
	}

	private void OnWindowDuplicate(object sender, EventArgs e)
	{
		CreateView(ActiveView.Document);
	}

	private void OnWindowSplit(object sender, EventArgs e)
	{
		((HexViewForm)_TabbedGroups.ActiveTabPage).Split();
	}
	
	protected void OnWindowOpenWindows(object sender, EventArgs e)
	{
		((Crownwood.DotNetMagic.Controls.TabControl)_TabbedGroups.ActiveLeaf.GroupControl).SelectedTab = (HexViewForm)OpenWindowMenu.SelectedItem.Tag;
	}

	protected void OnSelectionContextMenuDefineField(object sender, EventArgs e)
	{
		// TODO: StructureTree:

//		TreeListViewItem n = structurePanel.Tree.Items.Add("New Field");
//		n.Tag = new Record("New Field", (ulong)((HexViewForm)ActiveMdiChild).View.Selection.Start * 8,
//		                   (ulong)(((HexViewForm)ActiveMdiChild).View.Selection.End - ((HexViewForm)ActiveMdiChild).View.Selection.Start) / 8, 1);
//		n.BeginEdit();
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
		Console.WriteLine(String.Format("Progress notification timer TICK: {0} -> {1}", 
		                                CurrentProgressNotification.Index, next));
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

	
	/**********************************************
	      Document Collection Functions
	***********************************************/
	
	public Document OpenDocument(string filename)
	{
		Document document = null;
		foreach(Document doc in Documents.Keys)
		{
			if(doc.FileName == filename)
			{
				document = doc;
				break;
			}
		}
		
		if(document == null)
		{
			try
			{
				document = new Document(filename);
				Documents.Add(document, new List<HexViewForm>());
			}
			catch(System.Security.SecurityException ex)
			{
				MessageBox.Show(this, ex.Message, "Permission Denied", 
				                MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch(UnauthorizedAccessException ex)
			{
				MessageBox.Show(this, ex.Message, "Permission Denied", 
				                MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		return document;
	}
	
	public Document CreateDocument()
	{
		Document doc = new Document();
		Documents.Add(doc, new List<HexViewForm>());
		return doc;
	}
	
	public void CloseDocument(Document doc)
	{
		foreach(HexViewForm form in Documents[doc])
			CloseView(form);
		
		Documents[doc].Clear();
		Documents.Remove(doc);
	}
	
	public void CreateView(Document doc)
	{
		HexViewForm form = new HexViewForm(doc);
		if(doc.FileName != null)
		{
			string[] filenameParts = doc.FileName.Split(new char[] {Path.PathSeparator, 
			                                                        Path.DirectorySeparatorChar, 
			                                                        Path.AltDirectorySeparatorChar});
			form.Title = filenameParts[filenameParts.Length - 1];
		}
		else
			form.Title = "New File";
		form.Image = Settings.Instance.Image("document_16.png");
		if(form.View.Document.FileName != null)
			OpenWindowMenu.Add(form.View.Document.FileName, form);
		else
			OpenWindowMenu.Add(form.Title, form);
		_TabbedGroups.ActiveLeaf.TabPages.Add(form);
		((Crownwood.DotNetMagic.Controls.TabControl)_TabbedGroups.ActiveLeaf.GroupControl).SelectedTab = form;
		Documents[doc].Add(form);
	}
		
	public void CloseView(HexViewForm form)
	{
		((Crownwood.DotNetMagic.Controls.TabControl)_TabbedGroups.ActiveLeaf.GroupControl).SelectedTab = form;
		_TabbedGroups.ActiveLeaf.TabPages.Remove(form);
		OnViewClosed(new ViewClosedEventArgs(form));
	}
	
	public void CloseAllViews()
	{
		while(_TabbedGroups.ActiveTabPage != null)
			CloseView((HexViewForm)_TabbedGroups.ActiveTabPage);
	}
	
	public void OnViewClosed(ViewClosedEventArgs e)
	{
		Documents[e.Form.View.Document].Remove(e.Form);
		if(Documents[e.Form.View.Document].Count == 0)
		{
			e.Form.View.Document.Close();
			Documents.Remove(e.Form.View.Document);
		}
		OpenWindowMenu.Remove(e.Form);
	}
	
	public IEnumerator<Document> GetEnumerator()
	{
		return Documents.Keys.GetEnumerator();
	}
	
	IEnumerator IEnumerable.GetEnumerator()
	{
		return Documents.Keys.GetEnumerator();
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
	
	public ContextMenuStrip GetContextMenu(Menus menu)
	{
		return ContextMenus[(int)menu];
	}

	void IPluginHost.AddWindow(Control control, string name, Image image, 
	                           DefaultWindowPosition defaultPosition, bool visibleByDefault)
	{	
		Content content;
		if(image != null)
		{
			WindowImageList.Images.Add(image);
			int imageIndex = WindowImageList.Images.Count - 1;
			content = _dockingManager.Contents.Add(control, name, WindowImageList, imageIndex);
			ToolStripItem item = new ToolStripMenuItem(name, image);
			item.Tag = content;
			item.Click += OnViewWindowsMenuItemClicked;
			AddMenuItem(Menus.Main, "View", item); 
		}
		else
		{
			content = _dockingManager.Contents.Add(control, name);
			ToolStripItem item = new ToolStripMenuItem(name);
			item.Tag = content;
			item.Click += OnViewWindowsMenuItemClicked;
			AddMenuItem(Menus.Main, "View", item); 
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
	
	public void BringToFront(Control control)
	{
		foreach(Content c in _dockingManager.Contents)
			if(c.Control == control)
				c.BringToFront();
	}

	public void AddMenuItem(Menus menu, string path, ToolStripItem menuItem)
	{
		path = path.Replace("&", String.Empty);
		string[] parts = path.Split(new char[] {'/', '\\'});

		int i = 0;
		ToolStripMenuItem item = null;
		ToolStripItemCollection m = null;
		if(menu == Menus.Main)
			m = MainMenuStrip.Items;
		else
			m = ContextMenus[(int)menu].Items;
		for(i = 0; i < parts.Length; ++i)
		{
			ToolStripItem[] items = FindToolStripItems(m, parts[i], false, true);
			if(items.Length == 1)
				m = ((ToolStripMenuItem)items[0]).DropDownItems;
			else
				break;
		}

		for(; i < parts.Length; ++i)
		{
			if(parts[i].Length > 0)
			{
				item = (ToolStripMenuItem)m.Add(parts[i]);
				m = item.DropDownItems;
			}
		}
		
		m.Add(menuItem);
	}
}

