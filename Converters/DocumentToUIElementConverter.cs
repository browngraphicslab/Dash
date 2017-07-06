using System;
using Windows.UI.Xaml;

namespace Dash.Converters
{
    public class DocumentToUIElementConverter : SafeDataToXamlConverter<DocumentModel, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(DocumentModel data, object parameter = null)
        {
            return new DocumentController(data).MakeViewUI();
        }

        public override DocumentModel ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
