using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DashShared
{
    public class Key : Entity
    {
        public Key(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The actual name of the key which is displayed but can change
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Pretty print the key for debugging purposes
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + $"Id:{Id} Name:{Name}";
        }
    }
}
