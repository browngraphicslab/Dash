using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class FreeformViewModel : ViewModelBase
    {
        private FreeformModel _freeformModel;

        private bool _isEditorMode = true;

        public bool IsEditorMode
        {
            get { return _isEditorMode; }
            set { SetProperty(ref _isEditorMode, value); }
        }

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
