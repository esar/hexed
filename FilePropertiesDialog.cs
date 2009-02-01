using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;


class FilePropertiesDialog : DialogBase
{
	protected FileInfo Info;

	protected Label    FileNameLabel;
	protected Label    FileNameValue;
	protected Label    LocationLabel;
	protected Label    LocationValue;
	protected Label    SizeLabel;
	protected Label    SizeValue;
	protected Label    CreateDateLabel;
	protected Label    CreateDateValue;
	protected Label    ModifiedDateLabel;
	protected Label    ModifiedDateValue;
	protected Label    AccessedDateLabel;
	protected Label    AccessedDateValue;
	protected Label    AttributesLabel;
	protected CheckBox ReadOnlyCheck;
	protected CheckBox ArchiveCheck;
	protected CheckBox SystemCheck;
	protected CheckBox HiddenCheck;
	protected TableLayoutPanel Panel;
	
	
	public FilePropertiesDialog(string filename)
	{
		Info = new FileInfo(filename);
	
		Text = "File Properties";
		
		Panel = new TableLayoutPanel();
		Panel.RowCount = 8;
		Panel.ColumnCount = 3;
		
		FileNameLabel = new Label();
		FileNameLabel.Text = "File Name:";
		Panel.Controls.Add(FileNameLabel);
		
		FileNameValue = new Label();
		FileNameValue.Text = Info.Name;
		FileNameValue.AutoSize = true;
		Panel.Controls.Add(FileNameValue);
		Panel.SetColumnSpan(FileNameValue, 2);
		
		LocationLabel = new Label();
		LocationLabel.Text = "Location:";
		Panel.Controls.Add(LocationLabel);
		
		LocationValue = new Label();
		LocationValue.Text = Info.DirectoryName;
		LocationValue.AutoSize = true;
		Panel.Controls.Add(LocationValue);
		Panel.SetColumnSpan(LocationValue, 2);
		
		SizeLabel = new Label();
		SizeLabel.Text = "Size:";
		Panel.Controls.Add(SizeLabel);
		
		SizeValue = new Label();
		SizeValue.Text = Info.Length.ToString();
		SizeValue.AutoSize = true;
		Panel.Controls.Add(SizeValue);
		Panel.SetColumnSpan(SizeValue, 2);
		
		CreateDateLabel = new Label();
		CreateDateLabel.Text = "Created:";
		Panel.Controls.Add(CreateDateLabel);
		
		CreateDateValue = new Label();
		CreateDateValue.Text = Info.CreationTime.ToString();
		CreateDateValue.AutoSize = true;
		Panel.Controls.Add(CreateDateValue);
		Panel.SetColumnSpan(CreateDateValue, 2);
		
		ModifiedDateLabel = new Label();
		ModifiedDateLabel.Text = "Modified:";
		Panel.Controls.Add(ModifiedDateLabel);
		
		ModifiedDateValue = new Label();
		ModifiedDateValue.Text = Info.LastWriteTime.ToString();
		ModifiedDateValue.AutoSize = true;
		Panel.Controls.Add(ModifiedDateValue);
		Panel.SetColumnSpan(ModifiedDateValue, 2);
		
		AccessedDateLabel = new Label();
		AccessedDateLabel.Text = "Accessed:";
		Panel.Controls.Add(AccessedDateLabel);
		
		AccessedDateValue = new Label();
		AccessedDateValue.Text = Info.LastAccessTime.ToString();
		AccessedDateValue.AutoSize = true;
		Panel.Controls.Add(AccessedDateValue);
		Panel.SetColumnSpan(AccessedDateValue, 2);
		
		AttributesLabel = new Label();
		AttributesLabel.Text = "Attributes:";
		Panel.Controls.Add(AttributesLabel);
		Panel.SetRowSpan(AttributesLabel, 2);
		
		ReadOnlyCheck = new CheckBox();
		ReadOnlyCheck.Text = "Read Only";
		ReadOnlyCheck.Checked = Info.IsReadOnly;
		Panel.Controls.Add(ReadOnlyCheck);
		
		SystemCheck = new CheckBox();
		SystemCheck.Text = "System";
		SystemCheck.Checked = (Info.Attributes & FileAttributes.System) != 0;
		Panel.Controls.Add(SystemCheck);
		
		ArchiveCheck = new CheckBox();
		ArchiveCheck.Text = "Archive";
		ArchiveCheck.Checked = (Info.Attributes & FileAttributes.Archive) != 0;
		Panel.Controls.Add(ArchiveCheck);
		
		HiddenCheck = new CheckBox();
		HiddenCheck.Text = "Hidden";
		HiddenCheck.Checked = (Info.Attributes & FileAttributes.Hidden) != 0;
		Panel.Controls.Add(HiddenCheck);
		
		Panel.Dock = DockStyle.Fill;
		Panel.BackColor = Color.Transparent;
		Controls.Add(Panel);
	}
}
