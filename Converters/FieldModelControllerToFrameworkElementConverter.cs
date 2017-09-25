using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

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
    public class FieldModelControllerToFrameworkElementConverter : SafeDataToXamlConverter<BoundFieldModelController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(BoundFieldModelController data, object parameter = null)
        {
            return data.FieldModelController.GetTableCellView(new Context(data.ContextDocumentController));
        }

        public override BoundFieldModelController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
