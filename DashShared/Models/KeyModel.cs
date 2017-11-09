using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared.Models;

namespace DashShared
{
    public class KeyModel : FieldModel
    {
        /// <summary>
        /// The name of the entity, this is useful for search and provides a front end that
        /// can be displayed to the user
        /// </summary>
        [Required]
        public string Name { get; set; }

        public KeyModel() : this(Guid.NewGuid().ToString())
        {
        }

        public KeyModel(string guid) : base(guid) {
            Name = guid;
        }

        public KeyModel(string guid, string name) : base(guid) {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            var k = obj as KeyModel;
            if (k == null)
            {
                return false;
            }
            return k.Id.Equals(Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Name; 
        }
    }
}
