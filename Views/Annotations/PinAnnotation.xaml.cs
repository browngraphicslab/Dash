using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PinAnnotation
    {
        public PinAnnotation(NewAnnotationOverlay parent, Selection selectionViewModel) : 
            base(parent, selectionViewModel.RegionDocument)
        {
            this.InitializeComponent();

            DataContext = selectionViewModel;

            AnnotationType = AnnotationType.Pin;

            InitializeAnnotationObject(xShape, null, PlacementMode.Top);

            PointerPressed += (s, e) => e.Handled = true;

            //handlers for moving pin
            ManipulationMode = ManipulationModes.All;
            ManipulationStarted += (s, e) =>
            {
                ManipulationMode = ManipulationModes.All;
                e.Handled = true;
            };
            ManipulationDelta += (s, e) =>
            {
                var curPos = RegionDocumentController.GetPosition() ?? new Point();
                var p = Util.DeltaTransformFromVisual(e.Delta.Translation, s as UIElement);
                RegionDocumentController.SetPosition(new Point(curPos.X +p.X, curPos.Y + p.Y));
                e.Handled = true;
            };
        }


        #region Unimplemented Methods
        public override void StartAnnotation(Point p)
        {
        }

        public override void UpdateAnnotation(Point p)
        {
        }

        public override void EndAnnotation(Point p)
        {
        }

        public override double AddToRegion(DocumentController region)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
