using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

	class ColourComboBox : ComboBox
	{
		public Color	Color
		{
			get
			{
				return Colours[Colours.Length - 1];
			}
			set
			{
				InternalSelectionChange = true;
				Colours[Colours.Length - 1] = value;
				for(int i = 0; i < Colours.Length; ++i)
					if(Colours[i] == value || i == Colours.Length - 1)
						SelectedIndex = i;
				Invalidate();
				InternalSelectionChange = false;
			}
		}

		public int[]	CustomColors;

		private Color[] Colours;
		private bool	InternalSelectionChange = false;

		public ColourComboBox()
		{
			Colours = new Color[] 
			{
				Color.Red,
				Color.Green,
				Color.Blue,
				Color.Cyan,
				Color.Magenta,
				Color.Yellow,
				Color.Brown,
				Color.Gray,
				Color.Purple
			};

			DrawMode = DrawMode.OwnerDrawFixed;
			DropDownStyle = ComboBoxStyle.DropDownList;
			MaxDropDownItems = 10;
			DataSource = Colours;
		}

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			base.OnDrawItem(e);

			e.DrawBackground();

			if(e.Index > 0 && e.Index < Colours.Length - 1)// || (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
			{
				Rectangle rect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 4, e.Bounds.Width - 8, e.Bounds.Height - 8);
				e.Graphics.FillRectangle(new SolidBrush(Colours[e.Index]), rect);
			}
			else
			{
				Rectangle rect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
				if((e.State & DrawItemState.Selected) == DrawItemState.Selected)
					e.Graphics.DrawString("Custom...", e.Font, SystemBrushes.HighlightText, rect);
				else
					e.Graphics.DrawString("Custom...", e.Font, SystemBrushes.WindowText, rect);
			}
		}

		protected override void OnSelectionChangeCommitted(EventArgs e)
		{
			if(SelectedIndex == Colours.Length - 1 && InternalSelectionChange == false)
			{
				ColorDialog dlg = new ColorDialog();

				dlg.Color = Colours[Colours.Length - 1];
				dlg.AnyColor = true;
				dlg.FullOpen = true;


				if(CustomColors != null)
					dlg.CustomColors = CustomColors;

				if(dlg.ShowDialog() == DialogResult.OK)
					Colours[Colours.Length - 1] = dlg.Color;

				CustomColors = dlg.CustomColors;
			}
			else
				Colours[Colours.Length - 1] = Colours[SelectedIndex];

			if(InternalSelectionChange == false)
				base.OnSelectedIndexChanged(e);
		}
	}







