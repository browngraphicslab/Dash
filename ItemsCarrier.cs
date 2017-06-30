using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    public class ItemsCarrier
    {
        private static ItemsCarrier _carrier = new ItemsCarrier();
        public List<DocumentViewModel> Payload { get; set; }
        public CollectionView Source { get; set; }
        public CollectionView Destination { get; set; }    
        public Point Translate { get; set; }
        private ItemsCarrier()
        {
            Payload = new List<DocumentViewModel>();
        }

        public static ItemsCarrier GetInstance()
        {
            return _carrier;
        }
    }
}
