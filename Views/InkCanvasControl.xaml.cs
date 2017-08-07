using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class InkCanvasControl : SelectionElement
    {
        public InkFieldModelController InkFieldModelController;
        
        /// <summary>
        /// A control that contains an InkCanvas and interacts with an InkFieldModelController to reflect user strokes 
        /// on the canvas in the underlying data.
        /// </summary>
        /// <param name="inkFieldModelController"></param>
        public InkCanvasControl(InkFieldModelController inkFieldModelController)
        {
            this.InitializeComponent();
            InkSettings.Presenters.Add(XInkCanvas.InkPresenter);
            InkSettings.SetAttributes();
            XInkCanvas.InkPresenter.InputDeviceTypes = InkSettings.InkInputType;
            InkFieldModelController = inkFieldModelController;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            InkFieldModelController.FieldModelUpdated += InkFieldModelControllerOnFieldModelUpdated;
            Loaded += OnLoaded;
            Tapped += OnTapped;
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            OnSelected();
            e.Handled = true;
        }

        private void InkFieldModelControllerOnFieldModelUpdated(FieldModelController sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!IsLowestSelected)
            {
                XInkCanvas.InkPresenter.StrokeContainer.Clear();
                if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                    XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
            }
        }

        /// <summary>
        /// If the field model already has strokes, adds them to the new ink canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            XInkCanvas.InkPresenter.StrokeContainer = new InkStrokeContainer();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
        }

        /// <summary>
        /// When strokes are erased, modifies the controller's Strokes field to remove those strokes.
        /// Then calls update data on the controller so that the field model reflects the changes.
        /// TODO: the field model need not be updated with every stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
                if (InkFieldModelController != null)
                    InkFieldModelController.UpdateStrokesData(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            
        }

        /// <summary>
        /// When strokes are collected, adds them to the controller's HashSet of InkStrokes.
        /// Then calls update data on the controller so that the field model reflects the changes.
        /// TODO: the field model need not be updated with every stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            if (InkFieldModelController != null)
                InkFieldModelController.UpdateStrokesData(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
        }

        protected override void OnActivated(bool isSelected)
        {
            // Do nothing
        }

        protected override void OnLowestActivated(bool act)
        {
            //When lowest activated, ink canvas is drawable
            if (act)
            {
                XInkCanvas.ManipulationMode = ManipulationModes.None;
            }
        }
    }
}
