using Windows.UI.Xaml;

namespace Dash
{
    public interface ICommandBarBased
    {
        void CommandBarOpen(bool status);
        void SetComboBoxVisibility(Visibility visibility);
    }
}
