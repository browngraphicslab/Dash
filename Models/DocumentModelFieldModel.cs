using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash.Models
{
    class DocumentModelFieldModel : FieldModel
    {
        private DocumentModel _data;
        public DocumentModelFieldModel(DocumentModel data)
        {
            Data = data;
        }
        public DocumentModel Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }
    }
}
