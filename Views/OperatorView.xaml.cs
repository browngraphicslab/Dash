using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.ViewModels;
using DashShared;
using Newtonsoft.Json;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorView : UserControl
    {
        public delegate void IODragEventHandler(IOReference ioReference);

        /// <summary>
        /// Event that gets fired when an ellipse is dragged off of and a connection should be started
        /// </summary>
        public event IODragEventHandler IoDragStarted;

        /// <summary>
        /// Event that gets fired when an ellipse is dragged on to and a connection should be ended
        /// </summary>
        public event IODragEventHandler IoDragEnded;

        /// <summary>
        /// Reference to either a field input or output with other information about the pointer
        /// </summary>
        public class IOReference
        {
            public ReferenceFieldModel ReferenceFieldModel { get; set; }
            public bool IsOutput { get; set; }

            public Pointer Pointer { get; set; }

            public Ellipse Ellipse { get; set; }

            public IOReference(ReferenceFieldModel referenceFieldModel, bool isOutput, Pointer pointer, Ellipse e)
            {
                ReferenceFieldModel = referenceFieldModel;
                IsOutput = isOutput;
                Pointer = pointer;
                Ellipse = e;
            }
        }

        public OperatorView()
        {
            this.InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            InputListView.ItemsSource = (args.NewValue as OperatorFieldModel).Inputs;//TODO Make these binding in XAML
            OutputListView.ItemsSource = (args.NewValue as OperatorFieldModel).Outputs;//TODO Make these binding in XAML
        }

        /// <summary>
        /// Can return the position of the click in screen space;
        /// Gets the OperatorFieldModel associated with the input ellipse that is clicked 
        /// </summary>
        private void InputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                string docId = (DataContext as OperatorFieldModel).DocumentID;
                Ellipse el = sender as Ellipse;
                Key outputKey = el.DataContext as Key;
                IOReference ioRef = new IOReference(new ReferenceFieldModel(docId, outputKey), false, e.Pointer, el);
                OnIoDragStarted(ioRef);
            }
        }

        /// <summary>
        /// Can return the position of the click in screen space;
        /// Gets the OperatorFieldModel associated with the output ellipse that is clicked 
        /// </summary>
        private void OutputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                string docId = (DataContext as OperatorFieldModel).DocumentID;
                Ellipse el = sender as Ellipse;
                Key outputKey = el.DataContext as Key;
                IOReference ioRef = new IOReference(new ReferenceFieldModel(docId, outputKey), true, e.Pointer, el);
                OnIoDragStarted(ioRef);
            }
        }

        /// <summary>
        /// Keep operator view from moving when you drag on an ellipse
        /// </summary>
        private void Ellipse_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void OnIoDragStarted(IOReference ioreference)
        {
            IoDragStarted?.Invoke(ioreference);
        }

        private void OnIoDragEnded(IOReference ioreference)
        {
            IoDragEnded?.Invoke(ioreference);
        }


        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (DataContext as OperatorFieldModel).DocumentID;
            Ellipse el = sender as Ellipse;
            Key outputKey = el.DataContext as Key;
            IOReference ioRef = new IOReference(new ReferenceFieldModel(docId, outputKey), false, e.Pointer, el);
            OnIoDragEnded(ioRef);
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (DataContext as OperatorFieldModel).DocumentID;
            Ellipse el = sender as Ellipse;
            Key outputKey = el.DataContext as Key;
            IOReference ioRef = new IOReference(new ReferenceFieldModel(docId, outputKey), true, e.Pointer, el);
            OnIoDragEnded(ioRef);
        }
    }
}