using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    class FreeformViewModel
    {
        private FreeformModel _freeformModel;
        
        public static FreeformViewModel Instance { get; private set; }

        public delegate void ElementAddedEvent(UIElement element, float left, float top);

        public event ElementAddedEvent OnElementAdded;

        public FreeformViewModel()
        {
            Debug.Assert(Instance == null);
            Instance = this;

            _freeformModel = new FreeformModel();
        }

        public void AddElement(UIElement element, float left, float top)
        {
            OnElementAdded?.Invoke(element, left, top);
            //_freeformModel.AddElement(element, left, top);
        }

    }
}
