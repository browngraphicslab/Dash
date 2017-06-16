using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    //[ContentProperty("InnerContent")]
    public sealed partial class DocumentView : UserControl
    {
        Dictionary<string, TextBlock> textElementViews = new Dictionary<string, TextBlock>();

        private float _documentScale = 1.0f;
        public const float MinScale = 0.5f;
        public const float MaxScale = 2.0f;

        public DocumentView()
        {
            this.InitializeComponent();
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            this.DataContextChanged += DocumentView_DataContextChanged;

            this.Width = 200;
            this.Height = 400;
        }

        //public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(object), typeof(DocumentView), new PropertyMetadata(null));

        //public object InnerContent
        //{
        //    get { return (object) GetValue(InnerContentProperty); }
        //    set { SetValue(InnerContentProperty, value);}
        //}

        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var dvm = DataContext as DocumentViewModel;
            dvm.DocumentModel.DocumentFieldUpdated += DocumentModel_DocumentFieldUpdated;
            if (dvm != null)
            {
                xCanvas.Children.Clear();
                List<UIElement> elements = dvm.GetUiElements();
                foreach (var element in elements)
                {
                    xCanvas.Children.Add(element);
                }
            }
        }

        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            DocumentView_DataContextChanged(null, null);
        }

        private void elementModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReLayout();
        }

        public void ReLayout()
        {
            //var dvm = DataContext as DocumentViewModel;
            //if (dvm != null)
            //{
            //    var lm = dvm.DocumentViewModelSource.DocumentLayoutModel(dvm.DocumentModel);
            //    foreach (var item in dvm.DocumentModel.Fields)
            //    {
            //        var elementKey   = item.Key;
            //        var elementModel = lm.Fields[elementKey];
            //        var content      = item.Value;

            //        if (!textElementViews.ContainsKey(elementKey))
            //        {
            //            xCanvas.Children.Add(new TextBlock());
            //            textElementViews.Add(elementKey, xCanvas.Children.Last() as TextBlock);
            //        }
            //        var tb = textElementViews[elementKey];
            //        tb.FontSize = 16;
            //        tb.Width = 200;
            //        tb.TextWrapping = elementModel.TextWrapping;
            //        tb.FontWeight = elementModel.FontWeight;
            //        tb.Text = content == null ? "" : content.ToString();
            //        tb.Name = "x" + elementKey;
            //        tb.HorizontalAlignment = HorizontalAlignment.Center;
            //        tb.VerticalAlignment = VerticalAlignment.Center;
            //        Canvas.SetLeft(tb, elementModel.Left);
            //        Canvas.SetTop(tb,  elementModel.Top);
            //        tb.Visibility = elementModel.Visibility;
            //        elementModel.PropertyChanged -= elementModel_PropertyChanged;
            //        elementModel.PropertyChanged += elementModel_PropertyChanged;
            //    }
            //}
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            //   var viewEditor = new ViewEditor();
            //   FreeFormViewModel.MainFreeformView.AddToView(viewEditor, Constants.ViewEditorInitialLeft, Constants.ViewEditorInitialTop);
            //   viewEditor.SetCurrentlyDisplayedDocument(DataContext as DocumentViewModel);
        }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            //if ((Window.Current.Content as Frame).Content is FreeFormView)
            //{
            //    var gt = TransformToVisual(Window.Current.Content);
            //    // Use that to convert the generated Point into the page's coords
            //    Point pagePoint = gt.TransformPoint(e.Position);

            //    foreach (var cgv in ((((Window.Current.Content as Frame).Content as FreeFormView).Content as Canvas).Children.Where((c) => c is CollectionGridView).Select((x) => (CollectionGridView)x)))
            //        if (VisualTreeHelper.FindElementsInHostCoordinates(pagePoint, cgv).Count() > 0)
            //        {
            //            var cgvModel = (cgv as CollectionGridView).DataContext as CollectionGridViewModel;
            //            if (!cgvModel.Documents.Contains(DataContext as DocumentViewModel))
            //            {
            //                cgvModel.AddDocument((DataContext as DocumentViewModel).DocumentModel);
            //            }
            //        }
            //}
            base.OnManipulationCompleted(e);
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            //Create initial composite transform 
            TransformGroup group = new TransformGroup();

            ScaleTransform scale = new ScaleTransform
            {
                CenterX = e.Position.X,
                CenterY = e.Position.Y,
                ScaleX = e.Delta.Scale,
                ScaleY = e.Delta.Scale
            };

            //Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, this);
            //TranslateTransform translate = new TranslateTransform
            //{
            //    X = p.X,
            //    Y = p.Y
            //};
            TranslateTransform translate = Util.TranslateInCanvasSpace(e.Delta.Translation, this);


            //Clamp the scale factor 
            float newScale = _documentScale * e.Delta.Scale;
            if (newScale > MaxScale)
            {
                scale.ScaleX = MaxScale / _documentScale;
                scale.ScaleY = MaxScale / _documentScale;
                _documentScale = MaxScale;
            }
            else if (newScale < MinScale)
            {
                scale.ScaleX = MinScale / _documentScale;
                scale.ScaleY = MinScale / _documentScale;
                _documentScale = MinScale;
            }
            else
            {
                _documentScale = newScale;
            }

            group.Children.Add(scale);
            group.Children.Add(this.RenderTransform);
            group.Children.Add(translate);

            //Get top left and bottom right points of documents in canvas space
            Point p1 = group.TransformPoint(new Point(0, 0));
            Point p2 = group.TransformPoint(new Point(XGrid.ActualWidth, XGrid.ActualHeight));
            Debug.Assert(this.RenderTransform != null);
            Point oldP1 = this.RenderTransform.TransformPoint(new Point(0, 0));
            Point oldP2 = this.RenderTransform.TransformPoint(new Point(XGrid.ActualWidth, XGrid.ActualHeight));

            //Check if translating or scaling the document puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            if (p1.X < 0)
            {
                outOfBounds = true;
                translate.X = -oldP1.X;
                scale.CenterX = 0;
            }
            
            else if (p2.X > FreeformView.MainFreeformView.Canvas.ActualWidth)
            {
                outOfBounds = true;
                translate.X = FreeformView.MainFreeformView.Canvas.ActualWidth - oldP2.X;
                scale.CenterX = XGrid.ActualWidth;
            }
            if (p1.Y < 0)
            {
                outOfBounds = true;
                translate.Y = -oldP1.Y;
                scale.CenterY = 0;
            }
            else if (p2.Y > FreeformView.MainFreeformView.Canvas.ActualHeight)
            {
                outOfBounds = true;
                translate.Y = FreeformView.MainFreeformView.Canvas.ActualHeight - oldP2.Y;
                scale.CenterY = XGrid.ActualHeight;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(this.RenderTransform);
                group.Children.Add(translate);
            }

            this.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

        /// <summary>
        /// Brings up OperationWindow when DocumentView is double tapped 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            OperationWindow window = new OperationWindow(1000, 800);

            var dvm = DataContext as DocumentViewModel;
            if (dvm != null)
            {
                window.DocumentViewModel = dvm;
                //LayoutModel model = dvm.DocumentViewModelSource.DocumentLayoutModel(dvm.DocumentModel);
                //LayoutModel model = new LayoutModel(dvm.DocumentModel.DocumentType);
            }
            Point center = RenderTransform.TransformPoint(e.GetPosition(this));

            FreeformView.MainFreeformView.ViewModel.AddElement(window, (float)(center.X - window.Width / 2), (float)(center.Y - window.Height / 2));

            ////Get top left and bottom right points of documents in canvas space
            //Point p1 = this.RenderTransform.TransformPoint(new Point(0, 0));
            //Point p2 = this.RenderTransform.TransformPoint(new Point(XGrid.ActualWidth, XGrid.ActualHeight));
            //Debug.Assert(this.RenderTransform != null);
            //Point oldP1 = this.RenderTransform.TransformPoint(new Point(0, 0));
            //Point oldP2 = this.RenderTransform.TransformPoint(new Point(XGrid.ActualWidth, XGrid.ActualHeight));
        }
    }
}
