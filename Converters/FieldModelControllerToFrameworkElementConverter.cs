using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class FieldModelControllerToFrameworkElementConverter : SafeDataToXamlConverter<FieldModelController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(FieldModelController data, object parameter = null)
        {
            return data.GetTableCellView();
        }

        public override FieldModelController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
