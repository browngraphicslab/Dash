using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ItemsCarrier
    {
        private static ItemsCarrier _carrier = new ItemsCarrier();
        public List<DocumentController> Payload { get; set; }
        public CollectionView Source { get; set; }
        private ItemsCarrier()
        {
            Payload = new List<DocumentController>();
        }

        public static ItemsCarrier GetInstance()
        {
            return _carrier;
        }
    }
}
