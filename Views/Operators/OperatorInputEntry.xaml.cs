﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class OperatorInputEntry : UserControl
    {
        public static readonly DependencyProperty OperatorFieldReferenceProperty = DependencyProperty.Register(
            "OperatorFieldReference", typeof(FieldReference), typeof(OperatorInputEntry), new PropertyMetadata(default(FieldReference)));

        public FieldReference OperatorFieldReference
        {
            get { return (FieldReference)GetValue(OperatorFieldReferenceProperty); }
            set { xFieldLabel.Foreground = new SolidColorBrush(Colors.Red); SetValue(OperatorFieldReferenceProperty, value); }
        }

        private DocumentController _refDoc;

        public OperatorInputEntry()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputLinkHandle_OnDrop(object sender, DragEventArgs e)
        {
            
            if (e.DataView.Properties.ContainsKey("Operator Document"))
            {
                xHandle.Opacity = .75;
                xFieldLabel.Foreground = new SolidColorBrush(Colors.Red);
                // we pass a view document, so we get the data document
                var refDoc = (e.DataView.Properties["Operator Document"] as DocumentController)?.GetDataDocument();
                var opDoc = OperatorFieldReference.GetDocumentController(null);
                var el = sender as FrameworkElement;
                var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
                _refDoc = refDoc;
                if (e.DataView.Properties.ContainsKey("Operator Key"))
                {
                    var refKey = (KeyController)e.DataView.Properties["Operator Key"];
                    opDoc.SetField(key, new DocumentReferenceController(refDoc.Id, refKey), true);
                    xFieldLabel.Text = refKey.Name;
                }
                else
                {
                    SuggestBox.Visibility = Visibility.Visible;
                    SuggestBox.Focus(FocusState.Programmatic);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// When the pointer is hovering over the input handle. TODO: should also have this
        /// when you're just hovering over the label as well, makes it easier on user / for touch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputLinkHandle_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey("Operator Document"))
            {
                var refDoc = (DocumentController)e.DataView.Properties["Operator Document"];
                if (e.DataView.Properties.ContainsKey("Operator Key")) //There is a specified key, so check if it's the right type
                {
                    var refKey = (KeyController)e.DataView.Properties["Operator Key"];
                    var opField = OperatorFieldReference.DereferenceToRoot<OperatorController>(null);
                    var el = sender as FrameworkElement;
                    var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
                    var fieldType = refDoc.GetDereferencedField(refKey, null).TypeInfo;
                    var inputType = opField.Inputs[key].Type;
                    e.AcceptedOperation = inputType.HasFlag(fieldType) ? DataPackageOperation.Link : DataPackageOperation.None;
                }
                else //There's just a document, and a key needs to be chosen later, so accept for now
                {
                    e.AcceptedOperation = DataPackageOperation.Link;
                }
            }
            e.Handled = true;
        }

        private void AutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text == "")
                {
                    sender.ItemsSource = _refDoc.EnumDisplayableFields().Select(kv => kv.Key);
                }
                else
                {
                    sender.ItemsSource = _refDoc.EnumDisplayableFields().Select(kv => kv.Key)
                        .Where(k => k.Name.ToLower().Contains(sender.Text.ToLower()));
                }
            }
        }

        private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_refDoc == null)
            {
                return;
            }
            var key = ((DictionaryEntry?)DataContext)?.Key as KeyController;
            if (args.ChosenSuggestion != null)
            {
                OperatorFieldReference.GetDocumentController(null).SetField(key,
                    new DocumentReferenceController(_refDoc.Id, (KeyController)args.ChosenSuggestion), true);
            }
            else
            {
                KeyController refKey = _refDoc.EnumDisplayableFields()
                    .Select(kv => kv.Key).FirstOrDefault(k => k.Name == args.QueryText);
                if (refKey != null)
                {
                    OperatorFieldReference.GetDocumentController(null).SetField(key,
                        new DocumentReferenceController(_refDoc.Id, refKey), true);
                }
            }
        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_refDoc != null)
            {
                SuggestBox.Visibility = Visibility.Visible;
                SuggestBox.Focus(FocusState.Programmatic);
            }
        }

        private void SuggestBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SuggestBox.Visibility = Visibility.Collapsed;
        }
    }
}
