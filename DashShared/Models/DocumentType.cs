using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class DocumentType : EntityBase
    {

        /// <summary>
        /// The actual name of the type which is displayed but can change
        /// </summary>
        public string Type { get; set; }

        public DocumentType()
        {
        }

        public DocumentType(string guid)
        {
            Id = guid;
        }

        public DocumentType(string guid, string type)
        {
            Id = guid;
            Type = type;
        }
    }
}
