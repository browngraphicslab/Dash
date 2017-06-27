using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace DashServer
{
    public class Product
    {
        [Required] // cannot be null
        public int MyId { get; set; } 
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Type = "Product";
    }
}