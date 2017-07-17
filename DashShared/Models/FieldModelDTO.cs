using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class FieldModelDTO : EntityBase // TODO: AuthorizableEntityBase
    {
        [Required]
        public object Data { get; set; }

        [Required]
        public TypeInfo Type { get; set; }
    }
}
