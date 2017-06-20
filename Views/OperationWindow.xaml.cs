using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.ViewModels;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// Window that allows users to create their own Key,Value pairs 
    /// </summary>
    public sealed partial class OperationWindow : WindowTemplate
    {
        private DocumentModel InputDocument => (DataContext as OperationWindowViewModel).InputDocument;
        private DocumentModel OutputDocument => (DataContext as OperationWindowViewModel).OutputDocument;


        /// <summary>
        /// Line to create and display connection lines between OperationView fields and Document fields 
        /// </summary>
        private Line _connectionLine;

        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private OperatorView.IOReference _currReference;

        private List<Ellipse> _leftEllipses = new List<Ellipse>();

        private List<Ellipse> _rightEllipses = new List<Ellipse>();

        private Dictionary<ReferenceFieldModel, Line> _lineDict = new Dictionary<ReferenceFieldModel, Line>();

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();
        private Dictionary<string, DocumentView> _documentViews = new Dictionary<string, DocumentView>();

        private void OperationWindow_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            OperationWindowViewModel vm = args.NewValue as OperationWindowViewModel;
            Debug.Assert(vm != null);
            LeftListView.ItemsSource = vm.InputDocumentCollection;
            RightListView.ItemsSource = vm.OutputDocumentCollection;

            //Create Operator document
            var docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            OperatorDocumentModel opModel = new OperatorDocumentModel(new DivideOperatorModel())
            {
                Id = docEndpoint.GetDocumentId(),
                OperatorField = new DivideOperatorModel()
            };
            docEndpoint.UpdateDocumentAsync(opModel);
            DocumentView view = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
            opvm.IODragStarted += Vm_IODragStarted;
            opvm.IODragEnded += Vm_IODragEnded;
            view.DataContext = opvm;
            XFreeformView.Canvas.Children.Add(view);
            _documentViews.Add(opModel.Id, view);

            NumberFieldModel nfm = new NumberFieldModel(0);
            OutputDocument.SetField(DocumentModel.GetFieldKeyByName("Price/Sqft"), nfm);
        }

        /// <summary>
        /// Create OperationWindow with a width and height
        /// </summary>
        /// <param name="width">Width of the window</param>
        /// <param name="height">Height of the window</param>
        public OperationWindow(int width, int height)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;
        }

        /// <summary>
        /// EventHandler for when a drag for connecting input/output for operators or fields has started
        /// </summary>
        /// <param name="ioReference">IOReference for the field and the event info</param>
        private void Vm_IODragStarted(OperatorView.IOReference ioReference)
        {
            StartDrag(ioReference, false);
        }

        private void Vm_IODragEnded(OperatorView.IOReference ioReference)
        {
            EndDrag(ioReference, false);
        }

        private void StartDrag(OperatorView.IOReference ioReference, bool fromDoc)
        {
            if (_currentPointers.Contains(ioReference.Pointer.PointerId))
            {
                return;
            }
            _currentPointers.Add(ioReference.Pointer.PointerId);

            _currReference = ioReference;

            _connectionLine = new Line
            {
                StrokeThickness = 10,
                Stroke = new SolidColorBrush(Colors.Black),
                IsHitTestVisible = false,
                Clip = new RectangleGeometry { Rect = new Rect(LeftListView.ActualWidth, 0, XFreeformView.ActualWidth, XFreeformView.ActualHeight) },
                CompositeMode = ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed (https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };
            if (!fromDoc)
            {
                DocumentView view = _documentViews[ioReference.ReferenceFieldModel.DocId];
                MultiBinding<double> x1MultiBinding = new MultiBinding<double>(new FrameworkElementToPosition(true),
                    new KeyValuePair<FrameworkElement, FrameworkElement>(ioReference.Ellipse, XCanvas));
                x1MultiBinding.AddBinding(view, RenderTransformProperty);
                x1MultiBinding.AddBinding(XFreeformView.Canvas, RenderTransformProperty);
                MultiBinding<double> y1MultiBinding = new MultiBinding<double>(new FrameworkElementToPosition(false),
                    new KeyValuePair<FrameworkElement, FrameworkElement>(ioReference.Ellipse, XCanvas));
                y1MultiBinding.AddBinding(view, RenderTransformProperty);
                y1MultiBinding.AddBinding(XFreeformView.Canvas, RenderTransformProperty);
                Binding x1Binding = new Binding
                {
                    Source = x1MultiBinding,
                    Path = new PropertyPath("Property")
                };
                Binding y1Binding = new Binding
                {
                    Source = y1MultiBinding,
                    Path = new PropertyPath("Property")
                };

                _connectionLine.SetBinding(Line.X1Property, x1Binding);
                _connectionLine.SetBinding(Line.Y1Property, y1Binding);
            }
            else
            {
                Binding x1Binding = new Binding()
                {
                    Converter = new FrameworkElementToPosition(true),
                    ConverterParameter =
                        new KeyValuePair<FrameworkElement, FrameworkElement>(ioReference.Ellipse, XCanvas),
                    Source = XCanvas,
                    Path = new PropertyPath("RenderTransform")
                };
                Binding y1Binding = new Binding()
                {
                    Converter = new FrameworkElementToPosition(false),
                    ConverterParameter =
                        new KeyValuePair<FrameworkElement, FrameworkElement>(ioReference.Ellipse, XCanvas),
                    Source = XCanvas,
                    Path = new PropertyPath("RenderTransform")
                };
                _connectionLine.SetBinding(Line.X1Property, x1Binding);
                _connectionLine.SetBinding(Line.Y1Property, y1Binding);
            }

            XCanvas.Children.Add(_connectionLine);

            if(!ioReference.IsOutput)
            {
                CheckLinePresence(ioReference.ReferenceFieldModel);
                _lineDict.Add(ioReference.ReferenceFieldModel, _connectionLine);
            }
        }

        private void CancelDrag(Pointer p)
        {
            _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

        private void EndDrag(OperatorView.IOReference ioReference, bool onDoc)
        {
            _currentPointers.Remove(ioReference.Pointer.PointerId);
            if (_connectionLine == null) return;

            if (_currReference.IsOutput == ioReference.IsOutput)
            {
                UndoLine();
                return;
            }

            if (!ioReference.IsOutput)
            {
                CheckLinePresence(ioReference.ReferenceFieldModel);
                _lineDict.Add(ioReference.ReferenceFieldModel, _connectionLine);
            }

            if (onDoc)
            {
                if (ioReference.IsOutput)
                {
                    var docCont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                    var opDoc = docCont.GetDocumentAsync(_currReference.ReferenceFieldModel.DocId) as OperatorDocumentModel;
                    Debug.Assert(opDoc != null);
                    opDoc.AddInputReference(_currReference.ReferenceFieldModel.FieldKey,
                        ioReference.ReferenceFieldModel);
                    _connectionLine = null;
                }
                else
                {
                    var docCont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                    docCont.GetFieldInDocument(ioReference.ReferenceFieldModel).InputReference = _currReference.ReferenceFieldModel;
                    _connectionLine = null;
                }
            }
            else
            {
                DocumentView view = _documentViews[ioReference.ReferenceFieldModel.DocId];
                MultiBinding<double> x1MultiBinding = new MultiBinding<double>(new FrameworkElementToPosition(true),
                    new KeyValuePair<FrameworkElement, FrameworkElement>(ioReference.Ellipse, XCanvas));
                x1MultiBinding.AddBinding(view, RenderTransformProperty);
                x1MultiBinding.AddBinding(XFreeformView.Canvas, RenderTransformProperty);
                MultiBinding<double> y1MultiBinding = new MultiBinding<double>(new FrameworkElementToPosition(false),
                    new KeyValuePair<FrameworkElement, FrameworkElement>(ioReference.Ellipse, XCanvas));
                y1MultiBinding.AddBinding(view, RenderTransformProperty);
                y1MultiBinding.AddBinding(XFreeformView.Canvas, RenderTransformProperty);
                Binding x1Binding = new Binding
                {
                    Source = x1MultiBinding,
                    Path = new PropertyPath("Property")
                };
                Binding y1Binding = new Binding
                {
                    Source = y1MultiBinding,
                    Path = new PropertyPath("Property")
                };

                _connectionLine.SetBinding(Line.X2Property, x1Binding);
                _connectionLine.SetBinding(Line.Y2Property, y1Binding);
                if (ioReference.IsOutput)
                {
                    var docCont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                    docCont.GetFieldInDocument(_currReference.ReferenceFieldModel).InputReference = ioReference.ReferenceFieldModel;
                    _connectionLine = null;
                }
                else
                {
                    var docCont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                    var opDoc = docCont.GetDocumentAsync(ioReference.ReferenceFieldModel.DocId) as OperatorDocumentModel;
                    Debug.Assert(opDoc != null);
                    opDoc.AddInputReference(ioReference.ReferenceFieldModel.FieldKey,
                        _currReference.ReferenceFieldModel);
                    _connectionLine = null;
                }
            }
        }

        private void XCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(XCanvas).Position;
                _connectionLine.X2 = pos.X;
                _connectionLine.Y2 = pos.Y;
            }
        }

        private void WindowTemplate_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            CancelDrag(e.Pointer);
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(ReferenceFieldModel model)
        {
            /* 
            Line line = null;
            foreach (Line l in _lines)
            {
                if (l.X1 < x + 5 && l.X1 > x - 5 && l.Y1 < y + 5 && l.Y1 > y - 5)
                    line = l;
            }
            */
            if (_lineDict.ContainsKey(model))
            {
                Line line = _lineDict[model];
                XCanvas.Children.Remove(line);
                _lineDict.Remove(model);
            }
        }

        /// <summary>
        /// Creates new DocumentModel with a view from the updated document on the right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void B_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentView view = new DocumentView();
            DocumentViewModel viewModel = new DocumentViewModel(OutputDocument);
            view.DataContext = viewModel;
            FreeformView.MainFreeformView.Canvas.Children.Add(view);
        }

        /// <summary>
        /// Needed to make sure that the bounds on the windows size (min and max) don't exceed the size of the free form canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FreeformView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FreeformView freeform = sender as FreeformView;
            Debug.Assert(freeform != null);
            MaxHeight = HeaderHeight + freeform.CanvasHeight - 5;
            //MaxWidth = XDocumentGridLeft.ActualWidth + freeform.CanvasWidth + XDocumentGridRight.ActualWidth;
            //MinWidth = XDocumentGridLeft.ActualWidth + XDocumentGridRight.ActualWidth + 50;
            MinHeight = HeaderHeight * 2;
        }

        private void InputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            var dictEntry = (DictionaryEntry)(sender as Ellipse).DataContext;
            EndDrag(new OperatorView.IOReference(
                new ReferenceFieldModel(InputDocument.Id, dictEntry.Key as Key), true, e.Pointer,
                sender as Ellipse), true);
        }

        private void OutputEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            var dictEntry = (DictionaryEntry)(sender as Ellipse).DataContext;
            EndDrag(new OperatorView.IOReference(
                new ReferenceFieldModel(OutputDocument.Id, dictEntry.Key as Key), false, e.Pointer,
                sender as Ellipse), true);
        }

        private void UndoLine()
        {
            XCanvas.Children.Remove(_connectionLine);
            //_lineDict. //TODO lol figure this out later 
            _connectionLine = null;
            _currReference = null;
        }

        private void OutputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                e.Handled = true;
                var dictEntry = (DictionaryEntry) (sender as Ellipse).DataContext;
                StartDrag(new OperatorView.IOReference(
                    new ReferenceFieldModel(OutputDocument.Id, dictEntry.Key as Key), false, e.Pointer,
                    sender as Ellipse), true);
            }
        }

        private void InputEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                e.Handled = true;
                var dictEntry = (DictionaryEntry) (sender as Ellipse).DataContext;
                StartDrag(new OperatorView.IOReference(
                    new ReferenceFieldModel(InputDocument.Id, dictEntry.Key as Key), true, e.Pointer,
                    sender as Ellipse), true);
            }
        }

        private void UIElement_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }
    }
}
