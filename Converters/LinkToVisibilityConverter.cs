using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;

namespace Dash
{
    public class LinkToVisibilityConverter : SafeDataToXamlConverter<List<object>, Visibility>
    {
        public override Visibility ConvertDataToXaml(List<object> data, object parameter = null)
        {
            var linksTo = data[0] as List<DocumentController>;
            if (linksTo?.Any() ?? false)
            {
                return Visibility.Visible;
            }
            var linksFrom = data[1] as List<DocumentController>;
            if (linksFrom?.Any() ?? false)
            {
                return Visibility.Visible;
            }

            if (data[2] is List<DocumentController> regions)
            {
                foreach (var region in regions)
                {
                    var dataDoc = region.GetDataDocument();
                    var rLinksTo = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey);
                    if (rLinksTo?.Any() ?? false)
                    {
                        return Visibility.Visible;
                    }
                    var rLinksFrom = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey);
                    if (rLinksFrom?.Any() ?? false)
                    {
                        return Visibility.Visible;
                    }
                }
            }

            return Visibility.Collapsed;
        }

        public override List<object> ConvertXamlToData(Visibility xaml, object parameter = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
