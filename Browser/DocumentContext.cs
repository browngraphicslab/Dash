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
        public long CreationTimeTicks { get; set; }

        public DateTime CreationTimeStamp => new DateTime(CreationTimeTicks);

        public override bool Equals(object obj)
        {
            if (obj is DocumentContext dc)
            {
                return dc.Url == Url &&
                       dc.Scroll == Scroll &&
                       dc.Title == Title;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
