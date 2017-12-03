using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class DocumentTypeLinqQuery : IQuery<FieldModel> 
    {
        public DocumentTypeLinqQuery(DocumentType type)
        {
            Func = model => model is DocumentModel && ((DocumentModel)model).DocumentType.Equals(type);
        }

        public Func<FieldModel, bool> Func { private set; get; }
    }
}
