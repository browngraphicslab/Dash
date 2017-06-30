using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash.Converters
{
    public class DocumentToUIElementConverter : SafeDataToXamlConverter<DocumentModel, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(DocumentModel data)
        {
            return new DocumentController(data).MakeViewUI()[0];
        }

        public override DocumentModel ConvertXamlToData(FrameworkElement frameworkElement)
        {
            throw new NotImplementedException();
        }
    }
}
