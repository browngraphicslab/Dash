using System;
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
        private DocumentViewModel _documentViewModel;

        private DocumentModel _output;

        private Line _connectionLine;

        private List<Ellipse> _leftEllipses = new List<Ellipse>();

        private List<Ellipse> _rightEllipses = new List<Ellipse>();

        private HashSet<uint> _currentPointers = new HashSet<uint>();

        public DocumentViewModel DocumentViewModel
        {
            get { return _documentViewModel; }
            set
            {
                _documentViewModel = value;
                var layout = DocumentViewModel.GetLayoutModel();
                InitializeGrid(XDocumentGridLeft, DocumentViewModel.DocumentModel, layout, true);

                Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>();
                foreach (var documentModelField in _documentViewModel.DocumentModel.EnumFields())
                {
                    fields.Add(documentModelField.Key, documentModelField.Value.Copy());
                }
                DocumentEndpoint docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                _output = docEndpoint.CreateDocumentAsync(DocumentViewModel.DocumentModel.DocumentType.Type);//TODO Should this be the same as source document?
                _output.SetFields(fields);

                //DivideOperatorModel divide = new DivideOperatorModel();
                OperatorDocumentModel opModel = new OperatorDocumentModel(new DivideOperatorModel());
                opModel.Id = docEndpoint.GetDocumentId();
                opModel.OperatorField = new DivideOperatorModel();
                docEndpoint.UpdateDocumentAsync(opModel);
                DocumentView view = new DocumentView();
                view.Width = 200;
                view.Height = 200;
                OperatorDocumentViewModel vm = new OperatorDocumentViewModel(opModel);
                vm.IODragStarted += Vm_IODragStarted;
                view.DataContext = vm;
                XFreeformView.Canvas.Children.Add(view);

                //opModel.AddInputReference(DivideOperatorModel.AKey, new ReferenceFieldModel(_documentViewModel.DocumentModel.Id, PricePerSquareFootApi.PriceKey));
                NumberFieldModel nfm = new NumberFieldModel(0);
                //nfm.InputReference =
                    //new ReferenceFieldModel(opModel.Id, DivideOperatorModel.QuotientKey);
                _output.SetField(DocumentModel.GetFieldKeyByName("Price/Sqft"), nfm);
                //opModel.AddInputReference(DivideOperatorModel.BKey, new ReferenceFieldModel(_documentViewModel.DocumentModel.Id, PricePerSquareFootApi.SqftKey));

                InitializeGrid(XDocumentGridRight, _output, layout, false);


                XDocumentGridRight.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                XDocumentGridRight.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Button createButton = new Button
                {
                    Content = "Create"
                };
                Grid.SetRow(createButton, XDocumentGridRight.RowDefinitions.Count - 1);
                XDocumentGridRight.Children.Add(createButton);

                createButton.Tapped += B_Tapped;
            }
        }

        public OperationWindow(int width, int height)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;
        }

        private void Vm_IODragStarted(OperatorView.IOReference ioReference)
        {
            //TODO determine if this is the correct code if not remove it
            //Debug.WriteLine($"Operation Window Drag started: IsOutput: {ioReference.IsOutput}, DocId: {ioReference.ReferenceFieldModel.DocId},\n FieldName: {ioReference.ReferenceFieldModel.FieldKey.Name}, Key: {ioReference.ReferenceFieldModel.FieldKey.Id}, CursorPosition: {ioReference.CursorPosition}");
            //_connectionLine = new Line();
            //Point pos = Util.PointTransformFromVisual(ioReference.CursorPosition, XFreeformView);

            Debug.WriteLine($"Operation Window Drag started: {ioReference.Pointer.PointerId}");
            if (_currentPointers.Contains(ioReference.Pointer.PointerId))
            {
                return;
            }
            _currentPointers.Add(ioReference.Pointer.PointerId);
            _connectionLine = new Line();
            Point pos = Util.PointTransformFromVisual(ioReference.PointerPosition, XFreeformView);

            _connectionLine.X1 = pos.X;
            _connectionLine.Y1 = pos.Y;
            _connectionLine.X2 = 0;
            _connectionLine.Y2 = 0;
            _connectionLine.Stroke = new SolidColorBrush(Colors.Black);
            _connectionLine.StrokeThickness = 5;

            XFreeformView.Canvas.Children.Add(_connectionLine);
        }

        private void B_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentView view = new DocumentView();
            DocumentViewModel viewModel = new DocumentViewModel(_output);
            view.DataContext = viewModel;
            FreeformView.MainFreeformView.Canvas.Children.Add(view);
        }

        /// <summary>
        ///  Makes the left grid representing Key,Value pairs of document tapped 
        /// </summary>
        public void InitializeGrid(Grid grid, DocumentModel doc, LayoutModel layout, bool isOutput)
        {
            grid.Children.Clear();

            //Create rows
            foreach (var f in doc.EnumFields()) 
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            //Create columns 
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            //Make Key, Value headers 
            TextBlock v = new TextBlock
            {
                Text = "Value",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(v, 1);
            Grid.SetRow(v, 0);
            grid.Children.Add(v);

            TextBlock k = new TextBlock
            {
                Text = "Key",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(k, 0);
            Grid.SetRow(k, 0);
            grid.Children.Add(k);

            //Fill in Grid 
            int j = 1;
            foreach (KeyValuePair<Key, FieldModel> pair in doc.EnumFields())
            {
                //Add Value as FrameworkElement (field values)  
                TemplateModel template = null;
                if (layout.Fields.ContainsKey(pair.Key))
                    template = layout.Fields[pair.Key];
                // TODO commented out for debugging 
                //else
                //    Debug.Assert(false);

                FrameworkElement element = pair.Value.MakeView(template) as FrameworkElement;
                if (element != null)
                {
                    element.VerticalAlignment = VerticalAlignment.Center;
                    element.HorizontalAlignment = HorizontalAlignment.Center;
                }
                element.AllowDrop = true;
                element.DragEnter += (sender, args) =>
                {
                    args.AcceptedOperation = DataPackageOperation.Copy;
                };

                element.Drop += async (sender, args) =>
                {
                    if (args.DataView.Contains(StandardDataFormats.Text))
                    {
                        var text = await args.DataView.GetTextAsync();
                        var key = JsonConvert.DeserializeObject<OperatorView.IOReference>(text);
                        if (key.IsOutput == isOutput)
                        {
                            return;
                        }
                        if (key.IsOutput)
                        {
                            pair.Value.InputReference = key.ReferenceFieldModel;
                        }
                        else
                        {
                            var docCont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                            var opDoc = docCont.GetDocumentAsync(key.ReferenceFieldModel.DocId) as OperatorDocumentModel;
                            opDoc.AddInputReference(key.ReferenceFieldModel.FieldKey, new ReferenceFieldModel(_documentViewModel.DocumentModel.Id, pair.Key));
                        }
                    }
                };

                element.Margin = new Thickness(12, 5, 12, 5);
                Grid.SetColumn(element, 1);
                Grid.SetRow(element, j);
                grid.Children.Add(element);

                //Add Key Values (field names) 
                TextBlock tb = new TextBlock
                {
                    Text = pair.Key.Name,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(tb, 0);
                Grid.SetRow(tb, j);
                tb.Padding = new Thickness(12, 5, 12, 5);
                grid.Children.Add(tb);

                j++;
                if (j == 2) continue; 

                Ellipse el = new Ellipse
                {
                    Width = 10, Height = 10,
                    Fill = new SolidColorBrush(Colors.Black),
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
                }; 
                if (isOutput) _rightEllipses.Add(el);
                else _leftEllipses.Add(el);
                XCanvas.Children.Add(el);

            }
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
            this.MaxHeight = HeaderHeight + freeform.CanvasHeight - 5;
            this.MaxWidth = XDocumentGridLeft.ActualWidth + freeform.CanvasWidth + XDocumentGridRight.ActualWidth;
            this.MinWidth = XDocumentGridLeft.ActualWidth + XDocumentGridRight.ActualWidth + 50;
            this.MinHeight = HeaderHeight * 2;
        }

        private void XFreeformView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(XFreeformView).Position;
                _connectionLine.X2 = pos.X;
                _connectionLine.Y2 = pos.Y;
            }
        }

        private void WindowTemplate_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                _connectionLine = null;
            }
        }

        private void WindowTemplate_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Ellipses on the left grid 
            double height = XDocumentGridLeft.RowDefinitions[0].ActualHeight; 
            for (int i = 0; i < XDocumentGridLeft.RowDefinitions.Count - 1; i++)
            {
                RowDefinition r = XDocumentGridLeft.RowDefinitions[i+1];
                _leftEllipses[i].Margin = new Thickness(XDocumentGridLeft.ActualWidth-5, height + r.ActualHeight / 2 - 5, 0, 0); 
                
                height += r.ActualHeight; 
            }

            // Ellipses on the right grid  
            if (_rightEllipses.Count < XDocumentGridRight.RowDefinitions.Count)
            {
                for (int i = 0; i < XDocumentGridRight.RowDefinitions.Count - _rightEllipses.Count - 2; i++)
                {
                    Ellipse el = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = new SolidColorBrush(Colors.Black),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    _rightEllipses.Add(el);
                    XCanvas.Children.Add(el);
                }
            }
            height = XDocumentGridRight.RowDefinitions[0].ActualHeight;
            for (int i = 0; i < XDocumentGridRight.RowDefinitions.Count - 3; i++)
            {
                RowDefinition r = XDocumentGridRight.RowDefinitions[i+1];
                _rightEllipses[i].Margin = new Thickness(XDocumentGridLeft.ActualWidth + XFreeformView.ActualWidth-5, height + r.ActualHeight / 2 -5, 0, 0);

                height += r.ActualHeight;
            }
        }
    }
}
