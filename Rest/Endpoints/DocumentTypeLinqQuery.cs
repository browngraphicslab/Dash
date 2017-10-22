using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentTypeLinqQuery : IQuery<DocumentModel> 
    {
        public DocumentTypeLinqQuery(DocumentType type)
        {
            Func = model => model.DocumentType.Equals(type);
        }

        public Func<DocumentModel, bool> Func { private set; get; }
    }
}
