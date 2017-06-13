using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Dash
{
    public class DocumentsFieldModel : FieldModel
    {
        private List<DocumentModel> _docs;

        public DocumentsFieldModel(List<DocumentModel> docs)
        {
            _docs = docs;
        }

        public override UIElement MakeView(TemplateModel template)
        {
            foreach (var docModel in _docs)
            {
                DocumentViewModel docVM = new DocumentViewModel(docModel, DocumentLayoutModelSource.DefaultLayoutModelSource);
                DocumentView docView = new DocumentView();
                docView.DataContext = docVM;
            }
        }
    }
}