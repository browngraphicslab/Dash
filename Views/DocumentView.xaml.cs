using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentView : UserControl
    {
        Dictionary<string, TextBlock> textElementViews = new Dictionary<string, TextBlock>();

        public DocumentView()
        {
            this.InitializeComponent();
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            this.DataContextChanged += DocumentView_DataContextChanged;
        }
        

        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ReLayout();
        }

        private void elementModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReLayout();
        }

        public void ReLayout()
        {
            var dvm = DataContext as DocumentViewModel;
            if (dvm != null)
            {
                var lm = dvm.DocumentViewModelSource.DocumentLayoutModel(dvm.DocumentModel);
                foreach (var item in dvm.DocumentModel.Fields)
                {
                    var elementKey   = item.Key;
                    var elementModel = lm.Fields[elementKey];
                    var content      = item.Value;

                    if (!textElementViews.ContainsKey(elementKey))
                    {
                        xCanvas.Children.Add(new TextBlock());
                        textElementViews.Add(elementKey, xCanvas.Children.Last() as TextBlock);
                    }
                    var tb = textElementViews[elementKey];
                    tb.FontSize = 16;
                    tb.Width = 200;
                    tb.TextWrapping = elementModel.TextWrapping;
                    tb.FontWeight = elementModel.FontWeight;
                    tb.Text = content == null ? "" : content.ToString();
                    tb.Name = "x" + elementKey;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    Canvas.SetLeft(tb, elementModel.Left);
                    Canvas.SetTop(tb,  elementModel.Top);
                    tb.Visibility = elementModel.Visibility;
                    elementModel.PropertyChanged -= elementModel_PropertyChanged;
                    elementModel.PropertyChanged += elementModel_PropertyChanged;
                }
            }
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
         //   var viewEditor = new ViewEditor();
         //   FreeFormViewModel.Instance.AddToView(viewEditor, Constants.ViewEditorInitialLeft, Constants.ViewEditorInitialTop);
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
    }
}
