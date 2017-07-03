using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GuideLineView : UserControl
    {
        private GuideLineViewModel _vm;

        public GuideLineView(GuideLineViewModel guideViewModel)
        {
            this.InitializeComponent();
            _vm = guideViewModel;
            DataContext = _vm;
        }
    }
}
