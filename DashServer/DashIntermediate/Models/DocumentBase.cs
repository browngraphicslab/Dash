using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DashIntermediate;

namespace DashIntermediate
{
    public abstract class DocumentBase : Entity
    {
        /// <summary>
        /// Document Type
        /// </summary>
        [Required] // cannot be null
        public DocumentType DocumentType { get; private set; }

        public Dictionary<Key, object> Fields { get; set; }

        protected DocumentBase(DocumentType documentType)
        {
            DocumentType = documentType;
        }



    }
}
