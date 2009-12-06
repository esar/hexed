using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;


class ConfirmSaveDialog : DialogBase
{
	protected PieceBuffer.SavePlan SavePlan;
	
	protected CheckBox InPlaceCheckbox;
	protected Label    QuestionLabel;
	protected Label    DescriptionLabel;
	protected Label    DetailLabel;

	//protected TableLayoutPanel Panel;
	

	public bool SaveInPlace
	{
		get { return InPlaceCheckbox.Checked; }
	}	

	public ConfirmSaveDialog(PieceBuffer.SavePlan plan) : base(DialogBase.Buttons.All)
	{
		SavePlan = plan;

		Text = "Save Changes?";
		
		//Panel = new TableLayoutPanel();
		//Panel.RowCount = 3;
		//Panel.ColumnCount = 1;
		
		DetailLabel = new Label();
		DetailLabel.Text = plan.ToString();
		DetailLabel.Dock = DockStyle.Fill;
		DetailLabel.BackColor = Color.Transparent;
		Controls.Add(DetailLabel);

		DescriptionLabel = new Label();
		DescriptionLabel.AutoSize = true;
		DescriptionLabel.Dock = DockStyle.Top;
		DescriptionLabel.BackColor = Color.Transparent;
		Controls.Add(DescriptionLabel);
		//Panel.Controls.Add(DescriptionLabel);

		QuestionLabel = new Label();
		QuestionLabel.Text = "Are you sure you want to save your changes?";
		QuestionLabel.AutoSize = true;
		QuestionLabel.Dock = DockStyle.Top;
		QuestionLabel.BackColor = Color.Transparent;
		Controls.Add(QuestionLabel);
		//Panel.Controls.Add(QuestionLabel);

		InPlaceCheckbox = new CheckBox();
		InPlaceCheckbox.Text = "Save In-Place";
		InPlaceCheckbox.Checked = plan.IsInPlace;
		if(!plan.IsInPlace)
			InPlaceCheckbox.Enabled = false;
		InPlaceCheckbox.CheckedChanged += OnInPlaceCheckboxChanged;
		InPlaceCheckbox.Dock = DockStyle.Bottom;
		Controls.Add(InPlaceCheckbox);
		//Panel.Controls.Add(InPlaceCheckbox);

		//Panel.Dock = DockStyle.Fill;
		//Panel.BackColor = Color.Transparent;
		//Controls.Add(Panel);

		OnInPlaceCheckboxChanged(this, EventArgs.Empty);

		Size = new Size(480, 240);
	}

	protected void OnInPlaceCheckboxChanged(object sender, EventArgs e)
	{
		if(InPlaceCheckbox.Checked)
			DescriptionLabel.Text = SavePlan.WriteLength + " of " + SavePlan.TotalLength + 
			                        " bytes will be written in " + SavePlan.BlockCount + " pieces.";
		else
			DescriptionLabel.Text = SavePlan.TotalLength + " bytes will be written.";
	}
}

