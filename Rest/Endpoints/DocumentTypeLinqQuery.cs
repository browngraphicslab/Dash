using System;
using DashShared;

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
