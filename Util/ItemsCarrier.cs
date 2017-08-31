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
        public CollectionView SourceCollection { get; set; }

        public ICollectionViewModel _source { get; private set; } 
        public ICollectionViewModel Source
        {
            get
            {
                if (SourceCollection == null) return _source; 
                return SourceCollection.ViewModel;
            }
            set { _source = value; }
        }
        public ICollectionViewModel Destination { get; set; }

        //public BaseCollectionViewModel CurrBaseModel { get; set; } = (MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView).ViewModel;
        public ICollectionView CurrBaseModel { get; set; } = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

        public CollectionFreeformView StartingCollection { get; set; }
        private ItemsCarrier()
        {
            Payload = new List<DocumentController>();
        }
    }
}
