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
    }
    public class CollectionDBSchemaRecordFieldViewModel: DependencyObject
    {
        //public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        //    "Content", typeof(string), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
            "Width", typeof(double), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty DataReferenceProperty = DependencyProperty.Register(
            "DataReference", typeof(ReferenceFieldModelController), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(ReferenceFieldModelController)));

        public DocumentController _document;
        public KeyController      _fieldKey;
        public CollectionDBSchemaRecordFieldViewModel(double w, DocumentController document, KeyController fieldKey)
        {
            Width     = w;
            _document = document;
            _fieldKey = fieldKey;
             DataReference = new ReferenceFieldModelController(_document.GetId(), fieldKey);
            // Content = new ReferenceFieldModelController(_document.GetId(), fieldKey).GetValue(null).ToString();
        }
        public ReferenceFieldModelController DataReference
        {
            get { return (ReferenceFieldModelController) GetValue(DataReferenceProperty); }
            set { SetValue(DataReferenceProperty, value); }
        }
        public double Width
        {
            get { return 70; } //  (double) GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
        //public string Content
        //{
        //    get { return GetValue(ContentProperty)?.ToString(); }
        //    set { SetValue(ContentProperty, value); }
        //}
    }

}
