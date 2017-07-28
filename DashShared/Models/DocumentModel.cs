using System;
using System.Collections.Generic;
using System.Linq;
using DashShared;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class DocumentModel : AuthorizableEntityBase
    {
        static public Dictionary<string, DocumentModel> Map = new Dictionary<string, DocumentModel>();

        /// <summary>
        /// A dictionary of <see cref="Key"/> to <see cref="FieldModel.Id"/>. These fields represent all the 
        /// data that is stored in the document model
        /// </summary>
        public Dictionary<Key, string> Fields;

        /// <summary>
        /// The type of this document.
        /// </summary>
        public DocumentType DocumentType;
        
        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<Key, FieldModel> fields, DocumentType type, string id = null) : base(id)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            DocumentType = type;
            Fields = fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
            Map.Add(Id, this);
        }

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<Key, string> fields, DocumentType type, string id = null) : base(id)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            DocumentType = type;
            Fields = fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Map.Add(Id, this);
        }
    }
}
