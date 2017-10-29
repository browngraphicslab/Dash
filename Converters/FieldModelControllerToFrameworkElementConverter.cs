using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class BoundFieldModelController
    {
        public FieldControllerBase FieldModelController;
        public DocumentController   ContextDocumentController;
        public BoundFieldModelController(FieldControllerBase fieldModelController, DocumentController contextDocument)
        {
            FieldModelController = fieldModelController;
            ContextDocumentController = contextDocument;
        }
    }
    public class BoundFieldModelControllerToFrameworkElementConverter : SafeDataToXamlConverter<BoundFieldModelController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(BoundFieldModelController data, object parameter = null)
        {
            if (data == null)
                return new TextBox();
            var convertDataToXaml = data.FieldModelController.GetTableCellView(new Context(data.ContextDocumentController));
            return convertDataToXaml;
        }

        public override BoundFieldModelController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
    public class ReferenceFieldModelControllerToFrameworkElementConverter : SafeDataToXamlConverter<ReferenceFieldModelController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(ReferenceFieldModelController data, object parameter = null)
        {
            return data?.GetTableCellView(null) ?? new Grid();
        }

        public override ReferenceFieldModelController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
