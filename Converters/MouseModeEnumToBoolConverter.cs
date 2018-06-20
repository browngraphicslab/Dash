using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class MouseModeEnumToBoolConverter : SafeDataToXamlConverter<string, bool>
    {
        private readonly string _selfIdentifier;

        public MouseModeEnumToBoolConverter(SettingsView.MouseFuncMode selfIdentifier) => _selfIdentifier = selfIdentifier.ToString();

        public override bool ConvertDataToXaml(string data, object parameter = null)
        {
            return _selfIdentifier == data;
        }

        public override string ConvertXamlToData(bool xaml, object parameter = null) { return xaml ? _selfIdentifier : null; }
    }
}
