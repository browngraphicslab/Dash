using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class DocumentType : Entity
    {

        /// <summary>
        /// The actual name of the type which is displayed but can change
        /// </summary>
        public string Name { get; set; }

    }
}
