using Windows.UI.Xaml;

namespace Dash
{
    public class GuideLineViewModel : ViewModelBase
    {
        private readonly DashShared.GuideLineModel _guideModel;

        // example of property for binding
        private Visibility _visibility;
        public Visibility Visibility { get { return _visibility; } set { SetProperty(ref _visibility, value); } }

        public GuideLineViewModel(DashShared.GuideLineModel guideModel)
        {
            _guideModel = guideModel;
        }
    }
}
