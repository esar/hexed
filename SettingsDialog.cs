/*
	This file is part of TortoiseHg

	Copyright (C) 2006, 2007  Stephen Robinson <stephen@tortoisehg.esar.org.uk>

	TortoiseHg is free software; you can redistribute it and/or modify
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
using System.IO;
using System.Drawing;
using System.Windows.Forms;


class EditorSettingsPage : SettingsPage
{
	Label Description = new Label();
	
	public EditorSettingsPage()
	{
		Text = "Editor";
		
		Description.Text = "Change the settings on the sub pages to set your editor preferences.";
		Description.Dock = DockStyle.Fill;
		Description.Padding = new Padding(0, 5, 0, 0);
		Controls.Add(Description);
	}
}

class EditorLayoutSettingsPage : SettingsPage
{
	public EditorLayoutSettingsPage()
	{
		Text = "Layout";
	}
}

class EditorAppearanceSettingsPage : SettingsPage
{
	TableLayoutPanel Panel = new TableLayoutPanel();
	ListBox ElementList = new ListBox();
	Label ForeColorLabel = new Label();
	ComboBox ForeColorCombo = new ColourComboBox();
	Label BackColorLabel = new Label();
	ComboBox BackColorCombo = new ColourComboBox();
	Label BorderLeftColorLabel = new Label();
	ComboBox BorderLeftColorCombo = new ColourComboBox();
	Label BorderRightColorLabel = new Label();
	ComboBox BorderRightColorCombo = new ColourComboBox();
	Label BorderTopColorLabel = new Label();
	ComboBox BorderTopColorCombo = new ColourComboBox();
	Label BorderBottomColorLabel = new Label();
	ComboBox BorderBottomColorCombo = new ColourComboBox();
	
	
	public EditorAppearanceSettingsPage()
	{
		Text = "Appearance";
		
		ElementList.Items.Add("Address");
		ElementList.Items.Add("Address Highlight");
		ElementList.Items.Add("Data");
		ElementList.Items.Add("ASCII");
		
		ForeColorLabel.Text = "Foreground:";
		BackColorLabel.Text = "Background:";
		BorderLeftColorLabel.Text = "Left Border:";
		BorderRightColorLabel.Text = "Right Border:";
		BorderTopColorLabel.Text = "Top Border:";
		BorderBottomColorLabel.Text = "Bottom Border:";
		
		Panel.ColumnCount = 3;
		Panel.RowCount = 6;
		Panel.Controls.Add(ElementList, 0, 0);
		Panel.SetRowSpan(ElementList, 6);
		Panel.Controls.Add(ForeColorLabel, 1, 0);
		Panel.Controls.Add(ForeColorCombo, 1, 1);
		Panel.Controls.Add(BackColorLabel, 2, 0);
		Panel.Controls.Add(BackColorCombo, 2, 1);
		Panel.Controls.Add(BorderLeftColorLabel, 1, 2);
		Panel.Controls.Add(BorderLeftColorCombo, 1, 3);
		Panel.Controls.Add(BorderRightColorLabel, 2, 2);
		Panel.Controls.Add(BorderRightColorCombo, 2, 3);
		Panel.Controls.Add(BorderTopColorLabel, 1, 4);
		Panel.Controls.Add(BorderTopColorCombo, 1, 5);
		Panel.Controls.Add(BorderBottomColorLabel, 2, 4);
		Panel.Controls.Add(BorderBottomColorCombo, 2, 5);
		
		Panel.Dock = DockStyle.Fill;
		Controls.Add(Panel);
		
	}
}

	class ExternalProgramsSettingsPage : SettingsPage
	{
		Label	Description = new Label();

		public ExternalProgramsSettingsPage()
		{
			Text = "External Programs";

			Description.Text = "Change the settings on the sub pages to specify the location and to customise the behaviour of the external programs that TortoiseHg uses.";
			Description.Dock = DockStyle.Fill;
			Description.Padding = new Padding(0, 5, 0, 0);
			Controls.Add(Description);
		}
	}



	class CommandsSettingsPage : SettingsPage
	{
		public CommandsSettingsPage()
		{
			Text = "Commands";
		}
	}

	class RevisionGraphSettingsPage : SettingsPage
	{
		public RevisionGraphSettingsPage()
		{
			Text = "Revision Graph";
		}
	}

	class SettingsPage : Panel
	{
		public virtual void Save()
		{
		}
	}
	
	public class SettingsDialogSettings
	{
		public int		TreeViewWidth = 200;
	}

	class SettingsDialog : CommandDialog
	{
		protected ImageList		ImageList = new ImageList();

		protected TreeView		TreeView = new TreeView();
		protected Splitter		Splitter = new Splitter();
		protected Panel			PagePanel = new Panel();
		protected TitleLabel	TitleLabel = new TitleLabel();


		public SettingsDialog(HexEdApp app) : base(Buttons.OK | Buttons.Cancel | Buttons.Apply)
		{
			Text = "Hexed Settings";
//			Icon = Settings.Instance.Icon("overlays.ico");
			Image = Settings.Instance.Image("settings-48.png");
			Description = "Change the settings below to customise the behaviour and appearance of Hexed";
			DockPadding.All = 5;

			PagePanel.Dock = DockStyle.Fill;

			TitleLabel.Dock = DockStyle.Top;
			TitleLabel.Text = "No Settings Page Selected";
			PagePanel.Controls.Add(new Panel());
			PagePanel.Controls.Add(TitleLabel);
			PagePanel.Padding = new Padding(5, 0, 0, 0);
			Controls.Add(PagePanel);

			TreeView.Width = 150;
			TreeView.HideSelection = false;
			TreeView.Dock = DockStyle.Left;
			Splitter.Dock = DockStyle.Left;
			Controls.Add(Splitter);
			Controls.Add(TreeView);

			ImageList.ColorDepth = ColorDepth.Depth32Bit;
			ImageList.ImageSize = new Size(16, 16);
/*			ImageList.Images.Add(Settings.Instance.Image("settings-48.png"));
			ImageList.Images.Add(Settings.Instance.Image("overlays-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("diff-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("commit-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("extprogs-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("extprog-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("revgraph-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("locale-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("repo-16.png"));
			ImageList.Images.Add(Settings.Instance.Image("commands-16.png"));
			ImageList.Images.Add(Settings.Instance.Icon("merge.ico"));
*/			TreeView.ImageList = ImageList;


			SettingsPage page;

