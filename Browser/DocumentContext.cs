using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentContext : EntityBase
    {
        public double Scroll { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public double ViewDuration { get; set; }


        public override bool Equals(object obj)
        {
            return (obj as DocumentContext)?.Url == Url &&
                   (obj as DocumentContext)?.Scroll.Equals(Scroll) == true &&
                   (obj as DocumentContext)?.Title == Title;
        }
    }
}
