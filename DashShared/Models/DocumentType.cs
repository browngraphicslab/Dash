using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class DocumentType : EntityBase
    {
        // reserved default display for layoutless documents
        public static DocumentType DefaultType = new DocumentType("B492D995-701B-4703-B867-8C957762E352","Default");

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

        public override string ToString()
        {
            return Type;
        }
    }
}
