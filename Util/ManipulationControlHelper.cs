using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
        bool _useCache;

        Point GetPointerPosition()
        {
            var container = _manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>() as FrameworkElement;
            return container.PointerPos();
        }

        public ManipulationControlHelper(FrameworkElement eventElement, PointerRoutedEventArgs pointer, bool drillDown, bool useCache = false)
        {
            _useCache = useCache;
            move_hdlr = new PointerEventHandler((sender, e) => PointerMoved(sender, e));
            release_hdlr = new PointerEventHandler(PointerReleased);
            _eventElement = eventElement;

            var nestings = _eventElement.GetAncestorsOfType<CollectionView>().ToList();
            var manipTarget = (nestings.Count() < 2 || drillDown) ? _eventElement : nestings[nestings.Count - 2];
            var docAncestors = eventElement.GetAncestorsOfType<DocumentView>();
            _manipulationDocumentTarget = docAncestors.First();// manipTarget.GetFirstAncestorOfType<DocumentView>();
            freeformCanvas = ((manipTarget.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase)?.GetCanvas());
            _ancestorDocs = _eventElement.GetAncestorsOfType<DocumentView>().ToList();
            _ancestorDocs.AddRange(_eventElement.GetDescendantsOfType<DocumentView>());
            foreach (var n in _ancestorDocs)
                n.ManipulationMode = ManipulationModes.None;
            _collection = _eventElement as CollectionView;
            if (_collection != null)
                _collection.CurrentView.UserControl.ManipulationMode = ManipulationModes.None;
			
            //if (_manipulationDocumentTarget.ManipulationControls == null) return;
            pointerPressed(_eventElement, pointer);

            _manipulationDocumentTarget.PointerId = (pointer?.Pointer is Pointer pt) ? pt.PointerId : 1;
            
            //_eventElement.AddHandler(UIElement.PointerReleasedEvent, release_hdlr, true);
            //_eventElement.AddHandler(UIElement.PointerMovedEvent, move_hdlr, true);
            //if (!_eventElement.IsShiftPressed() && pointer != null)
            //    _eventElement.CapturePointer(pointer.Pointer);
        }

        private bool _pointerPressed = false;

        public void pointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.IsRightPressed()) _pointerPressed = true;
            _numMovements = 0;
            var pointerPosition = GetPointerPosition();
            _rightDragStartPosition = _rightDragLastPosition = pointerPosition;
            //_manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationStarted();
            if (_useCache) _eventElement.CacheMode = null;
        }


        /// <summary>
        /// Move view around if right mouse button is held down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_pointerPressed && e.IsRightPressed() && e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.Other)
            {
                _pointerPressed = false;
                _manipulationDocumentTarget.StartManipulation(e);
            }
            else if (e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                _pointerPressed = false;
            }
            //if (e?.Pointer != null)
            //    _eventElement.CapturePointer(e.Pointer);
            //_numMovements++;
            ////if (_manipulationDocumentTarget.ManipulationControls == null) return;
            
            //var pointerPosition = GetPointerPosition();
            //var translationBeforeAlignment = new Point(pointerPosition.X - _rightDragLastPosition.X, pointerPosition.Y - _rightDragLastPosition.Y);
            
            //_rightDragLastPosition = pointerPosition;

            //var translationAfterAlignment = _manipulationDocumentTarget.ManipulationControls.SimpleAlign(translationBeforeAlignment);

            //_manipulationDocumentTarget.ManipulationControls.TranslateAndScale(new Point(pointerPosition.X, pointerPosition.Y), translationAfterAlignment, 1.0f, null, this, e);

            //if (e != null)
            //    e.Handled = true;
        }

        public void Abort(PointerRoutedEventArgs e)
        {
            foreach (var n in _ancestorDocs)
                n.ManipulationMode = ManipulationModes.All;
            if (_collection != null)
                _collection.CurrentView.UserControl.ManipulationMode = ManipulationModes.All;
            _eventElement.RemoveHandler(UIElement.PointerReleasedEvent, release_hdlr);
            _eventElement.RemoveHandler(UIElement.PointerMovedEvent, move_hdlr);
            if (e!= null)
                _eventElement.ReleasePointerCapture(e.Pointer);
            if (_useCache) _eventElement.CacheMode = new BitmapCache();
            if (_eventElement is WebView web)
                web.Tag = null;
        }

        public void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pointerPressed = false;
            //if (_eventElement != null)
            //{
            //    if (e != null)
            //        _eventElement.ReleasePointerCapture(e.Pointer);
            //    _eventElement.RemoveHandler(UIElement.PointerReleasedEvent, release_hdlr);
            //    _eventElement.RemoveHandler(UIElement.PointerMovedEvent, move_hdlr);
            //}
            //foreach (var n in _ancestorDocs)
            //    n.ManipulationMode = ManipulationModes.All;
            //if (_collection != null)
            //    _collection.CurrentView.UserControl.ManipulationMode = ManipulationModes.All;

            //var pointerPosition = GetPointerPosition();

            //var delta = new Point(pointerPosition.X - _rightDragStartPosition.X, pointerPosition.Y - _rightDragStartPosition.Y);
            //var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            //if (dist < 100 && _numMovements < 10)
            //{
            //    _manipulationDocumentTarget?.TappedHandler(false);
            //    if (e == null)  // this is only true for WebBox's.  In this case, we need to generate a rightTap on the WebBox event element to create its context menu even if the manipulation document tareet was a higher level collection
            //        _eventElement.GetFirstAncestorOfType<DocumentView>()?.ForceRightTapContextMenu();
            //    _manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationCompleted(true);
            //}
            //else
            //    _manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationCompleted();

            //if (_useCache) _eventElement.CacheMode = new BitmapCache();
            //if (e != null)
            //    e.Handled = true;

            //if (_eventElement is RichTextView rtv)
            //{
            //    rtv.CompletedManipulation();
            //}
        }
    }
}
