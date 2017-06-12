using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DashShared
{
    public abstract class EntityBase
    {
        /// <summary>
        /// Object unique identifier
        /// </summary>
        [Key] // key in the database
        [Required] // cannot be null
        [JsonProperty("id")] // serialized as id (for documentdb)
        public string Id { get; set; }

        /// <summary>
        /// The name of the entity, this is useful for search and provides a front end that
        /// can be displayed to the user
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Pretty print the key for debugging purposes
        /// </summary>
        /// <returns>The well formatted string</returns>
        public override string ToString()
        {
            return base.ToString() + $"Id:{Id} Name:{Name}";
        }
    }
}
