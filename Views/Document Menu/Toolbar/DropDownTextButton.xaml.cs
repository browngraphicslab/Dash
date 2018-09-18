using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons;
using System.Collections.ObjectModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Document_Menu.Toolbar
{
	public sealed partial class DropDownTextButton : UserControl , IToolbarItem
	{
		private int _pos;
		private ObservableCollection<string> fonts;
		private bool _isCompact;
		private RichTextSubtoolbar _toolbar;

		public DropDownTextButton(RichTextSubtoolbar toolbar, int pos)
		{
			this.InitializeComponent();
			_toolbar = toolbar;
			_pos = pos;
			fonts = new ObservableCollection<string>();
			_isCompact = false;
			this.SetUpBinding();
		}

		public void SetUpBinding()
		{
			fonts.Add("Arial");
			fonts.Add("Courier New");
			fonts.Add("Times New Roman");
		}


		public int Position { get => _pos; set => _pos = value; }
		public bool IsCompact { get => _isCompact; set => _isCompact = value; }

		private void Font_Selected(object sender, SelectionChangedEventArgs e)
		{
			//tell the toolbar to update the font of the selected text to the chosen font
			//_toolbar.UpdateFont(xFontsCombo.SelectedItem);
		}
	}
}