//			AddPage(new GeneralSettingsPage(), 7);
//			AddPage(new OverlaySettingsPage(), 1);

			page = AddPage(new EditorSettingsPage(), 3);
			AddPage(page, new EditorLayoutSettingsPage(), 3);
			AddPage(page, new EditorAppearanceSettingsPage(), 3);
		
			page = AddPage(new PluginsSettingsPage(app), 3);
			page = AddPage(new ShortcutsSettingsPage(app), 3);
		
			page = AddPage(new CommandsSettingsPage(), 9);
			AddPage(page, new RevisionGraphSettingsPage(), 6);
//			AddPage(page, new CommitSettingsPage(), 3);

			page = AddPage(new ExternalProgramsSettingsPage(), 4);
//			AddPage(page, new MercurialSettingsPage(), 5);
//			AddPage(page, new GraphVizSettingsPage(), 5);

//			page = AddPage(new RepositoriesSettingsPage(), 8);

			TreeView.AfterSelect += new TreeViewEventHandler(OnTreeAfterSelect);

			TreeView.ExpandAll();

			OK.Click += new EventHandler(OnOK);
			Cancel.Click += new EventHandler(OnCancel);
			Apply.Click += new EventHandler(OnApply);

			Size = new Size(640, 480);
		}

		public SettingsPage AddPage(SettingsPage page, int imageIndex)
		{
			TreeNode n = TreeView.Nodes.Add(page.Text);
			n.ImageIndex = n.SelectedImageIndex = imageIndex;
			n.Name = page.Text;
			n.Text = page.Text;
			n.Tag = page;

			return page;
		}

		public SettingsPage AddPage(SettingsPage parentPage, SettingsPage page, int imageIndex)
		{
			TreeNode[] p = TreeView.Nodes.Find(parentPage.Text, true);

			if(p.Length == 0)
				throw new Exception("Parent page not found");
			else if(p.Length > 1)
				throw new Exception("Too many parent pages found");

			TreeNode n = p[0].Nodes.Add(page.Text);
			n.ImageIndex = n.SelectedImageIndex = imageIndex;
			n.Name = page.Text;
			n.Text = page.Text;
			n.Tag = page;

			return page;
		}

		protected void Save(TreeNodeCollection nodes)
		{
			foreach(TreeNode n in nodes)
			{
				Save(n.Nodes);

				SettingsPage page = (SettingsPage)n.Tag;
				if(page != null)
					page.Save();
			}
		}

		protected void OnApply(object sender, EventArgs e)
		{
			Save(TreeView.Nodes);
//			Settings.Instance.Save();
		}

		protected void OnOK(object sender, EventArgs e)
		{
			OnApply(sender, e);
			DialogResult = DialogResult.OK;
			Close();
		}

		protected void OnCancel(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		protected void OnTreeAfterSelect(object sender, TreeViewEventArgs e)
		{
			Control page = (Control)TreeView.SelectedNode.Tag;
			page.Dock = DockStyle.Fill;

			PagePanel.SuspendLayout();

			PagePanel.Controls.Add(page);
			PagePanel.Controls.SetChildIndex(page, 0);
			PagePanel.Controls.RemoveAt(1);
			TitleLabel.Text = page.Text;

			PagePanel.ResumeLayout();
		}
	}


