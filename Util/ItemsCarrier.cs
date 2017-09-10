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
        public static ItemsCarrier Instance = new ItemsCarrier();

        public List<DocumentController> Payload { get; set; }
        public ICollectionViewModel Source { get; set; }
        public ICollectionViewModel Destination { get; set; }    

        public CollectionFreeformView StartingCollection { get; set; }
        private ItemsCarrier()
        {
            Payload = new List<DocumentController>();
        }
    }
}
