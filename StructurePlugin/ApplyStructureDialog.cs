using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;


public class ApplyStructureDialog : DialogBase
{
	HexView HexView;
	string BaseDirectoryPath;

	TableLayoutPanel ApplyToPanel;
	Label ApplyToLabel;
	RadioButton ApplyToDocRadioButton;
	RadioButton ApplyToCursorRadioButton;
	RadioButton ApplyToSelectionRadioButton;
	RadioButton ApplyToRangeRadioButton;

	TableLayoutPanel RangePanel;
	Label    StartLabel;
	TextBox  StartTextBox;
	Label    EndLabel;
	TextBox  EndTextBox;
	Label    LengthLabel;
	TextBox  LengthTextBox;

	TableLayoutPanel TreePanel;
	TreeView DefinitionTree;

	TableLayoutPanel Panel;
	Label    DefinitionLabel;

	private string _DefinitionPath;
	public string DefinitionPath { get { return _DefinitionPath; } }

	private long _StartPosition;
	public long StartPosition 
	{
		get { return _StartPosition; }
		set
		{
			_StartPosition = value;
			StartTextBox.Text = value.ToString();
			LengthTextBox.Text = Length.ToString();
		}
	}

	private long _EndPosition;
	public long EndPosition
	{
		get { return _EndPosition; }
		set
		{
			_EndPosition = value;
			EndTextBox.Text = value.ToString();
			LengthTextBox.Text = Length.ToString();
		}
	}

	public long Length
	{
		get { return _EndPosition - _StartPosition; }
	}

	public ApplyStructureDialog(HexView view, string baseDirectoryPath) : base(DialogBase.Buttons.OK | DialogBase.Buttons.Cancel)
	{
		HexView = view;
		BaseDirectoryPath = baseDirectoryPath;

		Text = "Apply Structure";

		Panel = new TableLayoutPanel();
		Panel.ColumnCount = 2;
		Panel.RowCount = 1;
		Panel.BackColor = Color.Transparent;
		Panel.Dock = DockStyle.Fill;
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		TreePanel = new TableLayoutPanel();
		TreePanel.ColumnCount = 1;
		TreePanel.RowCount = 2;
		TreePanel.BackColor = Color.Transparent;
		TreePanel.Dock = DockStyle.Fill;
		TreePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		TreePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		TreePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		DefinitionLabel = new Label();
		DefinitionLabel.Text = "Definition:";
		DefinitionLabel.AutoSize = true;
		DefinitionLabel.Anchor = AnchorStyles.Left;
		TreePanel.Controls.Add(DefinitionLabel);

		DefinitionTree = new TreeView();
		DefinitionTree.Dock = DockStyle.Fill;
		DefinitionTree.AfterSelect += OnTreeViewAfterSelect;
		DefinitionTree.NodeMouseDoubleClick += OnTreeViewNodeMouseDoubleClick;
		TreePanel.Controls.Add(DefinitionTree);

		Panel.Controls.Add(TreePanel);

		ApplyToPanel = new TableLayoutPanel();
		ApplyToPanel.ColumnCount = 1;
		ApplyToPanel.RowCount = 6;
		ApplyToPanel.BackColor = Color.Transparent;
		ApplyToPanel.Dock = DockStyle.Fill;
		ApplyToPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		ApplyToPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		ApplyToPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		ApplyToPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		ApplyToPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		ApplyToPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		ApplyToPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		ApplyToLabel = new Label();
		ApplyToLabel.Text = "Apply To:";
		ApplyToLabel.AutoSize = true;
		ApplyToLabel.Anchor = AnchorStyles.Left;
		ApplyToPanel.Controls.Add(ApplyToLabel);
		
		ApplyToDocRadioButton = new RadioButton();
		ApplyToDocRadioButton.Text = "Document";
		ApplyToDocRadioButton.AutoSize = true;
		ApplyToDocRadioButton.Anchor = AnchorStyles.Left;
		ApplyToDocRadioButton.Checked = true;
		ApplyToDocRadioButton.CheckedChanged += OnApplyToDocRadioButtonCheckedChanged;
		ApplyToPanel.Controls.Add(ApplyToDocRadioButton);

		ApplyToCursorRadioButton = new RadioButton();
		ApplyToCursorRadioButton.Text = "Cursor";
		ApplyToCursorRadioButton.AutoSize = true;
		ApplyToCursorRadioButton.Anchor = AnchorStyles.Left;
		ApplyToCursorRadioButton.CheckedChanged += OnApplyToCursorRadioButtonCheckedChanged;
		ApplyToPanel.Controls.Add(ApplyToCursorRadioButton);

		ApplyToSelectionRadioButton = new RadioButton();
		ApplyToSelectionRadioButton.Text = "Selection";
		ApplyToSelectionRadioButton.AutoSize = true;
		ApplyToSelectionRadioButton.Anchor = AnchorStyles.Left;
		ApplyToSelectionRadioButton.CheckedChanged += OnApplyToSelectionRadioButtonCheckedChanged;
		ApplyToPanel.Controls.Add(ApplyToSelectionRadioButton);

		ApplyToRangeRadioButton = new RadioButton();
		ApplyToRangeRadioButton.Text = "Range";
		ApplyToRangeRadioButton.AutoSize = true;
		ApplyToRangeRadioButton.Anchor = AnchorStyles.Left;
		ApplyToRangeRadioButton.CheckedChanged += OnApplyToRangeRadioButtonCheckedChanged;
		ApplyToPanel.Controls.Add(ApplyToRangeRadioButton);

		RangePanel = new TableLayoutPanel();
		RangePanel.ColumnCount = 2;
		RangePanel.RowCount = 4;
		RangePanel.BackColor = Color.Transparent;
		RangePanel.Dock = DockStyle.Fill;
		RangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		RangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		RangePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		RangePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		RangePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		RangePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		RangePanel.DockPadding.Left = 16;
		RangePanel.Enabled = false;

		StartLabel = new Label();
		StartLabel.Text = "Start:";
		StartLabel.AutoSize = true;
		StartLabel.Anchor = AnchorStyles.Left;
		RangePanel.Controls.Add(StartLabel);

		StartTextBox = new TextBox();
		StartTextBox.Dock = DockStyle.Fill;
		StartTextBox.TextChanged += OnStartTextBoxTextChanged;
		RangePanel.Controls.Add(StartTextBox);

		EndLabel = new Label();
		EndLabel.Text = "End Position:";
		EndLabel.AutoSize = true;
		EndLabel.Anchor = AnchorStyles.Left;
		RangePanel.Controls.Add(EndLabel);

		EndTextBox = new TextBox();
		EndTextBox.Dock = DockStyle.Fill;
		EndTextBox.TextChanged += OnEndTextBoxTextChanged;
		RangePanel.Controls.Add(EndTextBox);

		LengthLabel = new Label();
		LengthLabel.Text = "Length:";
		LengthLabel.AutoSize = true;
		LengthLabel.Anchor = AnchorStyles.Left;
		RangePanel.Controls.Add(LengthLabel);

		LengthTextBox = new TextBox();
		LengthTextBox.Dock = DockStyle.Fill;
		LengthTextBox.TextChanged += OnLengthTextBoxTextChanged;
		RangePanel.Controls.Add(LengthTextBox);

		ApplyToPanel.Controls.Add(RangePanel);

		Panel.Controls.Add(ApplyToPanel);

		Controls.Add(Panel);

		StartPosition = 0;
		EndPosition = HexView.Document.Length;

		//DefinitionTree.SetFocus();
		Size = new Size(480, 320);
	}

