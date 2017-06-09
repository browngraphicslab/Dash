using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DashIntermediate
{
    public class DocumentType : Entity
    {

        /// <summary>
        /// The actual name of the type which is displayed but can change
        /// </summary>
        public string Name { get; set; }

    }
}
