using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class FieldModel : EntityBase // TODO: AuthorizableEntityBase
    {
        [Required]
        public object Data { get; set; }

        [Required]
        public System.Type DocumentType { get; set; }

    }
}
