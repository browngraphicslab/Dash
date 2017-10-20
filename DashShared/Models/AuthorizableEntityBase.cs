using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    /// <summary>
    /// Interface used to determine when an item is something that users must be authorized to view
    /// </summary>
    public abstract class AuthorizableEntityBase : EntityBase
    {
        public AuthorizableEntityBase(string id) : base(id)
        {
            
        }

        //[Required] // cannot be null
        //public string UserId { get; set; }

    }
}
