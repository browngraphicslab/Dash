using System;
using System.Collections.Generic;
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
    public sealed partial class CollectionMapView : UserControl
    {
        public CollectionMapView()
        {
            this.InitializeComponent();

            DataContextChanged += CollectionMapView_DataContextChanged;

            XOperatorType.ItemsSource = new List<string>
            {
                "Test"
            };

            XOperatorType.SelectionChanged += XOperatorType_SelectionChanged;
        }

        private void XOperatorType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _operatorDoc.SetField(CollectionMapOperator.InputOperatorKey, new DivideOperatorFieldModelController(new OperatorFieldModel("Divide")), true);
        }

        private DocumentController _operatorDoc;

        private void CollectionMapView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var refToOp = args.NewValue as FieldReference;
            var doc = refToOp.GetDocumentController(null);
            _operatorDoc = doc;

            doc.AddFieldUpdatedListener(CollectionMapOperator.InputOperatorKey, InputOperatorChanged);
        }

        private void InputOperatorChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            
        }
    }
}
