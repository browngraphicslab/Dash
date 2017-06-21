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

        private OperatorView.IOReference _currReference;

        private readonly List<Line> _lines = new List<Line>();

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
                    if (!fields.ContainsKey(documentModelField.Key))
                        fields.Add(documentModelField.Key, documentModelField.Value.Copy());
                }
                DocumentEndpoint docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                _output = docEndpoint.CreateDocumentAsync(DocumentViewModel.DocumentModel.DocumentType.Type);//TODO Should this be the same as source document?
                _output.SetFields(fields);

                //DivideOperatorModel divide = new DivideOperatorModel();
                var opModel = new OperatorDocumentModel(new DivideOperatorModel())
                {
                    Id = docEndpoint.GetDocumentId(),
                    OperatorField = new DivideOperatorModel()
                };
                docEndpoint.UpdateDocumentAsync(opModel);
                var vm = new OperatorDocumentViewModel(opModel);
                var view = new DocumentView(vm)
                {
                    Width = 200,
                    Height = 200
                };
                vm.IODragStarted += Vm_IODragStarted;
                
                XFreeformView.Canvas.Children.Add(view);

                //opModel.AddInputReference(DivideOperatorModel.AKey, new ReferenceFieldModel(_documentViewModel.DocumentModel.Id, PricePerSquareFootApi.PriceKey));
                NumberFieldModel nfm = new NumberFieldModel(0);
                //nfm.InputReference =
                    //new ReferenceFieldModel(opModel.Id, DivideOperatorModel.QuotientKey);
                _output.SetField(DocumentModel.GetFieldKeyByName("Price/Sqft"), nfm, false);
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
            //Debug.WriteLine($"Operation Window Drag started: IsOutput: {ioReference.IsOutput}, DocId: {ioReference.ReferenceFieldModel.DocId},\n FieldName: {ioReference.ReferenceFieldModel.FieldKey.Name}, Key: {ioReference.ReferenceFieldModel.FieldKey.Id}, CursorPosition: {ioReference.CursorPosition}");
            if (_currentPointers.Contains(ioReference.Pointer.PointerId))
            {
                return;
            }
            _currentPointers.Add(ioReference.Pointer.PointerId);

            Point pos = new Point(); //  Util.PointTransformFromVisual(ioReference.Pointer.Position, XFreeformView);
            _currReference = ioReference;
            _connectionLine = new Line
            {
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.Black),
                X1 = pos.X,
                Y1 = pos.Y,
                X2 = pos.X,
                Y2 = pos.Y
            };
            //Point pos = Util.PointTransformFromVisual(ioReference.CursorPosition, XCanvas);

            /*
            Binding x1 = new Binding {Path = new PropertyPath("RenderTransform"), Source = ioReference.Box };
            Binding y1 = new Binding {Path = new PropertyPath("RenderTransform"), Source = ioReference.Box };
            //x1.Converter = new ConvertRenderTransform();
            //y1.Converter = new ConvertRenderTransform();
            _connectionLine.SetBinding(Line.X1Property, x1);
            _connectionLine.SetBinding(Line.Y1Property, y1);
            // TODO attempt at binding, binding to position calculated doesn't work obviously 
            // TODO ALSO ioReference.Ellipse has margin of 0 so???????????????????????????????????
            //*/

            XFreeformView.Canvas.Children.Add(_connectionLine);
            //XCanvas.Children.Add(_connectionLine);

            CheckLinePresence(pos.X, pos.Y);
            _lines.Add(_connectionLine);
        }

        private void XFreeformView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(XFreeformView).Position;
                //Point pos = e.GetCurrentPoint(XCanvas).Position;
                _connectionLine.X2 = pos.X;
                _connectionLine.Y2 = pos.Y;
            }
        }

        private void WindowTemplate_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _currentPointers.Remove(e.Pointer.PointerId);
            XFreeformView.Canvas.Children.Remove(_connectionLine);
            //XCanvas.Children.Remove(_connectionLine);
            _lines.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }

        private void CheckLinePresence(double x, double y)
        {
            Line line = null;
            foreach (Line l in _lines)
            {
                if (l.X1 < x + 5 && l.X1 > x - 5 && l.Y1 < y + 5 && l.Y1 > y - 5)
                    line = l;
            }
            _lines.Remove(line);
            XFreeformView.Canvas.Children.Remove(line);
            //XCanvas.Children.Remove(line); 
        }

        private void B_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentViewModel viewModel = new DocumentViewModel(_output);
            DocumentView view = new DocumentView(viewModel);
            FreeformView.MainFreeformView.Canvas.Children.Add(view);
        }

        /// <summary>
        ///  Makes the left grid representing Key,Value pairs of document tapped 
        /// </summary>
        public void InitializeGrid(Grid grid, DocumentModel doc, LayoutModel layout, bool isOutput)
        {
            grid.Children.Clear();

            var fields = doc.EnumFields();
            //Create rows
            foreach (var field in fields)
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
            foreach (KeyValuePair<Key, FieldModel> pair in fields)
            {
                //Add Value as FrameworkElement (field values)  
                TemplateModel template = null;
                if (layout.Fields.ContainsKey(pair.Key))
                    template = layout.Fields[pair.Key];
                if (template != null) {
                    // TODO commented out for debugging 
                    //else
                    //    Debug.Assert(false);
                    var elements = template.MakeViewUI(pair.Value);
                    if (elements != null)
                    foreach (FrameworkElement element in template.MakeViewUI(pair.Value))
                        if (element != null)
                        {
                            element.VerticalAlignment = VerticalAlignment.Center;
                            element.HorizontalAlignment = HorizontalAlignment.Center;

                            element.Margin = new Thickness(12, 5, 12, 5);
                            Grid.SetColumn(element, 1);
                            Grid.SetRow(element, j);
                            grid.Children.Add(element);
                        }
                }

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

                Ellipse el = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.Black),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                if (isOutput) _leftEllipses.Add(el);
                else _rightEllipses.Add(el);

                el.PointerReleased += (sender, args) =>
                {
                    _currentPointers.Remove(args.Pointer.PointerId);
                    if (_connectionLine == null) return;

                    if (_currReference.IsOutput == isOutput)
                    {
                        return;
                    }
                    if (_currReference.IsOutput)
                    {
                        pair.Value.InputReference = _currReference.ReferenceFieldModel;
                    }
                    else
                    {
                        var docCont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                        var opDoc = docCont.GetDocumentAsync(_currReference.ReferenceFieldModel.DocId) as OperatorDocumentModel;
                        opDoc.AddInputReference(_currReference.ReferenceFieldModel.FieldKey,
                            new ReferenceFieldModel(_documentViewModel.DocumentModel.Id, pair.Key));
                    }
                    _connectionLine = null;
                };

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
            MaxHeight = HeaderHeight + freeform.CanvasHeight - 5;
            MaxWidth = XDocumentGridLeft.ActualWidth + freeform.CanvasWidth + XDocumentGridRight.ActualWidth;
            MinWidth = XDocumentGridLeft.ActualWidth + XDocumentGridRight.ActualWidth + 50;
            MinHeight = HeaderHeight * 2;
        }

        private void WindowTemplate_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Ellipses on the left grid 
            double height = XDocumentGridLeft.RowDefinitions[0].ActualHeight;
            for (int i = 0; i < XDocumentGridLeft.RowDefinitions.Count - 1; i++)
            {
                RowDefinition r = XDocumentGridLeft.RowDefinitions[i + 1];
                _leftEllipses[i].Margin = new Thickness(XDocumentGridLeft.ActualWidth - 5, height + r.ActualHeight / 2 - 5, 0, 0);

                height += r.ActualHeight;
            }

            // Ellipses on the right grid  
            height = XDocumentGridRight.RowDefinitions[0].ActualHeight;
            for (int i = 0; i < XDocumentGridRight.RowDefinitions.Count - 3; i++)
            {
                RowDefinition r = XDocumentGridRight.RowDefinitions[i + 1];
                _rightEllipses[i].Margin = new Thickness(XDocumentGridLeft.ActualWidth + XFreeformView.ActualWidth - 5, height + r.ActualHeight / 2 - 5, 0, 0);

                height += r.ActualHeight;
            }
        }
    }
}
