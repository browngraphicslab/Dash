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
using Dash.StaticClasses;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionMapView : UserControl
    {
        private ObservableCollection<Key> _operatorKeys = new ObservableCollection<Key>();

        public CollectionMapView()
        {
            this.InitializeComponent();

            DataContextChanged += CollectionMapView_DataContextChanged;

            XOperatorType.ItemsSource = new List<string>
            {
                "Divide",
                "Filter"
            };

            XOperatorType.SelectionChanged += XOperatorType_SelectionChanged;

            XKeyList.ItemsSource = _operatorKeys;
        }

        private void XOperatorType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OperatorFieldModelController opFmc;
            if (XOperatorType.SelectedIndex == 0)
            {
                opFmc = new DivideOperatorFieldModelController();
            }
            else
            {
                opFmc = new FilterOperatorFieldModelController();
            }
            _operatorDoc.SetField(CollectionMapOperator.InputOperatorKey,
                opFmc, true);

            _operatorKeys.Clear();
            foreach (var opFmcInput in opFmc.Inputs)
            {
                _operatorKeys.Add(opFmcInput.Key);
            }
        }

        private DocumentController _operatorDoc;
        private CollectionMapOperator _operator;

        private void CollectionMapView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var refToOp = args.NewValue as FieldReference;
            var doc = refToOp.GetDocumentController(null);
            _operatorDoc = doc;
            _operator = doc.GetField(OperatorDocumentModel.OperatorKey) as CollectionMapOperator;

            doc.AddFieldUpdatedListener(CollectionMapOperator.InputOperatorKey, InputOperatorChanged);
        }

        private void InputOperatorChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            _operator.UpdateInputs(args.NewValue as OperatorFieldModelController);
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var key = sender.DataContext as Key;
            var coll = _operatorDoc.GetField(key).DereferenceToRoot<DocumentCollectionFieldModelController>(null)?.GetDocuments();
            if (coll == null)
            {
                return;
            }
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 0)
                    sender.ItemsSource = FilterUtils.GetKeySuggestions(coll, sender.Text.ToLower());
                else
                    sender.ItemsSource = new[] { "No suggestions..." };
            }

            var keys = FilterUtils.GetKeys(coll).ToList();
            var inkey = keys.FirstOrDefault(k => k.Name.ToLower().Equals(sender.Text.ToLower()));
            if (inkey != null)
            {
                _operator.InputKeyMap[key] = inkey;
                _operatorDoc.Execute(null, true);
            }
        }
    }
}
