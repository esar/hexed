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
using System.IO;


class ConfirmSaveDialog : DialogBase
{
	protected CheckBox InPlaceCheckbox;
	protected Label    QuestionLabel;
	protected Label    DetailLabel;
	protected ProgressBar  ProgressBar;

	protected TableLayoutPanel Panel;
	
	protected Timer Timer;

	protected Document Doc;
	protected PieceBuffer.SavePlan SavePlan;
	protected string Filename;

	public ConfirmSaveDialog(Document doc, PieceBuffer.SavePlan plan, string filename, bool confirm) : 
	       base(DialogBase.Buttons.OK | DialogBase.Buttons.Cancel)
	{
		Doc = doc;
		SavePlan = plan;
		Filename = filename;

		Text = "Save Changes?";
		
		Panel = new TableLayoutPanel();
		Panel.RowCount = 3;
		Panel.ColumnCount = 1;
		
		QuestionLabel = new Label();
		QuestionLabel.Text = "Are you sure you want to save your changes?";
		QuestionLabel.AutoSize = true;
		QuestionLabel.Dock = DockStyle.Fill;
		QuestionLabel.BackColor = Color.Transparent;
		Panel.Controls.Add(QuestionLabel);

		DetailLabel = new Label();
		DetailLabel.Text = plan.ToString();
		DetailLabel.Dock = DockStyle.Fill;
		DetailLabel.BackColor = Color.Transparent;
		Panel.Controls.Add(DetailLabel);

		Panel.Dock = DockStyle.Fill;
		Panel.BackColor = Color.Transparent;
		Controls.Add(Panel);

		Size = new Size(480, 240);

		if(!confirm)
			OnOK(this, EventArgs.Empty);
	}

	protected void SaveCompleteCallback(IAsyncResult result)
	{
		if(Filename != null)
			Doc.EndSaveAs((PieceBuffer.SavePlan)result);
		else
			Doc.EndSave((PieceBuffer.SavePlan)result);

		ProgressBar.Value = 100;
		QuestionLabel.Text = "Written " + SavePlan.TotalLength + " of " + SavePlan.TotalLength + " bytes";	

		Timer.Stop();
		Timer.Dispose();
		Timer = null;

		DialogResult = DialogResult.OK;
		Close();
	}

	protected void OnTimerTick(object sender, EventArgs e)
	{
		ProgressBar.Value = (int)(((double)SavePlan.LengthWritten / SavePlan.TotalLength) * 100);
		QuestionLabel.Text = "Written " + SavePlan.LengthWritten + " of " + SavePlan.TotalLength + " bytes";	
	}

	protected override void OnOK(object sender, EventArgs e)
	{
		Text = "Saving...";

		OkButton.Enabled = false;

		ProgressBar = new ProgressBar();
		ProgressBar.Maximum = 100;
		ProgressBar.Minimum = 0;
		ProgressBar.Value = 0;
		ProgressBar.Dock = DockStyle.Fill;
		ProgressBar.Style = ProgressBarStyle.Continuous;

		Panel.Controls.Remove(DetailLabel);
		Panel.Controls.Add(ProgressBar);

		QuestionLabel.Text = "Written 0 of " + SavePlan.TotalLength + " bytes";	
		QuestionLabel.TextAlign = ContentAlignment.MiddleCenter;

		Timer = new Timer();
		Timer.Interval = 100;
		Timer.Tick += OnTimerTick;
		Timer.Enabled = true;

		if(Filename != null)
			SavePlan = Doc.BeginSaveAs(Filename, new AsyncCallback(SaveCompleteCallback), null);
		else
			SavePlan = Doc.BeginSave(true, new AsyncCallback(SaveCompleteCallback), null);
	}

	protected override void OnCancel(object sender, EventArgs e)
	{
		if(ProgressBar != null)
		{
			if(MessageBox.Show(this, "Are you sure you want to cancel the save operation?", "Cancel?", 
			                   MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return;
			}
			
			CancelButton.Enabled = false;
			SavePlan.Abort();
		}
		else
			Close();
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if(Timer != null && Timer.Enabled)
			e.Cancel = true;
	}
}

