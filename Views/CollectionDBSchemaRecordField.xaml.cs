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

namespace Dash
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
    public class CollectionDBSchemaRecordFieldViewModel: ViewModelBase
    {
        //public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        //    "Content", typeof(string), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(string)));
         
        double    _width;
        bool      _isSelected;
        Thickness _borderThickness;
        ReferenceFieldModelController _dataReference;
        
        public bool Selected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public Thickness BorderThickness
        {
            get => _borderThickness;
            set => SetProperty(ref _borderThickness, value);
        }
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }
        public ReferenceFieldModelController DataReference
        {
            get => _dataReference;
            set => SetProperty(ref _dataReference, value);
        }

        public DocumentController Document;
        public int                Row;
        public CollectionDBSchemaHeader.HeaderViewModel HeaderViewModel;
        public CollectionDBSchemaRecordFieldViewModel(DocumentController document, CollectionDBSchemaHeader.HeaderViewModel headerViewModel, Border headerBorder, int row)
        {
            Document        = document;
            HeaderViewModel = headerViewModel;
            Row             = row;
            DataReference   = new ReferenceFieldModelController(Document.GetId(), headerViewModel.FieldKey);
            BorderThickness = headerBorder.BorderThickness; // not expected to change at run-time, so not registering for callbacks
            Width           = BorderThickness.Left + BorderThickness.Right + (double)HeaderViewModel.Width;
            HeaderViewModel.PropertyChanged += (sender, e) => Width = BorderThickness.Left + BorderThickness.Right + (double)HeaderViewModel.Width;
            Document.AddFieldUpdatedListener(HeaderViewModel.FieldKey, Document_DocumentFieldUpdated);
            //Content = new ReferenceFieldModelController(document.GetId(), headerViewModel.FieldKey).DereferenceToRoot(null).ToString();
        }

        private void Document_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            DataReference = new ReferenceFieldModelController(Document.GetId(), HeaderViewModel.FieldKey);
        }
        
        //public string Content
        //{
        //    get { return (string)GetValue(ContentProperty); }
        //    set { SetValue(ContentProperty, value); }
        //}
    }

}
