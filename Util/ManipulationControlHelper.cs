using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

namespace Dash
{
    public class ManipulationControlHelper
    {
        List<DocumentView> _ancestorDocs;
        FrameworkElement   _eventElement;
        CollectionView     _collection;
        DocumentView       _manipulationDocumentTarget = null;
        Panel freeformCanvas = null;
        PointerEventHandler move_hdlr;
        PointerEventHandler release_hdlr;
        Point _rightDragLastPosition, _rightDragStartPosition;
        int _numMovements;
        private bool _useCache;

        public ManipulationControlHelper(FrameworkElement eventElement, Pointer pointer, bool drillDown, bool useCache = false)
        {
            _useCache = useCache;
            move_hdlr = new PointerEventHandler((sender, e) => PointerMoved(sender, e));
            release_hdlr = new PointerEventHandler(PointerReleased);
            _eventElement = eventElement;

            var nestings = _eventElement.GetAncestorsOfType<CollectionView>().ToList();
            var manipTarget = (nestings.Count() < 2 || drillDown) ? _eventElement : nestings[nestings.Count - 2];
            var docAncestors = manipTarget.GetAncestorsOfType<DocumentView>().ToList();
            _manipulationDocumentTarget = docAncestors[docAncestors.Count > 3 ? 1 : 0];// manipTarget.GetFirstAncestorOfType<DocumentView>();
            freeformCanvas = ((manipTarget.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase)?.GetCanvas());
            _ancestorDocs = _eventElement.GetAncestorsOfType<DocumentView>().ToList();
            _ancestorDocs.AddRange(_eventElement.GetDescendantsOfType<DocumentView>());
            foreach (var n in _ancestorDocs)
                n.ManipulationMode = ManipulationModes.None;
            _collection = _eventElement as CollectionView;
            if (_collection != null)
                _collection.CurrentView.ManipulationMode = ManipulationModes.None;

            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || _manipulationDocumentTarget.ManipulationControls == null) return;
            pointerPressed(_eventElement, null);
            _manipulationDocumentTarget.PointerId = pointer.PointerId;
            if (false) // bcz: set to 'true' for drag/Drop interactions
                _manipulationDocumentTarget.SetupDragDropDragging(null);
            else
            {
                _eventElement.AddHandler(UIElement.PointerReleasedEvent, release_hdlr, true);
                _eventElement.AddHandler(UIElement.PointerMovedEvent, move_hdlr, true);
                if (!_eventElement.IsShiftPressed() && pointer != null)
                    _eventElement.CapturePointer(pointer);
            }
        }


        public void pointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _numMovements = 0;
            var pointerPosition = _manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>().PointerPos();
            _rightDragStartPosition = _rightDragLastPosition = pointerPosition;
            _manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationStarted();
            _manipulationDocumentTarget.DocumentView_PointerEntered();
            if (_useCache) _eventElement.CacheMode = null;
            //MainPage.Instance.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Move view around if right mouse button is held down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e?.Pointer != null)
                _eventElement.CapturePointer(e.Pointer);
            _numMovements++;
            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || _manipulationDocumentTarget.ManipulationControls == null) return;
            
            var pointerPosition = _manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>().PointerPos();
            var translationBeforeAlignment = new Point(pointerPosition.X - _rightDragLastPosition.X, pointerPosition.Y - _rightDragLastPosition.Y);
            
            _rightDragLastPosition = pointerPosition;

            var translationAfterAlignment = _manipulationDocumentTarget.ManipulationControls.SimpleAlign(translationBeforeAlignment);

            _manipulationDocumentTarget.ManipulationControls.TranslateAndScale(new Point(pointerPosition.X, pointerPosition.Y), translationAfterAlignment, 1.0f);

            if (e != null)
                e.Handled = true;
        }

        public void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_eventElement != null)
            {
                if (e != null)
                    _eventElement.ReleasePointerCapture(e.Pointer);
                _eventElement.RemoveHandler(UIElement.PointerReleasedEvent, release_hdlr);
                _eventElement.RemoveHandler(UIElement.PointerMovedEvent, move_hdlr);
            }
            foreach (var n in _ancestorDocs)
                n.ManipulationMode = ManipulationModes.All;
            if (_collection != null)
                _collection.CurrentView.ManipulationMode = ManipulationModes.All;
            
            var pointerPosition = _manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>().PointerPos();

            var delta = new Point(pointerPosition.X - _rightDragStartPosition.X, pointerPosition.Y - _rightDragStartPosition.Y);
            var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (dist < 100 && _numMovements < 10)
            {
                _manipulationDocumentTarget?.DocumentView_OnTapped(null, new TappedRoutedEventArgs());
                if (e == null)  // this is only true for WebBox's.  In this case, we need to generate a rightTap on the WebBox event element to create its context menu even if the manipulation document tareet was a higher level collection
                    _eventElement.GetFirstAncestorOfType<DocumentView>()?.ForceRightTapContextMenu();
                _manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationCompleted(true);
            }
            else
                _manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationCompleted();

            if (_useCache) _eventElement.CacheMode = new BitmapCache();
            if (e != null)
                e.Handled = true;
        }
    }
}
