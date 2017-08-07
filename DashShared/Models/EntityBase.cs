﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DashShared
{
    /// <summary>
    /// the base class for anything that is stored in the Database, with few exceptions every model
    /// should inherit from this class.
    /// </summary>
    public abstract class EntityBase
    {

        protected EntityBase(string id = null)
        {
            Id = id ?? Util.GenerateNewId();

        }

        /// <summary>
        /// Object unique identifier
        /// </summary>
        [Key] // key in the database
        [Required] // cannot be null
        [JsonProperty("id")] // serialized as id (for documentdb)
        public string Id { get; set; }

        /// <summary>
        /// Pretty print the key for debugging purposes
        /// </summary>
        /// <returns>The well formatted string</returns>
        public override string ToString()
        {
            return $"Id:{Id}" + base.ToString();
        }
    }
}
