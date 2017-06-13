using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash.Models
{
    class DocumentsFieldModel : FieldModel
    {
        public ICollection<DocumentModel> Data { get; set; }

        DocumentsFieldModel()
        {
        }

        DocumentsFieldModel(ICollection<DocumentModel> documents)
        {
            Data = new List<DocumentModel>(documents);
        }

        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }
    }
}
