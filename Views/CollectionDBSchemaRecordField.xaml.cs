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
            this.DataContextChanged += CollectionDBSchemaRecordField_DataContextChanged;
        }

        private void CollectionDBSchemaRecordField_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
        }
    }
    public class CollectionDBSchemaRecordFieldViewModel: DependencyObject, INotifyPropertyChanged
    {
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
            "Width", typeof(double), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty ControllerProperty = DependencyProperty.Register(
            "Controller", typeof(BoundFieldModelController), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(BoundFieldModelController)));

        public CollectionDBSchemaRecordFieldViewModel(double w, DocumentController document, KeyController fieldKey)
        {
            Width = w;
            Controller = new BoundFieldModelController(document.GetField(fieldKey), document);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BoundFieldModelController Controller
        {
            get { return (BoundFieldModelController)GetValue(ControllerProperty); }
            set { SetValue(ControllerProperty, value); }
        }
        public double Width
        {
            get { return (double) GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
    }

}
