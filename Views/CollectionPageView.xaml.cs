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
            xThumbs.SizeChanged += (sender, e) =>
            {
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Height = xThumbs.ActualHeight;
            };
        }
        public static KeyController DocumentContextKey = new KeyController("17D4CFDE-9146-47E9-8AF0-0F9D546E94EC", "Data Context Key");

        public ObservableCollection<DocumentViewModel> PageDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();


        private void CollectionPageView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.ThumbDocumentViewModels.Clear();
            foreach (var pageDoc in ViewModel.DocumentViewModels.Reverse().Select((vm) => vm.DocumentController))
            {
                var pageDocLayoutDelegate = pageDoc.MakeActiveLayoutDelegate(double.NaN, double.NaN);
                pageDocLayoutDelegate.SetField(DocumentContextKey, new DocumentFieldModelController(pageDoc), true);

                CurPage = new DocumentViewModel(pageDocLayoutDelegate);
                PageDocumentViewModels.Insert(0,CurPage);

                var thumbnailImageDoc = (pageDoc.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentFieldModelController)?.Data?.MakeDelegate();
                var thumbnailImageDocLayout =  thumbnailImageDoc != null ? thumbnailImageDoc.MakeActiveLayoutDelegate(double.NaN,double.NaN) : pageDoc;
                thumbnailImageDoc.SetField(KeyStore.ActiveLayoutKey, new DocumentFieldModelController(thumbnailImageDocLayout), true);
                ViewModel.ThumbDocumentViewModels.Insert(0, new DocumentViewModel(thumbnailImageDoc));
            }
        }

        public DocumentViewModel CurPage
        {
            get { return this.xDocView.DataContext as DocumentViewModel; }
            set
            {
                xDocView.DataContext = value;

                // replace old layout of page name/id with a new one because
                // fieldbinding's can't be removed yet
                navBar.Children.Remove(xPageNum);
                xPageNum = new Button() { Name = "xPageNum" };
                RelativePanel.SetRightOf(xPageNum, this.fitPageButton);

                var binding = new FieldBinding<DocumentFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = value.DocumentController,
                    Key = value.DocumentController.GetField(DocumentContextKey, true) == null ? KeyStore.ThisKey : DocumentContextKey,
                    Converter = new DocumentControllerToStringConverter()
                };
                
                navBar.Children.Add(xPageNum);
                xPageNum.AddFieldBinding(Button.ContentProperty, binding);
            }
        }

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
            ViewModel.UpdateDocumentsOnSelection(isSelected);
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

        private void xThumbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void xThumbs_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
           
        }

        private void xThumbs_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
