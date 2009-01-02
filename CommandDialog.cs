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
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

	class CommandDialog : Form
	{
		[Flags]
		public enum Buttons
		{
			None	= 0,
			OK		= 1,
			Cancel	= 2,
			Apply	= 4
		}

		PictureBox	titleImage;
		Label		titleLabel;
		Label		descriptionLabel;
		Panel		titleTextPanel;
		Panel		titlePanel;

		HorzRule	topRule;

		Panel		contentPanel;

		HorzRule	bottomRule;

		public Button		OK;
		public Button		Cancel;
		public Button		Apply;
		Label		spacer1;
		Label		spacer2;
		Panel		buttonPanel;

		public override string Text
		{
			get { return base.Text; }
			set { base.Text = value; titleLabel.Text = value; }
		}

		public string Description
		{
			get { return descriptionLabel.Text; }
			set { descriptionLabel.Text = value; }
		}

		public Image Image
		{
			get { return titleImage.Image; }
			set { titleImage.Image = value; }
		}

		public Control.ControlCollection BaseControls
		{
			get { return base.Controls; }
		}

		public new Control.ControlCollection Controls
		{
			get { return contentPanel.Controls; }
		}

		public new ScrollableControl.DockPaddingEdges DockPadding
		{
			get { return contentPanel.DockPadding; }
		}

		public CommandDialog(Buttons buttons)
		{
			titleImage = new PictureBox();
			titleImage.Dock = DockStyle.Right;
			titleImage.SizeMode = PictureBoxSizeMode.CenterImage;

			titleLabel = new Label();
			titleLabel.Dock = DockStyle.Top;
			titleLabel.Font = new Font("Tahoma", 8, FontStyle.Bold);
			titleLabel.AutoSize = true;

			descriptionLabel = new Label();
			descriptionLabel.Dock = DockStyle.Fill;

			titleTextPanel = new Panel();
			titleTextPanel.Dock = DockStyle.Fill;
			titleTextPanel.Controls.Add(descriptionLabel);
			titleTextPanel.Controls.Add(titleLabel);
			titleTextPanel.DockPadding.All = 5;

			titlePanel = new Panel();
			titlePanel.Controls.Add(titleTextPanel);
			titlePanel.Controls.Add(titleImage);
			titlePanel.Dock = DockStyle.Top;
			titlePanel.Height = 56;
			titlePanel.BackColor = Color.White;

			topRule = new HorzRule();
			topRule.Dock = DockStyle.Top;

			contentPanel = new Panel();
			contentPanel.Dock = DockStyle.Fill;

			bottomRule = new HorzRule();
			bottomRule.Dock = DockStyle.Bottom;

			OK = new Button();
			OK.Text = "OK";
			OK.Dock = DockStyle.Right;
			OK.FlatStyle = FlatStyle.System;

			Cancel = new Button();
			Cancel.Text = "Cancel";
			Cancel.Dock = DockStyle.Right;
			Cancel.FlatStyle = FlatStyle.System;

			Apply = new Button();
			Apply.Text = "Apply";
			Apply.Dock = DockStyle.Right;
			Apply.FlatStyle = FlatStyle.System;

			spacer1 = new Label();
			spacer1.Width = 8;
			spacer1.Dock = DockStyle.Right;
			spacer2 = new Label();
			spacer2.Width = 8;
			spacer2.Dock = DockStyle.Right;

			buttonPanel = new Panel();
			buttonPanel.Dock = DockStyle.Bottom;
			if((buttons & Buttons.OK) != 0)
			{
				buttonPanel.Controls.Add(OK);
			}
			if((buttons & Buttons.Cancel) != 0)
			{
				buttonPanel.Controls.Add(spacer1);
				buttonPanel.Controls.Add(Cancel);
			}
			if((buttons & Buttons.Apply) != 0)
			{
				buttonPanel.Controls.Add(spacer2);
				buttonPanel.Controls.Add(Apply);
			}
			buttonPanel.Height = 31;
			buttonPanel.DockPadding.Top = 8;
			buttonPanel.DockPadding.Bottom = 0;
			buttonPanel.DockPadding.Right = 24;
			buttonPanel.BackColor = Color.Transparent;

			base.Controls.Add(contentPanel);
			base.Controls.Add(topRule);
			base.Controls.Add(titlePanel);
			base.Controls.Add(bottomRule);
			base.Controls.Add(buttonPanel);

			base.DockPadding.Bottom = 8;

			SizeGripStyle = SizeGripStyle.Show;

		}

		public void ShowBottomRule(bool show)
		{
			if(show == false && base.Controls.Contains(bottomRule))
				base.Controls.Remove(bottomRule);
			else if(show == true && !base.Controls.Contains(bottomRule))
				base.Controls.Add(bottomRule);
		}
	}
