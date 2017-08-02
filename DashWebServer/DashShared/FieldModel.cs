using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class FieldModel : EntityBase // TODO: AuthorizableEntityBase
    {
        public object Data { get; set; }
    }
}
