using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class ServerDocumentModel : EntityBase // TODO: AuthorizableEntityBase
    {
        public ServerDocumentModel(Dictionary<Key,string> fields, DocumentType type, string docId) {
            this.DocumentType = type;
            Id = docId;
            this.Fields = fields;
        }

        [Required]
        public Dictionary<Key, string> Fields { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

    }
}
