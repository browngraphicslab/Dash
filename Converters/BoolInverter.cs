namespace Dash
{
    public class BoolInverter : SafeDataToXamlConverter<bool, bool>
    {
        public override bool ConvertDataToXaml(bool data, object parameter = null) => !data;

        public override bool ConvertXamlToData(bool data, object parameter = null) => !data;
    }
}
