using System;
using System.Windows.Forms;
using System.Drawing;


class SplashScreen : Form
{
	private static SplashScreen _Instance;
	protected static SplashScreen Instance
	{
		get 
		{
			if(_Instance == null)
				_Instance = new SplashScreen();
			return _Instance;
		}
	}
	
	Label    StatusValue = new Label();
	TableLayoutPanel Panel = new TableLayoutPanel();
	
	public static string Status
	{
		set
		{
			Instance.StatusValue.Text = value;
			Application.DoEvents();
		}
	}
	
	public SplashScreen()
	{
		FormBorderStyle = FormBorderStyle.None;
		BackgroundImage = Settings.Instance.Image("splash.jpg");
		//BackColor = Color.White;
		ForeColor = Color.White;
		Font = new Font(Font.SystemFontName, 10, FontStyle.Bold);
		
//		Panel.ColumnCount = 1;
//		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//		Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
//		Panel.BackColor = Color.Transparent;
//		Panel.RowCount = 1;
		
		StatusValue.Text = "Loading...";
		StatusValue.Dock = DockStyle.Fill;
		StatusValue.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
		StatusValue.BackColor = Color.Transparent;
		StatusValue.Padding = new Padding(0, 0, 0, 10);
		Controls.Add(StatusValue);
//		Panel.Controls.Add(StatusValue);
		
//		Panel.Dock = DockStyle.Fill;
//		Controls.Add(Panel);
		
		StartPosition = FormStartPosition.CenterScreen;
		Size = new System.Drawing.Size(320, 240);
	}
	
	public static void Show()
	{
		Instance.Show(null);
		Application.DoEvents();
	}
	
	public static void Hide()
	{
		((Form)Instance).Hide();
		Application.DoEvents();
	}
}
