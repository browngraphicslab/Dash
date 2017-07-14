using System;
using Windows.UI.Xaml;

namespace Dash.Converters
{
    public class DocumentToUIElementConverter : SafeDataToXamlConverter<DocumentController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(DocumentController data, object parameter = null)
        {
            return data.MakeViewUI(null);
        }

        public override DocumentController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
