using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class DocumentModel : EntityBase // TODO: AuthorizableEntityBase
    {
        [Required]
        public Dictionary<Key, string> Fields { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

    }
}
