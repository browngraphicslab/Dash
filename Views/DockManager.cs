using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    /// <summary>
    /// Each DockingFrame uses this as a manager class to handle saving, loading, and other complex logic-based systems.
    /// </summary>
    class DockManager
    {
        public DocumentController DocControl { get; set; }
        private DockingFrame _frame;

        private readonly bool[] _firstDock = { true, true, true, true };
        private readonly DockingFrame[] _lastDockedViews = { null, null, null, null };

        public DockManager(DockingFrame frame)
        {
            _frame = frame;
        }

        public void Dock(DocumentController toDock, DockDirection dir)
        {
            toDock = toDock.GetViewCopy();
            DocumentView copiedView = new DocumentView
            {
                DataContext = new DocumentViewModel(toDock),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ViewModel = { Width = Double.NaN, Height = Double.NaN, DisableDecorations = true }
            };

            DockingFrame dockedView = new DockingFrame
            {
                DocumentContent = copiedView,
                ParentFrame = _frame,
                Direction = dir,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            if (_firstDock[(int)dir])
            {
                double length = 300;

                if (toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null) == null)
                    toDock.SetField(KeyStore.DockedLength, new NumberController(length), true);
                else
                    length = toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null).Data;

                _frame.Dock(dockedView, dir, MainPage.GridSplitterThickness, length);

                _firstDock[(int)dir] = false;
            }
            else
            {
                var lastView = _lastDockedViews[(int) dir];
                dockedView.Dock(lastView, dir);
                _frame.ReplaceView(dir, dockedView, _lastDockedViews[(int)dir]);

                // if there's no previous saved length, then set it. Otherwise, set it to that length.
                if (toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null) == null)
                    toDock.SetField(KeyStore.DockedLength, new NumberController(_frame.GetNestedViewSize(dir)), true);
                else
                    _frame.SetNestedViewSize(dir, toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null).Data);
            }

            // write these into the database
            switch (dir)
            {
                case DockDirection.Left:
                    DocControl.SetField(KeyStore.DockedLeftKey, toDock, true);
                    break;
                case DockDirection.Right:
                    DocControl.SetField(KeyStore.DockedRightKey, toDock, true);
                    break;
                case DockDirection.Top:
                    DocControl.SetField(KeyStore.DockedTopKey, toDock, true);
                    break;
                case DockDirection.Bottom:
                    DocControl.SetField(KeyStore.DockedBottomKey, toDock, true);
                    break;
            }

            _lastDockedViews[(int)dir] = dockedView;
        }
        
        // undocks self
        public void Undock()
        {
            // TODO: do this later once loading is figured out (will likely use many of the same mechanics)
        }

        public void LoadDockedItems()
        {
            // TODO 
        }
    }
}
