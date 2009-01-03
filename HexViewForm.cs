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
		Commands.Add("EditCut", OnEditCut);
		Commands.Add("EditCopy", OnEditCopy);
		Commands.Add("EditPaste", OnEditPaste);
		Commands.Add("EditInsertFile", OnEditInsertFile);
		Commands.Add("EditInsertPattern", OnEditInsertPattern);
		Commands.Add("EditSelectAll", OnEditSelectAll);
		Commands.Add("SelectionDefineField", OnSelectionDefineField);
		Commands.Add("ViewAddressRadix", OnViewAddressRadix);
		Commands.Add("ViewDataRadix", OnViewDataRadix);
		Commands.Add("ViewGoToTop", OnViewGoToTop);
		Commands.Add("ViewGoToBottom", OnViewGoToBottom);
		Commands.Add("ViewGoToSelectionStart", OnViewGoToSelectionStart);
		Commands.Add("ViewGoToSelectionEnd", OnViewGoToSelectionEnd);
		Commands.Add("ViewGoToAddress", OnViewGoToAddress);
		Commands.Add("ViewGoToSelectionAsAddress", OnViewGoToSelectionAsAddress);
		Commands.Add("ViewBytes", OnViewBytes);
		Commands.Add("ViewWords", OnViewWords);
		Commands.Add("ViewDwords", OnViewDwords);
		Commands.Add("ViewQwords", OnViewQwords);
		Commands.Add("ViewLittleEndian", OnViewLittleEndian);
		Commands.Add("ViewBigEndian", OnViewBigEndian);
		
		View = new HexView(doc);
		View.Dock = DockStyle.Fill;
		View.ContextMenu += OnViewContextMenu;
		Controls.Add(View);
		
		Size = new Size(800, 300);

		Document = doc;
	}

	protected override void OnActivated(EventArgs e)
	{
		// TODO: MONO: Why does mono not call Activate() itself. Without calling activate
		//             the parent window doesn't get a call to ActivateMdiChild() and as a
		//             result we don't merge the commandset so none of the command handlers
		//             ever get called
		Activate();
		
		base.OnActivated(e);
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
				break;
		}
	}
	
	protected void OnEditUndo(object sender, EventArgs e)
	{
		Document.Buffer.Undo();
	}

	protected void OnEditRedo(object sender, EventArgs e)
	{
		Document.Buffer.Redo();
	}

	protected void OnEditCopy(object sender, EventArgs e)
	{
		View.Copy();
	}
	
	protected void OnEditCut(object sender, EventArgs e)
	{
		View.Cut();
	}
	
	protected void OnEditPaste(object sender, EventArgs e)
	{
		View.Paste();
	}
	
	protected void OnEditInsertFile(object sender, EventArgs e)
	{
		OpenFileDialog dlg = new OpenFileDialog();
		if(dlg.ShowDialog() == DialogResult.OK)
		{
			System.IO.FileInfo info = new System.IO.FileInfo(dlg.FileName);
			PieceBuffer.Mark a = View.Document.Buffer.CreateMarkAbsolute(View.Selection.Start / 8);
			PieceBuffer.Mark b = View.Document.Buffer.CreateMarkAbsolute(View.Selection.End / 8);
			View.Document.Buffer.InsertFile(a, b, dlg.FileName, 0, info.Length);
			View.Document.Buffer.DestroyMark(a);
			View.Document.Buffer.DestroyMark(b);
		}
	}
	
	protected void OnEditInsertPattern(object sender, EventArgs e)
	{
		PieceBuffer.Mark a = View.Document.Buffer.CreateMarkAbsolute(View.Selection.Start / 8);
		PieceBuffer.Mark b = View.Document.Buffer.CreateMarkAbsolute(View.Selection.End / 8);
		View.Document.Buffer.FillConstant(a, b, 0xFF, (View.Selection.End - View.Selection.Start) / 8);
		View.Document.Buffer.DestroyMark(a);
		View.Document.Buffer.DestroyMark(b);
	}
	
	protected void OnEditSelectAll(object sender, EventArgs e)
	{
		View.Selection.Set(0, View.Document.Buffer.Length * 8);
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
		View.AddressRadix = (uint)(int)args.Arg;
	}

	protected void OnViewDataRadix(object sender, CommandEventArgs args)
	{
		View.DataRadix = (uint)(int)args.Arg;
	}

	protected void OnViewGoToTop(object sender, EventArgs e)
	{
		View.ScrollToAddress(0);
	}

	protected void OnViewGoToBottom(object sender, EventArgs e)
	{
		View.Selection.Set((Document.Buffer.Length - 1) * 8, (Document.Buffer.Length - 1) * 8);
		View.EnsureVisible(View.Selection.Start);
	}

	protected void OnViewGoToSelectionStart(object sender, EventArgs e)
	{
		View.Selection.Set(0, 0);
		View.EnsureVisible(0);
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
	
	protected void OnViewBytes(object sender, EventArgs e)
	{
		View.BytesPerWord = 1;
	}
	
	protected void OnViewWords(object sender, EventArgs e)
	{
		View.BytesPerWord = 2;
	}
	
	protected void OnViewDwords(object sender, EventArgs e)
	{
		View.BytesPerWord = 4;
	}
	
	protected void OnViewQwords(object sender, EventArgs e)
	{
		View.BytesPerWord = 8;
	}
	
	protected void OnViewLittleEndian(object sender, EventArgs e)
	{
		View.Endian = Endian.Little;
	}

	protected void OnViewBigEndian(object sender, EventArgs e)
	{
		View.Endian = Endian.Big;
	}
}
