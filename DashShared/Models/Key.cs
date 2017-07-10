using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class Key : EntityBase
    {
        /// <summary>
        /// The name of the entity, this is useful for search and provides a front end that
        /// can be displayed to the user
        /// </summary>
        [Required]
        public string Name { get; set; }

        public Key()
        {
        }

        public Key(string guid)
        {
            Id = guid;
        }

        public Key(string guid, string name)
        {
            Id = guid;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            Key k = obj as Key;
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
