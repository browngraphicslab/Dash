using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DashShared;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class DocumentModel : AuthorizableEntityBase
    {
        public static Dictionary<string, DocumentModel> Map = new Dictionary<string, DocumentModel>();

        /// <summary>
        /// A dictionary of <see cref="KeyModel.Id"/> to <see cref="FieldModel.Id"/>. These fields represent all the 
        /// data that is stored in the document model
        /// </summary>
        public Dictionary<string, string> Fields;

        /// <summary>
        /// The type of this document.
        /// </summary>
        public DocumentType DocumentType;
        private DocumentType type;

        private DocumentModel() : base(null)
        {
        }

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<KeyModel, FieldModelDTO> fields, DocumentType type, string id = null) : base(id)
        {
            DocumentType = type ?? throw new ArgumentNullException();
            Fields = fields.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value.Id);
            Map.Add(Id, this);
        }

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<KeyModel, string> fields, DocumentType type, string id = null) : base(id)
        {
            DocumentType = type ?? throw new ArgumentNullException();
            Fields = fields.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
            Map.Add(Id, this);
        }


    }
}
