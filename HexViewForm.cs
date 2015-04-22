using System;
using System.Drawing;
using System.Windows.Forms;

#if DOTNETMAGIC
using MagicTabPage = Crownwood.DotNetMagic.Controls.TabPage;
#else
using MagicTabPage = Crownwood.Magic.Controls.TabPage;
#endif


class HexViewForm : MagicTabPage
{


	public HexView View							= null;
	public HexView SplitView					= null;
	public Splitter Splitter					= null;
	public Document Document;
	
	public CommandSet Commands = new CommandSet();
	
	public HexViewForm(Document doc)
	{
		Commands.Add("Edit/Undo", OnEditUndo);
		Commands.Add("Edit/Redo", OnEditRedo);
		Commands.Add("Edit/Cut", OnEditCut);
		Commands.Add("Edit/Copy", OnEditCopy);
		Commands.Add("Edit/Paste", OnEditPaste);
		Commands.Add("Edit/Insert File", OnEditInsertFile);
		Commands.Add("Edit/Insert Pattern", OnEditInsertPattern);
		Commands.Add("Edit/Select All", OnEditSelectAll);
		Commands.Add("View/Address Radix", OnViewAddressRadix);
		Commands.Add("View/Data Radix", OnViewDataRadix);
		Commands.Add("View/Go To Top", OnViewGoToTop);
		Commands.Add("View/Go To Bottom", OnViewGoToBottom);
		Commands.Add("View/Go To Selection Start", OnViewGoToSelectionStart);
		Commands.Add("View/Go To Selection End", OnViewGoToSelectionEnd);
		Commands.Add("View/Go To Address", OnViewGoToAddress);
		Commands.Add("View/Go To Selection As Address", OnViewGoToSelectionAsAddress);
		Commands.Add("View/Bytes", OnViewBytes);
		Commands.Add("View/Words", OnViewWords);
		Commands.Add("View/Double Words", OnViewDwords);
		Commands.Add("View/Quad Words", OnViewQwords);
		Commands.Add("View/Little Endian", OnViewLittleEndian);
		Commands.Add("View/Big Endian", OnViewBigEndian);
		
		View = new HexView(doc);
		View.Dock = DockStyle.Fill;
		View.ContextMenu += OnViewContextMenu;
		Controls.Add(View);
		
		Size = new Size(800, 300);

		Document = doc;
	}

/*	protected override void OnActivated(EventArgs e)
	{
		// TODO: MONO: Why does mono not call Activate() itself. Without calling activate
		//             the parent window doesn't get a call to ActivateMdiChild() and as a
		//             result we don't merge the commandset so none of the command handlers
		//             ever get called
		Activate();
		
		base.OnActivated(e);
	}*/
	
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
			case HexView.HexViewHit.HitType.Address:
				if(HexEdApp.Instance.GetContextMenu(Menus.AddressContext).Items.Count > 0)
					HexEdApp.Instance.GetContextMenu(Menus.AddressContext).Show(this, e.Position);
				break;
			case HexView.HexViewHit.HitType.Data:
			case HexView.HexViewHit.HitType.Ascii:
				if(HexEdApp.Instance.GetContextMenu(Menus.DataContext).Items.Count > 0)
					HexEdApp.Instance.GetContextMenu(Menus.DataContext).Show(this, e.Position);
				break;
			case HexView.HexViewHit.HitType.DataSelection:
			case HexView.HexViewHit.HitType.AsciiSelection:
				if(HexEdApp.Instance.GetContextMenu(Menus.SelectedDataContext).Items.Count > 0)
					HexEdApp.Instance.GetContextMenu(Menus.SelectedDataContext).Show(this, e.Position);
				break;
		}
	}
	
	protected void OnEditUndo(object sender, EventArgs e)
	{
		Document.Undo();
	}

	protected void OnEditRedo(object sender, EventArgs e)
	{
		Document.Redo();
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
			View.Document.InsertFile(View.Selection.BufferRange.Start, View.Selection.BufferRange.End, dlg.FileName, 0, info.Length);
		}
	}
	
	protected void OnEditInsertPattern(object sender, EventArgs e)
	{
		PatternDialog dlg = new PatternDialog();
		dlg.Text = "Insert Pattern";
		if(dlg.ShowDialog() == DialogResult.OK)
		{
			byte[] pattern = dlg.Pattern;
			if(pattern.Length > 0)
				View.Document.FillConstant(View.Selection.BufferRange.Start, View.Selection.BufferRange.End, pattern, View.Selection.Length / 8);
		}
	}
	
	protected void OnEditSelectAll(object sender, EventArgs e)
	{
		View.Selection.Set(0, View.Document.Length * 8);
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
		View.Selection.Set((Document.Length - 1) * 8, (Document.Length - 1) * 8);
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
		View.WordsPerGroup = 8;
	}
	
	protected void OnViewWords(object sender, EventArgs e)
	{
		View.BytesPerWord = 2;
		View.WordsPerGroup = 4;
	}
	
	protected void OnViewDwords(object sender, EventArgs e)
	{
		View.BytesPerWord = 4;
		View.WordsPerGroup = 2;
	}
	
	protected void OnViewQwords(object sender, EventArgs e)
	{
		View.BytesPerWord = 8;
		View.WordsPerGroup = 1;
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
