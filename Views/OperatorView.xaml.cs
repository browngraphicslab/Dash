﻿using System;
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

        public event IODragEventHandler IODragStarted;

        public class IOReference
        {
            public ReferenceFieldModel ReferenceFieldModel { get; set; }
            public bool IsOutput { get; set; }

            public Point CursorPosition { get; set; }

            public IOReference(ReferenceFieldModel referenceFieldModel, bool isOutput, Point p)
            {
                ReferenceFieldModel = referenceFieldModel;
                IsOutput = isOutput;
                CursorPosition = p; 
            }
        }

        public OperatorView()
        {
            this.InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            InputListView.ItemsSource = (args.NewValue as OperatorFieldModel).Inputs;
            OutputListView.ItemsSource = (args.NewValue as OperatorFieldModel).Outputs;

            //TODO functionality of drag/drop, but lose the line functionality 
            //InputListView.CanDragItems = true;
            //OutputListView.CanDragItems = true;
        }

        private void InputListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var key = e.Items.Cast<Key>().FirstOrDefault();
            e.Data.SetText(JsonConvert.SerializeObject(new IOReference(new ReferenceFieldModel((DataContext as OperatorFieldModel).DocumentID, key), false, new Point())));
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        private void OutputListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var key = e.Items.Cast<Key>().FirstOrDefault();
            e.Data.SetText(JsonConvert.SerializeObject(new IOReference(new ReferenceFieldModel((DataContext as OperatorFieldModel).DocumentID, key), true, new Point())));
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        /*
        //TODO delete this
        private void InputEllipse_OnDragStarting(UIElement sender, DragStartingEventArgs e)
        {
            Ellipse el = sender as Ellipse;
            Key key = el.DataContext as Key;
            e.Data.SetText(JsonConvert.SerializeObject(new IOReference(new ReferenceFieldModel((DataContext as OperatorFieldModel).DocumentID, key), false, new Point())));
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        private void OutputEllipse_OnDragStarting(UIElement sender, DragStartingEventArgs e)
        {
            Ellipse el = sender as Ellipse;
            Key key = el.DataContext as Key;
            e.Data.SetText(JsonConvert.SerializeObject(new IOReference(new ReferenceFieldModel((DataContext as OperatorFieldModel).DocumentID, key), true, new Point())));
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        */ 
        /// <summary>
        ///  Can return the position of the click in screen space 
        /// </summary>
        private void InputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                string docId = (DataContext as OperatorFieldModel).DocumentID;
                Ellipse el = sender as Ellipse;
                Key outputKey = el.DataContext as Key;
                IOReference ioRef = new IOReference(new ReferenceFieldModel(docId, outputKey), false,
                    el.TransformToVisual(Window.Current.Content)
                        .TransformPoint(new Point(el.Width / 2, el.Height / 2)));
                OnIODragStarted(ioRef);
            }
            Debug.WriteLine("Pointer exited");
        }

        private void OutputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                string docId = (DataContext as OperatorFieldModel).DocumentID;
                Ellipse el = sender as Ellipse;
                Key outputKey = el.DataContext as Key;
                IOReference ioRef = new IOReference(new ReferenceFieldModel(docId, outputKey), true,
                    el.TransformToVisual(Window.Current.Content)
                        .TransformPoint(new Point(el.Width / 2, el.Height / 2)));
                OnIODragStarted(ioRef);
            }
            Debug.WriteLine("Pointer exited");
        }

        private void Ellipse_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void OnIODragStarted(IOReference ioreference)
        {
            IODragStarted?.Invoke(ioreference);
        }

        
    }
}
