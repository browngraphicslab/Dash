using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class BoundController
    {
        public FieldControllerBase FieldModelController;
        public DocumentController   ContextDocumentController;
        public BoundController(FieldControllerBase fieldModelController, DocumentController contextDocument)
        {
            FieldModelController = fieldModelController;
            ContextDocumentController = contextDocument;
        }
    }
    public class BoundControllerToFrameworkElementConverter : SafeDataToXamlConverter<BoundController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(BoundController data, object parameter = null)
        {
            if (data == null)
                return new TextBox();
            var convertDataToXaml = data.FieldModelController.GetTableCellView(new Context(data.ContextDocumentController));
            return convertDataToXaml;
        }

        public override BoundController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
    public class ReferenceFieldModelControllerToFrameworkElementConverter : SafeDataToXamlConverter<ReferenceController, FrameworkElement>
    {
        public override FrameworkElement ConvertDataToXaml(ReferenceController data, object parameter = null)
        {
            return data?.GetTableCellView(null) ?? new Grid();
        }

        public override ReferenceController ConvertXamlToData(FrameworkElement xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
