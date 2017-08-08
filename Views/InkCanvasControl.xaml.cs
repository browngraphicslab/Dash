using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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

namespace Dash
{
    public sealed partial class InkCanvasControl : SelectionElement
    {
        public InkFieldModelController InkFieldModelController;
        private readonly bool _isInterfaceBuilder;
        private ManipulationControls _controls;

        public Grid Grid => XGrid;

        /// <summary>
        /// A control that contains an InkCanvas and interacts with an InkFieldModelController to reflect user strokes 
        /// on the canvas in the underlying data.
        /// </summary>
        /// <param name="inkFieldModelController"></param>
        public InkCanvasControl(InkFieldModelController inkFieldModelController, bool isInterfaceBuilder)
        {
            this.InitializeComponent();
            _isInterfaceBuilder = isInterfaceBuilder;
            GlobalInkSettings.Presenters.Add(XInkCanvas.InkPresenter);
            GlobalInkSettings.SetAttributes();
            XInkCanvas.InkPresenter.InputDeviceTypes = GlobalInkSettings.InkInputType;
            InkFieldModelController = inkFieldModelController;
            _isInterfaceBuilder = isInterfaceBuilder;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            InkFieldModelController.FieldModelUpdated += InkFieldModelControllerOnFieldModelUpdated;
            Loaded += OnLoaded;
            XInkCanvas.Tapped += OnTapped;
            Tapped += OnTapped;
            OnLowestActivated(false);
        }
        

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!_isInterfaceBuilder)
            {
                OnSelected();
                e.Handled = true;
            }
        }

        private void InkFieldModelControllerOnFieldModelUpdated(FieldModelController sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!IsLowestSelected || args?.Action == DocumentController.FieldUpdatedAction.Replace)
            {
                UpdateStrokes();
            }
        }

        private void UpdateStrokes()
        {
            XInkCanvas.InkPresenter.StrokeContainer.Clear();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
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
            ScrollViewer.ChangeView(1000 - ActualWidth / 2, 1000 - ActualHeight / 2, 1);
        }

        /// <summary>
        /// When strokes are erased, modifies the controller's Strokes field to remove those strokes.
        /// Then calls update data on the controller so that the field model reflects the changes.
        /// TODO: the field model need not be updated with every stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            if (InkFieldModelController != null)
                InkFieldModelController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            
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
                InkFieldModelController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
        }

        protected override void OnActivated(bool isSelected)
        {
            // Do nothing
        }

        protected override void OnLowestActivated(bool act)
        {
            UpdateStrokes();
            //When lowest activated, ink canvas is drawable
            if (act)
            {
                EditingSymbol.Foreground = new SolidColorBrush(Colors.Black);
                EditButton.IsHitTestVisible = true;
                XGrid.BorderBrush = (SolidColorBrush)Application.Current.Resources["WindowsBlue"];
                XInkCanvas.InkPresenter.IsInputEnabled = true;
                ScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
                ScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
                ManipulationMode = ManipulationModes.None;
            } else
            {
                EditingSymbol.Foreground = new SolidColorBrush(Colors.LightGray);
                XGrid.BorderBrush = new SolidColorBrush(Colors.Black);
                XInkCanvas.InkPresenter.IsInputEnabled = false;
                ScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                ScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                if(InkToolbar.Visibility == Visibility.Visible) xCollapseSettings.Begin();
                ManipulationMode = ManipulationModes.All;
                EditButton.IsHitTestVisible = false;
                
            }
        }

        private void XCollapseSettingsOnCompleted(object sender, object o)
        {
            InkToolbar.Visibility = Visibility.Collapsed;
            RedoButton.Visibility = Visibility.Collapsed;
            UndoButton.Visibility = Visibility.Collapsed;
            ToolbarScroller.Visibility = Visibility.Collapsed;
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        private void SelectionElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            XGrid.Width = Width;
            XGrid.Height = Height;
        }

        private void EditButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (InkToolbar.Visibility == Visibility.Visible)
            {
                xCollapseSettings.Begin();
            }
            else
            {
                InkToolbar.Visibility = Visibility.Visible;
                RedoButton.Visibility = Visibility.Visible;
                UndoButton.Visibility = Visibility.Visible;
                ToolbarScroller.Visibility = Visibility.Visible;
                xExpandSettings.Begin();
            }
        }

        private void RedoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Redo();
        }

        private void UndoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Undo();
        }
    }
}
