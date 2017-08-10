using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DashShared
{

    public class FieldModelDTO : EntityBase
    {
        public FieldModelDTO(TypeInfo type, object data, string id = null) : base(id)
        {
            Data = data;
            Type = type;
        }

        [Required]
        public object Data { get; set; }

        [Required]
        public TypeInfo Type { get; set; }
        
    }
}
