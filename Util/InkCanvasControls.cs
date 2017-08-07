﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Dash
{
    class InkCanvasControls
    {
        public InkFieldModelController InkFieldModelController;

        public InkCanvas XInkCanvas { get; set; }

        public bool IsDrawing { get; set; }

        public InkCanvasControls(InkCanvas inkCanvas)
        {
            XInkCanvas = inkCanvas;
            InkSettings.Presenters.Add(XInkCanvas.InkPresenter);
            InkSettings.SetAttributes();
            XInkCanvas.InkPresenter.InputDeviceTypes = InkSettings.InkInputType;
            XInkCanvas.InkPresenter.IsInputEnabled = IsDrawing;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            XInkCanvas.Loaded += OnLoaded;
            XInkCanvas.Tapped += XInkCanvasOnTapped;
        }

        private void XInkCanvasOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            IsDrawing = !IsDrawing;
            XInkCanvas.InkPresenter.IsInputEnabled = IsDrawing;
        }

        /// <summary>
        /// A control that contains an InkCanvas and interacts with an InkFieldModelController to reflect user strokes 
        /// on the canvas in the underlying data.
        /// </summary>
        /// <param name="inkFieldModelController"></param>
        public InkCanvasControls(InkCanvas inkCanvas, InkFieldModelController inkFieldModelController)
        {
            XInkCanvas = inkCanvas;
            InkSettings.Presenters.Add(XInkCanvas.InkPresenter);
            InkSettings.SetAttributes();
            XInkCanvas.InkPresenter.InputDeviceTypes = InkSettings.InkInputType;
            XInkCanvas.InkPresenter.IsInputEnabled = IsDrawing;
            InkFieldModelController = inkFieldModelController;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            XInkCanvas.Loaded += OnLoaded;
            InkFieldModelController.FieldModelUpdated += InkFieldModelControllerOnFieldModelUpdated;
        }

        private void InkFieldModelControllerOnFieldModelUpdated(FieldModelController sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!IsDrawing)
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
    }
}
