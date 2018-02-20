﻿using DashShared;
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
        FrameworkElement   _eventElement;
        CollectionView     _collection;
        DocumentView       manipulationDocumentTarget = null;
        Canvas             freeformCanvas = null;
        PointerEventHandler move_hdlr;
        PointerEventHandler release_hdlr;
        Point _rightDragLastPosition, _rightDragStartPosition;
        int _numMovements;

        public ManipulationControlHelper(FrameworkElement eventElement, Pointer pointer, bool drillDown)
        {
            move_hdlr = new PointerEventHandler(pointerMoved);
            release_hdlr = new PointerEventHandler(pointerReleased);
            _eventElement = eventElement;
            _eventElement.AddHandler(UIElement.PointerReleasedEvent, release_hdlr, true);
            _eventElement.AddHandler(UIElement.PointerMovedEvent, move_hdlr, true);
            if (pointer != null)
                _eventElement.CapturePointer(pointer);

            var nestings = _eventElement.GetAncestorsOfType<CollectionView>().ToList();
            var manipTarget = nestings.Count() < 2 || drillDown ? _eventElement : nestings[nestings.Count - 2];
            manipulationDocumentTarget = manipTarget.GetFirstAncestorOfType<DocumentView>();
            freeformCanvas = ((manipTarget.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView)?.xItemsControl.ItemsPanelRoot as Canvas);
            _ancestorDocs = _eventElement.GetAncestorsOfType<DocumentView>().ToList();
            _ancestorDocs.AddRange(_eventElement.GetDescendantsOfType<DocumentView>());
            foreach (var n in _ancestorDocs)
                n.ManipulationMode = ManipulationModes.None;
            _collection = _eventElement as CollectionView;
            if (_collection != null)
                _collection.CurrentView.ManipulationMode = ManipulationModes.None;

            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || manipulationDocumentTarget.ManipulationControls == null) return;
            pointerPressed(_eventElement, null);
        }
        public void pointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _numMovements = 0;
            var pointerPosition = MainPage.Instance
                .TransformToVisual(manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core
                    .CoreWindow.GetForCurrentThread().PointerPosition);
            _rightDragStartPosition = _rightDragLastPosition = pointerPosition;
            manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationStarted(null, null);
            manipulationDocumentTarget.DocumentView_PointerEntered(null, null);
        }
        /// <summary>
        /// Move view around if right mouse button is held down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void pointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _numMovements++;
            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || manipulationDocumentTarget.ManipulationControls == null) return;

            var pointerPosition = MainPage.Instance.TransformToVisual(manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(CoreWindow.GetForCurrentThread().PointerPosition);
            var translation = new Point(pointerPosition.X - _rightDragLastPosition.X, pointerPosition.Y - _rightDragLastPosition.Y);
            
            _rightDragLastPosition = pointerPosition;
            manipulationDocumentTarget.ManipulationControls.TranslateAndScale(new Point(pointerPosition.X, pointerPosition.Y), translation, 1.0f, 
                        manipulationDocumentTarget.ManipulationControls.Grouping);

            //Only preview a snap if the grouping only includes the current node. 
            if (manipulationDocumentTarget.ManipulationControls.Grouping == null || manipulationDocumentTarget.ManipulationControls.Grouping.Count < 2)
                manipulationDocumentTarget.ManipulationControls.Snap(true);

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
                n.ManipulationMode = ManipulationModes.All;
            if (_collection != null)
                _collection.CurrentView.ManipulationMode = ManipulationModes.All;

            var pointerPosition = MainPage.Instance.TransformToVisual(manipulationDocumentTarget.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

            var delta = new Point(pointerPosition.X - _rightDragStartPosition.X, pointerPosition.Y - _rightDragStartPosition.Y);
            var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (dist < 100 && _numMovements < 10)
            {
                manipulationDocumentTarget?.OnTapped(null, new TappedRoutedEventArgs());
                if (e == null)  // this is only true for WebBox's.  In this case, we need to generate a rightTap on the WebBox event element to create its context menu even if the manipulation document tareet was a higher level collection
                    _eventElement.GetFirstAncestorOfType<DocumentView>()?.RightTap();
            }
            else
                manipulationDocumentTarget.ManipulationControls?.ElementOnManipulationCompleted(null, null);
            manipulationDocumentTarget.DocumentView_ManipulationCompleted(null, null);

            if (e != null)
                e.Handled = true;
        }
    }
}
