using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class ShapeModel : AuthorizableEntityBase
    {
        [Required]
        public double Width { get; set; }

        [Required]
        public double Height { get; set; }

        [Required]
        public double X { get; set; }

        [Required]
        public double Y { get; set; }
    }
}
