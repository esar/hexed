using System;
using System.Drawing;
using System.Windows.Forms;




class HexViewForm : Form
{


	public HexView View							= null;
	public HexView SplitView					= null;
	public Splitter Splitter					= null;
	public Document Document;
	
	public CommandSet Commands = new CommandSet();
	
	public HexViewForm(Document doc)
	{
		Commands.Add("EditUndo", OnEditUndo);
		Commands.Add("EditRedo", OnEditRedo);
		Commands.Add("SelectionDefineField", OnSelectionDefineField);
		Commands.Add("ViewAddressRadix", OnViewAddressRadix);
		Commands.Add("ViewDataRadix", OnViewDataRadix);
		Commands.Add("ViewGoToTop", OnViewGoToTop);
		Commands.Add("ViewGoToBottom", OnViewGoToBottom);
		Commands.Add("ViewGoToSelectionStart", OnViewGoToSelectionStart);
		Commands.Add("ViewGoToSelectionEnd", OnViewGoToSelectionEnd);
		Commands.Add("ViewGoToAddress", OnViewGoToAddress);
		Commands.Add("ViewGoToSelectionAsAddress", OnViewGoToSelectionAsAddress);
		
		View = new HexView(doc);
		View.Dock = DockStyle.Fill;
		View.ContextMenu += OnViewContextMenu;
		Controls.Add(View);

		Size = new Size(580, 300);

		Document = doc;
	}

	public void Split()
	{
		if(SplitView != null)
		{
			Controls.Remove(SplitView);
			Controls.Remove(Splitter);
			SplitView = null;
			Splitter = null;
		}
		else
		{
			Splitter = new Splitter();
			Splitter.Dock = DockStyle.Bottom;
			Controls.Add(Splitter);
			
			SplitView = new HexView(Document);
			SplitView.Dock = DockStyle.Bottom;
			Controls.Add(SplitView);
			
			SplitView.Height = View.Height / 2;
		}
	}
	
	protected void OnViewContextMenu(object sender, HexView.ContextMenuEventArgs e)
	{
		switch(e.Hit.Type)
		{
			case HexView.HexViewHit.HitType.DataSelection:
			case HexView.HexViewHit.HitType.AsciiSelection:
				ContextMenuStrip menu = new ContextMenuStrip();
				menu.Items.Add(HexEdApp.CreateMenuItem("Cut", "cut_16.png", "EditCut", Keys.Control | Keys.X));
				menu.Items.Add(HexEdApp.CreateMenuItem("Copy", "copy_16.png", "EditCopy", Keys.Control | Keys.C));
				menu.Items.Add(HexEdApp.CreateMenuItem("Paste", "paste_16.png", "EditPaste", Keys.Control | Keys.V));
				menu.Items.Add(new ToolStripSeparator());
				menu.Items.Add(HexEdApp.CreateMenuItem("Define Field", null, "SelectionDefineField", Keys.None));
				menu.Items.Add(HexEdApp.CreateMenuItem("Go To Selection As Address", null, "ViewGoToSelectionAsAddress", Keys.None));
				menu.Show(this, e.Position);
//				mi.Click += new EventHandler(OnSelectionContextMenuDefineField);
				break;
		}
	}
	
	protected void OnEditUndo(object sender, EventArgs e)
	{
		MessageBox.Show("Undo");
	}

	protected void OnEditRedo(object sender, EventArgs e)
	{
		MessageBox.Show("Redo");
	}

	protected void OnSelectionDefineField(object sender, EventArgs e)
	{
//		TreeListViewItem n = structurePanel.Tree.Items.Add("New Field");
//		n.Tag = new Record("New Field", (ulong)((HexViewForm)ActiveMdiChild).View.Selection.Start * 8,
//		                   (ulong)(((HexViewForm)ActiveMdiChild).View.Selection.End - ((HexViewForm)ActiveMdiChild).View.Selection.Start) / 8, 1);
//		n.BeginEdit();
	}

	protected void OnViewAddressRadix(object sender, CommandEventArgs args)
	{
		View.AddressRadix = (int)args.Arg;
	}

	protected void OnViewDataRadix(object sender, CommandEventArgs args)
	{
		View.DataRadix = (int)args.Arg;
	}

	protected void OnViewGoToTop(object sender, EventArgs e)
	{
		View.ScrollToAddress(0);
	}

	protected void OnViewGoToBottom(object sender, EventArgs e)
	{
		View.ScrollToAddress(Document.Buffer.Length - 1);
	}

	protected void OnViewGoToSelectionStart(object sender, EventArgs e)
	{
		View.ScrollToAddress(View.Selection.Start);
	}

	protected void OnViewGoToSelectionEnd(object sender, EventArgs e)
	{
		View.ScrollToAddress(View.Selection.End);
	}

	protected void OnViewGoToAddress(object sender, EventArgs e)
	{
		AddressDialog dlg = new AddressDialog();
		if(dlg.ShowDialog() == DialogResult.OK)
			View.ScrollToAddress(dlg.Address);
	}

	protected void OnViewGoToSelectionAsAddress(object sender, EventArgs e)
	{
		View.ScrollToAddress(View.Selection.AsInteger());
	}
}
