using System;
using Windows.UI.Xaml;

namespace Dash
{
    public class RadioEnumToBoolConverter : SafeDataToXamlConverter<string, bool>
    {
        private readonly string _selfIdentifier;

        public RadioEnumToBoolConverter(Enum selfIdentifier) => _selfIdentifier = selfIdentifier.ToString();

        public override bool ConvertDataToXaml(string data, object parameter = null) => _selfIdentifier == data;

        public override string ConvertXamlToData(bool xaml, object parameter = null) => xaml ? _selfIdentifier : null;
    }

    public class RadioEnumToVisibilityConverter : SafeDataToXamlConverter<string, Visibility>
    {
        private readonly string _selfIdentifier;

        public RadioEnumToVisibilityConverter(Enum selfIdentifier) => _selfIdentifier = selfIdentifier.ToString();

        public override Visibility ConvertDataToXaml(string data, object parameter = null) => _selfIdentifier == data ? Visibility.Visible : Visibility.Collapsed;

        public override string ConvertXamlToData(Visibility xaml, object parameter = null) => xaml == Visibility.Visible ? _selfIdentifier : null;
    }
}
