using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class EditableTextBlock
    {
        public TextBox Box {
            get { return xTextBox; }
            set { xTextBox = value; }
        }

        public TextBlock Block {
            get { return xTextBlock; }
            set { xTextBlock = value; }
        }

        public EditableTextBlock()
        {
            InitializeComponent();

            Box.PointerWheelChanged += (s, e) => e.Handled = true;
            Box.ManipulationDelta += (s, e) => e.Handled = true;

            Box.LostFocus += (s, e) =>
            {
                Box.Visibility = Visibility.Collapsed;
                Block.Visibility = Visibility.Visible;
            };

            Block.DoubleTapped += (s, e) =>
            {
                e.Handled = true;
                Block.Visibility = Visibility.Collapsed;
                Box.Visibility = Visibility.Visible;
                Box.Focus(FocusState.Programmatic);
                Box.SelectAll();
            };
        }
    }
}
