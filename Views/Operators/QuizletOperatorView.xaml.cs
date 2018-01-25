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
using DashShared.Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class QuizletOperatorView : UserControl
    {
        private DocumentController _operatorDoc;

        public QuizletOperatorView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);
        }

        private void OnSendTapped(object sender, TappedRoutedEventArgs e)
        {

            var collection = _operatorDoc.GetField<ListController<DocumentController>>(QuizletOperator.CollectionKey);
            var termKey = GetKeyFromOp(QuizletOperator.TermKey);
            var definitionKey = GetKeyFromOp(QuizletOperator.DefinitionKey);
            var imageKey = GetKeyFromOp(QuizletOperator.ImageKey);

            if (collection == null) return;

            foreach (var doc in collection.TypedData)
            {
                // TODO check to see if 
                var term = doc.GetField<TextController>(termKey);
                var definition = doc.GetField<TextController>(definitionKey);
                var image = doc.GetField<ImageController>(imageKey);
            }
        }

        private KeyController GetKeyFromOp(KeyController keyController)
        {
            var outputKeyId = _operatorDoc.GetField<TextController>(keyController)?.Data;
            KeyController outputKey = null;
            if (outputKeyId != null)
            {
                outputKey = ContentController<FieldModel>.GetController<KeyController>(outputKeyId);
            }
            return outputKey;
        }
    }
}
