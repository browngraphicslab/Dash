using System;
using System.Collections.Generic;
using System.Linq;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    [FieldModelType(TypeInfo.Document)]
    public class DocumentModel : FieldModel
    {
        /// <summary>
        /// A dictionary of <see cref="KeyModel.Id"/> to <see cref="FieldModel.Id"/>. These fields represent all the 
        /// data that is stored in the document model
        /// </summary>
        public Dictionary<string, string> Fields;

        /// <summary>
        /// The type of this document.
        /// </summary>
        public DocumentType DocumentType;

        public DocumentModel() : base(null) { }

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<string, string> fields, DocumentType type, string id = null) : base(id)
        {
            DocumentType = type ?? throw new ArgumentNullException();
            Fields = new Dictionary<string, string>(fields);
            //Map.Add(Id, this);
        }
    }
}
