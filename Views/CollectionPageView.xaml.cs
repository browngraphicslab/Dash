using Dash.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class CollectionPageView : SelectionElement, ICollectionView
    {
        public BaseCollectionViewModel ViewModel { get; private set; }
        //private ScrollViewer _scrollViewer;

        public CollectionPageView()
        {
            this.InitializeComponent();
            DataContextChanged += CollectionPageView_DataContextChanged;
        }
        public ObservableCollection<DocumentViewModel> PageDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();

        private void CollectionPageView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            foreach (var vm in ViewModel.DocumentViewModels.Reverse())
            {
                var pageDocDelegate = vm.DocumentController.MakeDelegate();
                var pageDocLayoutDelegate = pageDocDelegate.GetActiveLayout(new Context(pageDocDelegate))?.Data?.MakeDelegate();
                if (pageDocLayoutDelegate != null)
                {
                    pageDocLayoutDelegate.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(double.NaN), true);
                    pageDocLayoutDelegate.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(double.NaN), true);
                    pageDocLayoutDelegate.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point()), true);
                    pageDocDelegate.SetField(KeyStore.ActiveLayoutKey, new DocumentFieldModelController(pageDocLayoutDelegate), true);
                }

                CurPage = new DocumentViewModel(pageDocDelegate);
                PageDocumentViewModels.Insert(0,CurPage);

                var thumbnailImageDoc = (pageDocDelegate.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentFieldModelController)?.Data?.MakeDelegate();
                if (thumbnailImageDoc != null)
                {
                    var thumbnailLayoutDoc = thumbnailImageDoc.GetActiveLayout(new Context(thumbnailImageDoc))?.Data?.MakeDelegate();
                    if (thumbnailLayoutDoc != null)
                    {
                        thumbnailLayoutDoc.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(double.NaN), true);
                        thumbnailLayoutDoc.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(double.NaN), true);
                        thumbnailLayoutDoc.SetField(CourtesyDocument.HorizontalAlignmentKey, new TextFieldModelController(HorizontalAlignment.Stretch.ToString()), true);
                        thumbnailLayoutDoc.SetField(CourtesyDocument.VerticalAlignmentKey, new TextFieldModelController(VerticalAlignment.Stretch.ToString()), true);
                        thumbnailLayoutDoc.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point()), true);
                        thumbnailImageDoc.SetField(KeyStore.ActiveLayoutKey, new DocumentFieldModelController(thumbnailLayoutDoc), true);
                    }
                    pageDocDelegate.SetField(KeyStore.ThumbnailFieldKey, new DocumentFieldModelController(thumbnailImageDoc), true);
                }
                else
                    thumbnailImageDoc = vm.DocumentController;
                var dvm = new DocumentViewModel(thumbnailImageDoc);
                var doc = new DocumentView(dvm);

                //doc.HorizontalAlignment = HorizontalAlignment.Stretch;
                //doc.VerticalAlignment = VerticalAlignment.Stretch;
                xThumbs.Children.Insert(0,doc);
            }
        }
        
        public DocumentViewModel CurPage
        {
            get { return this.xDocView.DataContext as DocumentViewModel; }
            set
            {
                value.Width = value.Height = double.NaN;
                this.xDocView.DataContext = value;

                // replace old layout of page name/id with a new one because
                // fieldbinding's can't be removed yet
                navBar.Children.Remove(xPageNum);
                xPageNum = new Button() { Name = "xPageNum" };
                RelativePanel.SetRightOf(xPageNum, this.fitPageButton);
                var binding = new FieldBinding<DocumentFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = value.DocumentController,
                    Key = KeyStore.ThisKey,
                    Converter = new DocumentControllerToStringConverter()
                };
                
                xPageNum.AddFieldBinding(Button.ContentProperty, binding);
                navBar.Children.Add(xPageNum);
            }
        }
        private void XGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            //_scrollViewer = xGridView.GetFirstDescendantOfType<ScrollViewer>();
            //_scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
            //UpdateVisibleIndices(true);
        }

        private int _prevOffset;

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
        }

        #endregion

        #region DragAndDrop


        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }
        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }
        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;
            OnSelected();
        }

        #endregion

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var ind = PageDocumentViewModels.IndexOf(CurPage);
            CurPage = PageDocumentViewModels[Math.Max(0,ind-1)];
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

            var ind = PageDocumentViewModels.IndexOf(CurPage);
            CurPage = PageDocumentViewModels[Math.Min(PageDocumentViewModels.Count - 1, ind + 1)];
        }

        private void FitPageButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