	protected override void OnShown(EventArgs e)
	{
		DefinitionTree.Nodes.Clear();
		PopulateDefinitionTree(DefinitionTree.Nodes, BaseDirectoryPath);
	}

	private void PopulateDefinitionTree(TreeNodeCollection nodes, string directoryPath)
	{
		if(directoryPath == null)
			directoryPath = BaseDirectoryPath;

		string[] directories = Directory.GetDirectories(directoryPath);
		foreach(string directory in directories)
		{
			TreeNode node = nodes.Add(Path.GetFileName(directory));
			PopulateDefinitionTree(node.Nodes, Path.Combine(directoryPath, directory));
		}

		string[] filenames = Directory.GetFiles(directoryPath, "*.def");
		foreach(string filename in filenames)
		{
			TreeNode node = nodes.Add(Path.GetFileName(filename));
			node.Tag = filename;
		}	
	}

	protected void OnTreeViewAfterSelect(object sender, TreeViewEventArgs e)
	{
		if(DefinitionTree.SelectedNode != null && DefinitionTree.SelectedNode.Tag != null)
		{
			_DefinitionPath = (string)DefinitionTree.SelectedNode.Tag;
			OkButton.Enabled = true;
		}
		else
			OkButton.Enabled = false;
	}

	protected void OnTreeViewNodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
	{
		if(OkButton.Enabled)
			OnOK(this, new EventArgs());
	}

	protected void OnApplyToDocRadioButtonCheckedChanged(object sender, EventArgs e)
	{
		if(ApplyToDocRadioButton.Checked)
		{
			StartPosition = 0;
			EndPosition = HexView.Document.Length;
		}
	}

	protected void OnApplyToCursorRadioButtonCheckedChanged(object sender, EventArgs e)
	{
		if(ApplyToCursorRadioButton.Checked)
		{
			StartPosition = HexView.Selection.Start;
			EndPosition = HexView.Document.Length;
		}
	}

	protected void OnApplyToSelectionRadioButtonCheckedChanged(object sender, EventArgs e)
	{
		if(ApplyToSelectionRadioButton.Checked)
		{
			StartPosition = HexView.Selection.Start;
			EndPosition = HexView.Selection.End;
		}
	}

	protected void OnApplyToRangeRadioButtonCheckedChanged(object sender, EventArgs e)
	{
		RangePanel.Enabled = ApplyToRangeRadioButton.Checked;
	}

	protected void OnStartTextBoxTextChanged(object sender, EventArgs e)
	{
		long val;

		if(Int64.TryParse(StartTextBox.Text, out val))
		{
			_StartPosition = val;
			LengthTextBox.Text = Length.ToString();
		}
	}

	protected void OnEndTextBoxTextChanged(object sender, EventArgs e)
	{
		long val;

		if(Int64.TryParse(EndTextBox.Text, out val))
		{
			_EndPosition = val;
			LengthTextBox.Text = Length.ToString();
		}
	}

	protected void OnLengthTextBoxTextChanged(object sender, EventArgs e)
	{
		long val;

		if(Int64.TryParse(LengthTextBox.Text, out val))
			EndPosition = StartPosition + val;
	}
}

		

