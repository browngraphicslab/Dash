using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DashIntermediate
{
    public abstract class Entity
    {
        /// <summary>
        /// Object unique identifier
        /// </summary>
        [Key] // key in the database
        [Required] // cannot be null
        [JsonProperty("id")] // serialized as id (for documentdb)
        public string Id { get; set; }
    }
}
