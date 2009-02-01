using System;
using System.Drawing;
using System.Windows.Forms;


public class PatternDialog : DialogBase
{
	TableLayoutPanel Panel;
	Label    TypeLabel;
	ComboBox TypeCombo;
	Label    RadixLabel;
	ComboBox RadixCombo;
	Label    ValueLabel;
	HexView  HexView;
	Panel    HexViewPanel;
	TextBox  TextBox;
	Panel    ValuePanel;
	TableLayoutPanel ExtrasPanel;
	int      LastSelectedIndex = -1;
	
	
	public virtual byte[] Pattern
	{
		get
		{
			if(TypeCombo.SelectedIndex != 0)
			{
				byte[] pattern = new byte[HexView.Document.Length];
				HexView.Document.GetBytes(0, pattern, pattern.Length);
				return pattern;
			}
			else
				return System.Text.Encoding.ASCII.GetBytes(TextBox.Text);
		}
	}

	protected TableLayoutPanel Extras
	{
		get { return ExtrasPanel; }
	}
	
	public PatternDialog()
	{
		Panel = new TableLayoutPanel();
		Panel.ColumnCount = 4;
		Panel.RowCount = 3;
		Panel.BackColor = Color.Transparent;
		Panel.Dock = DockStyle.Fill;
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		Panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		Panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		
		TypeLabel = new Label();
		TypeLabel.Text = "Type:";
		TypeLabel.AutoSize = true;
		TypeLabel.Anchor = AnchorStyles.Left;
		Panel.Controls.Add(TypeLabel);
		
		TypeCombo = new ComboBox();
		TypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		TypeCombo.Items.Add("ASCII String");
		TypeCombo.Items.Add("Bytes");
		TypeCombo.Items.Add("Words (LE)");
		TypeCombo.Items.Add("Words (BE)");
		TypeCombo.Items.Add("Double Words (LE)");
		TypeCombo.Items.Add("Double Words (BE)");
		TypeCombo.Items.Add("Quad Words (LE)");
		TypeCombo.Items.Add("Quad Words (BE)");
		TypeCombo.SelectedIndex = 0;
		TypeCombo.SelectedIndexChanged += OnTypeComboSelectedIndexChanged;
		TypeCombo.Dock = DockStyle.Fill;
		Panel.Controls.Add(TypeCombo);
		
		RadixLabel = new Label();
		RadixLabel.Text = "Radix:";
		RadixLabel.AutoSize = true;
		RadixLabel.Anchor = AnchorStyles.Left;
		Panel.Controls.Add(RadixLabel);
		
		RadixCombo = new ComboBox();
		RadixCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		RadixCombo.Items.Add("Binary");
		RadixCombo.Items.Add("Octal");
		RadixCombo.Items.Add("Decimal");
		RadixCombo.Items.Add("Hexadecimal");
		RadixCombo.SelectedIndex = 3;
		RadixCombo.SelectedIndexChanged += OnRadixComboSelectedIndexChanged;
		RadixCombo.Dock = DockStyle.Fill;
		Panel.Controls.Add(RadixCombo);

		ValuePanel = new Panel();
		ValuePanel.Dock = DockStyle.Fill;
		
		HexView = new HexView(new Document());
		HexView.WordsPerGroup = 1;
		HexView.EditMode = EditMode.Insert;
		HexView.Dock = DockStyle.Fill;
		HexViewPanel = new Panel();
		HexViewPanel.BorderStyle = BorderStyle.Fixed3D;
		HexViewPanel.Dock = DockStyle.Fill;
		HexViewPanel.Controls.Add(HexView);
		HexViewPanel.Visible = false;
		ValuePanel.Controls.Add(HexViewPanel);
		
		TextBox = new TextBox();
		TextBox.Multiline = true;
		TextBox.Dock = DockStyle.Fill;
		ValuePanel.Controls.Add(TextBox);
		
		Panel.Controls.Add(ValuePanel);
		Panel.SetColumnSpan(ValuePanel, 4);
		
		
		ExtrasPanel = new TableLayoutPanel();
		ExtrasPanel.Height = 10;
		ExtrasPanel.Dock = DockStyle.Fill;
		ExtrasPanel.AutoSize = true;
		ExtrasPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		Panel.Controls.Add(ExtrasPanel);
		Panel.SetColumnSpan(ExtrasPanel, 4);

		Controls.Add(Panel);
		
		TextBox.Focus();
		
		Size = new Size(480, 240);
	}

	protected override void OnShown(EventArgs e)
	{
		if(TypeCombo.SelectedIndex == 0)
			TextBox.Focus();
		else
			HexView.Focus();
	}
	
	protected void OnTypeComboSelectedIndexChanged(object sender, EventArgs e)
	{
		ValuePanel.SuspendLayout();
		if(TypeCombo.SelectedIndex == 0)
		{
			byte[] data = new byte[HexView.Document.Length];
			HexView.Document.GetBytes(0, data, data.Length);
			if(LastSelectedIndex != 0)
				TextBox.Text = System.Text.Encoding.ASCII.GetString(data);
			HexViewPanel.Visible = false;
			TextBox.Visible = true;
			TextBox.Focus();
		}
		else
		{
			if(LastSelectedIndex == 0)
				HexView.Document.Insert(HexView.Document.Marks.Start, HexView.Document.Marks.End, TextBox.Text);
			TextBox.Visible = false;
			HexViewPanel.Visible = true;
			HexView.Focus();
			switch(TypeCombo.SelectedIndex)
			{
				case 1:
					HexView.BytesPerWord = 1;
					break;
				case 2:
					HexView.BytesPerWord = 2;
					HexView.Endian = Endian.Little;
					break;
				case 3:
					HexView.BytesPerWord = 2;
					HexView.Endian = Endian.Big;
					break;
				case 4:
					HexView.BytesPerWord = 4;
					HexView.Endian = Endian.Little;
					break;
				case 5:
					HexView.BytesPerWord = 4;
					HexView.Endian = Endian.Big;
					break;
				case 6:
					HexView.BytesPerWord = 8;
					HexView.Endian = Endian.Little;
					break;
				case 7:
					HexView.BytesPerWord = 8;
					HexView.Endian = Endian.Big;
					break;
			}
		}
		ValuePanel.ResumeLayout();
		
		LastSelectedIndex = TypeCombo.SelectedIndex;
	}
	
	protected void OnRadixComboSelectedIndexChanged(object sender, EventArgs e)
	{
		switch(RadixCombo.SelectedIndex)
		{
			case 0:
				HexView.DataRadix = 2;
				break;
			case 1:
				HexView.DataRadix = 8;
				break;
			case 2:
				HexView.DataRadix = 10;
				break;
			case 3:
				HexView.DataRadix = 16;
				break;
		}
	}
}
