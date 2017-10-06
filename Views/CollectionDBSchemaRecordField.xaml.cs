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
        public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register(
            "BorderThickness", typeof(Thickness), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(Thickness)));
        public static readonly DependencyProperty DataReferenceProperty = DependencyProperty.Register(
            "DataReference", typeof(ReferenceFieldModelController), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(ReferenceFieldModelController)));

        private DocumentController _document;
        public KeyController      _fieldKey;

        /// <summary>
        /// View model for a single field (think a cell) in a schema view
        /// </summary>
        /// <param name="w">The width of this field view</param>
        /// <param name="document">The document that this field view's field is coming from</param>
        /// <param name="fieldKey">The key referencing the field in the document</param>
        /// <param name="thickness">controls padding this is essential for proper alignment</param>
        public CollectionDBSchemaRecordFieldViewModel(double w, DocumentController document, KeyController fieldKey, Thickness thickness)
        {
            Width     = w;
            _document = document;
            _fieldKey = fieldKey;
             DataReference = new ReferenceFieldModelController(_document.GetId(), _fieldKey);
            BorderThickness = thickness;
           // Content = new ReferenceFieldModelController(_document.GetId(), fieldKey).DereferenceToRoot(null).ToString();
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
            get { return (double) GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        //public string Content
        //{
        //    get { return new ReferenceFieldModelController(_document.GetId(), _fieldKey).DereferenceToRoot(null).ToString(); }
        //    set { SetValue(ContentProperty, value); }
        //}
    }

}
