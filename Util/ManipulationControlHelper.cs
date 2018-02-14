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
        DocumentView parent = null;
        Canvas freeformCanvas = null;
        public ManipulationControlHelper(FrameworkElement element)
        {
            parent = element.GetFirstAncestorOfType<DocumentView>();
            freeformCanvas = ((element.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView)?.xItemsControl.ItemsPanelRoot as Canvas);
        }

        private Point _rightDragLastPosition, _rightDragStartPosition;
        public void ForcePointerPressed()
        {
            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || parent.ManipulationControls == null) return;

            parent.ToFront();
            var pointerPosition = MainPage.Instance
                .TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core
                    .CoreWindow.GetForCurrentThread().PointerPosition);
            _rightDragStartPosition = _rightDragLastPosition = pointerPosition;
            parent.ManipulationControls?.ElementOnManipulationStarted(null, null);
            parent.DocumentView_PointerEntered(null, null);
        }
        public  void ForcePointerReleased()
        {
            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

            var delta = new Point(pointerPosition.X - _rightDragStartPosition.X, pointerPosition.Y - _rightDragStartPosition.Y);
            var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (dist < 100)
                parent.OnTapped(null, new TappedRoutedEventArgs());
            else
                parent.ManipulationControls?.ElementOnManipulationCompleted(null, null);
            var dvm = parent.ViewModel;
            parent.DocumentView_PointerExited(null, null);
            parent.DocumentView_ManipulationCompleted(null, null);
        }
        public void ForcePointerMove()
        { 
            var parentCollectionTransform = freeformCanvas?.RenderTransform as MatrixTransform;
            if (parentCollectionTransform == null || parent.ManipulationControls == null) return;

            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(CoreWindow.GetForCurrentThread().PointerPosition);

            var translation = new Point(pointerPosition.X - _rightDragLastPosition.X, pointerPosition.Y - _rightDragLastPosition.Y);

            translation.X *= parentCollectionTransform.Matrix.M11;
            translation.Y *= parentCollectionTransform.Matrix.M22;

            _rightDragLastPosition = pointerPosition;
            parent.ManipulationControls.TranslateAndScale(new
                ManipulationDeltaData(new Point(pointerPosition.X, pointerPosition.Y),
                    translation,
                    1.0f), parent.ManipulationControls._grouping);

            //Only preview a snap if the grouping only includes the current node. TODO: Why is _grouping public?
            if (parent.ManipulationControls._grouping == null || parent.ManipulationControls._grouping.Count < 2)
                parent.ManipulationControls.Snap(true);
        }
    }
}
