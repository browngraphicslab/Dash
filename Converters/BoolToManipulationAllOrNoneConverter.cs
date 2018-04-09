using Windows.UI.Xaml.Input;

namespace Dash
{
    public class BoolToManipulationAllOrNoneConverter : SafeDataToXamlConverter<bool, ManipulationModes>
    {
        public override ManipulationModes ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? ManipulationModes.None : ManipulationModes.All;
        }

        public override bool ConvertXamlToData(ManipulationModes xaml, object parameter = null)
        {
            return xaml == ManipulationModes.None;
        }
    }
}
