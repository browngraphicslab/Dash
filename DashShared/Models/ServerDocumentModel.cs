using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class ServerDocumentModel : EntityBase // TODO: AuthorizableEntityBase
    {

        public ServerDocumentModel()
        {

        }
        public ServerDocumentModel(Dictionary<Key,string> fields, DocumentType type, string docId) {
            this.DocumentType = type;
            Id = docId;

            Fields = new Dictionary<string, string>();
            foreach (KeyValuePair<Key,string> item in fields)
                Fields.Add(item.Key.ToString(), item.Value);
        }

        [Required]
        public Dictionary<string, string> Fields { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

    }
}
