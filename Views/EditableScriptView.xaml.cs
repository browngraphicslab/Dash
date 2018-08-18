using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Dash.Annotations;
using Dash.Models.DragModels;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class EditableScriptView : INotifyPropertyChanged
    {


        private bool _textBoxLoaded;

        private bool TextBoxLoaded
        {
            get => _textBoxLoaded;
            set
            {
                _textBoxLoaded = value;
                OnPropertyChanged();
            }
        }

        public EditableScriptViewModel ViewModel
        {
            get => DataContext as EditableScriptViewModel;
            set => DataContext = value;
        }


        public EditableScriptView()
        {
            this.InitializeComponent();
        }

        public void MakeEditable()
        {
            SetExpression(XTextBlock.Text);
            TextBoxLoaded = true;
        }

        private void XTextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            TextBoxLoaded = true;
        }

        private void XTextBox_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XTextBox_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //e.Handled = true;
        }

        private void XTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                SetExpression(XTextBox.Text);
        }

        private void XTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private bool SetExpression(string text)
        {
            TextBoxLoaded = false;
            try
            {
                var field = DSL.InterpretUserInput(text, scope: Scope.CreateStateWithThisDocument(ViewModel.Reference.GetDocumentController(ViewModel.Context)));
                ViewModel?.Reference.SetField(field, ViewModel.Context);
            }
            catch (DSLException)
            {
                return false;
            }
            return true;
        }

        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            //if (ViewModel != null && e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            //{
            //    var dropDocument = (e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel).DraggedDocument;
            //    ViewModel?.Reference.GetDocumentController(null).SetField(ViewModel?.Reference.FieldKey, dropDocument.GetViewCopy(), true);
            //    e.Handled = true;
            //}

            //TODO: IS THE CODE BELOW FUNCTIONAL / REPRESENTATIVE OF CODE ABOVE ??

            if (ViewModel == null || !e.DataView.HasDataOfType(Internal)) return;

            var docsToStore = (await e.DataView.GetDroppableDocumentsForDataOfType(Internal, sender as FrameworkElement)).Select(d => d.GetViewCopy()).ToList();
            ViewModel?.Reference.GetDocumentController(null).SetField<ListController<DocumentController>>(ViewModel?.Reference.FieldKey, docsToStore, true);

            e.Handled = true;
        }

        private string GetRootExpression()
        {
            return ViewModel?.Reference.DereferenceToRoot(ViewModel.Context)?.GetValue(ViewModel.Context)?.ToString();
        }

        private string GetExpression()
        {
            return ViewModel?.Reference.Dereference(ViewModel.Context)?.GetValue(ViewModel.Context)?.ToString();
        }

        private void XTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxLoaded)
            {
               // SetExpression(XTextBox.Text);
            }
        }

        private void XTextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            XTextBlock.Visibility = Visibility.Collapsed;
            XTextBox.Focus(FocusState.Programmatic);
            XTextBox.Text = GetExpression() ?? XTextBlock.Text;
            XTextBox.SelectAll();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private EditableScriptViewModel _oldViewModel;
        private void EditableScriptView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == null || ViewModel == _oldViewModel)
            {
                return;
            }

            _oldViewModel = ViewModel;
            var binding = new FieldBinding<FieldControllerBase>
            {
                Document = ViewModel.Reference.GetDocumentController(ViewModel.Context),
                Key = ViewModel.Reference.FieldKey,
                Context = ViewModel.Context,
                Converter = new ObjectToStringConverter(),
                Mode = BindingMode.TwoWay,
            };
            XTextBlock.AddFieldBinding(TextBlock.TextProperty, binding);
        }
    }
}
