using System.Collections.Generic;
using DashShared;

namespace Dash
{ 
    public class CreateNewDocumentRequestArgs : RequestArgs
    {
        public Dictionary<Key, FieldModel> Fields { get; }
        public DocumentType Type { get; }

        public CreateNewDocumentRequestArgs(Dictionary<Key, FieldModel> fields, DocumentType type)
        {
            Fields = fields;
            Type = type;
        }
    }
}
