using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class FreeformViewModel
    {
       private FreeformModel _freeformModel;
        
        //public static FreeformViewModel Instance { get; private set; }

        public delegate void ElementAddedEvent(UIElement element, float left, float top);

        public event ElementAddedEvent ElementAdded;

        public FreeformViewModel()
        {
            //Debug.Assert(Instance == null);
            //Instance = this;

            _freeformModel = new FreeformModel();
        }

        public void AddElement(UIElement element, float left, float top)
        {
            ElementAdded?.Invoke(element, left, top);
        }

    }
}
