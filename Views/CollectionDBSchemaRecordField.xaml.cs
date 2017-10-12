using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class CollectionDBSchemaRecordField : UserControl
    {
        public CollectionDBSchemaRecordField()
        {
            this.InitializeComponent();
        }
        
        public delegate void FieldTapped(CollectionDBSchemaRecordField field);
        public static event FieldTapped FieldTappedEvent;

        private void UserControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (FieldTappedEvent != null)
                FieldTappedEvent(this);
        }
    }
    public class CollectionDBSchemaRecordFieldViewModel: DependencyObject
    {
        //public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        //    "Content", typeof(string), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            "Selected", typeof(bool), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
            "Width", typeof(double), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register(
            "BorderThickness", typeof(Thickness), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(Thickness)));
        public static readonly DependencyProperty DataReferenceProperty = DependencyProperty.Register(
            "DataReference", typeof(ReferenceFieldModelController), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(ReferenceFieldModelController)));


        public DocumentController Document;
        public int                Row;
        public CollectionDBSchemaHeader.HeaderViewModel HeaderViewModel;
        public CollectionDBSchemaRecordFieldViewModel(DocumentController document, CollectionDBSchemaHeader.HeaderViewModel headerViewModel, Border headerBorder, int row)
        {
            HeaderViewModel = headerViewModel;
            Row = row;
            BorderThickness = headerBorder.BorderThickness;
            Width = BorderThickness.Left + BorderThickness.Right + (double)HeaderViewModel.Width;
            Document = document;
            Document.AddFieldUpdatedListener(HeaderViewModel.FieldKey, Document_DocumentFieldUpdated);
            DataReference = new ReferenceFieldModelController(Document.GetId(), headerViewModel.FieldKey);
            HeaderViewModel.RegisterPropertyChangedCallback(CollectionDBSchemaHeader.HeaderViewModel.WidthProperty, WidthChangedCallback);
            //Content = new ReferenceFieldModelController(document.GetId(), headerViewModel.FieldKey).DereferenceToRoot(null).ToString();
        }

        private void Document_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            DataReference = new ReferenceFieldModelController(Document.GetId(), HeaderViewModel.FieldKey);
        }
        private void WidthChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            Width = BorderThickness.Left + BorderThickness.Right + (double)HeaderViewModel.Width;
        }

        public ReferenceFieldModelController DataReference
        {
            get { return (ReferenceFieldModelController) GetValue(DataReferenceProperty); }
            set { SetValue(DataReferenceProperty, value); }
        }
        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }
        public double Width
        {
            get { return (double)GetValue(WidthProperty);  }
            set { SetValue(WidthProperty, value); }
        }

        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        //public string Content
        //{
        //    get { return (string)GetValue(ContentProperty); }
        //    set { SetValue(ContentProperty, value); }
        //}
    }

}
