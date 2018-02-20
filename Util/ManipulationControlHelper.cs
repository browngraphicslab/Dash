using DashShared;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Controllers;
using static Dash.NoteDocuments;
using Dash.Controllers.Operators;
using Dash.Views;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;
using DashShared.Models;
using Flurl.Util;
using NewControls.Geometry;
using Syncfusion.Pdf.Graphics;


namespace Dash
{
    public class ManipulationControlHelper
    {
        List<DocumentView> _ancestorDocs;
        List<DocumentView> _descendantDocs;
        FrameworkElement   _eventElement;
        CollectionView     _collection;
        DocumentView       parent = null;
        Canvas             freeformCanvas = null;
        PointerEventHandler move_hdlr;
        PointerEventHandler release_hdlr;
        Point _rightDragLastPosition, _rightDragStartPosition;

        public ManipulationControlHelper(FrameworkElement element, FrameworkElement eventElement, Pointer pointer, bool drillDown)
        {
            move_hdlr = new PointerEventHandler(pointerMOved);
            release_hdlr = new PointerEventHandler(pointerReleased);
            _eventElement = eventElement;
            _eventElement.AddHandler(UIElement.PointerReleasedEvent, release_hdlr, true);
            _eventElement.AddHandler(UIElement.PointerMovedEvent, move_hdlr, true);
            if (pointer != null)
                _eventElement.CapturePointer(pointer);

            var nestings = element.GetAncestorsOfType<CollectionView>().ToList();
            var manipTarget = nestings.Count() < 2 || drillDown ? element : nestings[nestings.Count - 2];
            parent = manipTarget.GetFirstAncestorOfType<DocumentView>();
            freeformCanvas = ((manipTarget.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView)?.xItemsControl.ItemsPanelRoot as Canvas);
            _ancestorDocs = element.GetAncestorsOfType<DocumentView>().ToList();
            foreach (var n in _ancestorDocs)
                n.OuterGrid.ManipulationMode = ManipulationModes.None;
            _descendantDocs = element.GetDescendantsOfType<DocumentView>().ToList();
            foreach (var n in _descendantDocs)
                n.OuterGrid.ManipulationMode = ManipulationModes.None;
            _collection = element as CollectionView;
            if (_collection != null)
                _collection.CurrentView.ManipulationMode = ManipulationModes.None;

            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || parent.ManipulationControls == null) return;
            pointerPressed(element, null);
        }
        public void pointerPressed(object sender, PointerRoutedEventArgs e) { 

            parent.ToFront();
            var pointerPosition = MainPage.Instance
                .TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core
                    .CoreWindow.GetForCurrentThread().PointerPosition);
            _rightDragStartPosition = _rightDragLastPosition = pointerPosition;
            parent.ManipulationControls?.ElementOnManipulationStarted(null, null);
            parent.DocumentView_PointerEntered(null, null);
        }
        /// <summary>
        /// Move view around if right mouse button is held down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void pointerMOved(object sender, PointerRoutedEventArgs e)
        {
            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || parent.ManipulationControls == null) return;

            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(CoreWindow.GetForCurrentThread().PointerPosition);

            var translation = new Point(pointerPosition.X - _rightDragLastPosition.X, pointerPosition.Y - _rightDragLastPosition.Y);
            
            _rightDragLastPosition = pointerPosition;
            parent.ManipulationControls.TranslateAndScale(new
                ManipulationDeltaData(new Point(pointerPosition.X, pointerPosition.Y),
                    translation,
                    1.0f), parent.ManipulationControls._grouping);

            //Only preview a snap if the grouping only includes the current node. TODO: Why is _grouping public?
            if (parent.ManipulationControls._grouping == null || parent.ManipulationControls._grouping.Count < 2)
                parent.ManipulationControls.Snap(true);

            if (e != null)
                e.Handled = true;
        }

        public void pointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_eventElement != null)
            {
                if (e != null)
                    _eventElement.ReleasePointerCapture(e.Pointer);
                _eventElement.RemoveHandler(UIElement.PointerReleasedEvent, release_hdlr);
                _eventElement.RemoveHandler(UIElement.PointerMovedEvent, move_hdlr);
            }
            foreach (var n in _ancestorDocs)
                n.OuterGrid.ManipulationMode = ManipulationModes.All;
            foreach (var n in _descendantDocs)
                n.OuterGrid.ManipulationMode = ManipulationModes.All;
            if (_collection != null)
                _collection.CurrentView.ManipulationMode = ManipulationModes.All;

            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

            var delta = new Point(pointerPosition.X - _rightDragStartPosition.X, pointerPosition.Y - _rightDragStartPosition.Y);
            var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (dist < 100)
            {
                parent.OnTapped(null, new TappedRoutedEventArgs());
                if (e == null)
                    _eventElement.GetFirstAncestorOfType<DocumentView>()?.RightTap();
            }
            else
                parent.ManipulationControls?.ElementOnManipulationCompleted(null, null);
            parent.DocumentView_ManipulationCompleted(null, null);

            if (e != null)
                e.Handled = true;
        }
    }
}
