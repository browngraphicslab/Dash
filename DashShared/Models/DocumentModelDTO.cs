using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class DocumentModelDTO : EntityBase
    {
        [Required]
        public List<KeyModel> KeyList;

        [Required]
        public List<FieldModelDTO> FieldList;

        [Required]
        public DocumentType DocumentType;

        public DocumentModelDTO(List<FieldModelDTO> fieldList, List<KeyModel> keyList, DocumentType type, string id = null) : base(id)
        {
            FieldList = fieldList ?? new List<FieldModelDTO>();
            KeyList = keyList ?? new List<KeyModel>();
            DocumentType = type;
        }

    }
}
