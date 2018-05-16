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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValueScriptView : INotifyPropertyChanged
    {

        public EditableScriptViewModel ViewModel
        {
            get => DataContext as EditableScriptViewModel;
            set => DataContext = value;
        }

        public KeyValueScriptView()
        {
            this.InitializeComponent();
            PointerWheelChanged += (s, e) => e.Handled = true;
            DoubleTapped += (s, e) =>
            {
                e.Handled = true;
                XTextBox.IsEnabled = true;
                XTextBox.IsReadOnly = false;
            };
            KeyDown += (s, e) => {
                if (e.Key == Windows.System.VirtualKey.Enter)
                    SetExpression(XTextBox.Text);
            };
            KeyUp += (s, e) => e.Handled = true;
            LostFocus += (s, e) =>
            {
                SetExpression(XTextBox.Text);
                CollapseBox();
            };
        }
        private bool SetExpression(string text)
        {
            try
            {
                XTextBox.IsReadOnly = true;
                XTextBox.IsEnabled = false;
                FieldControllerBase field = DSL.InterpretUserInput(text, state: ScriptState.CreateStateWithThisDocument(ViewModel.Reference.GetDocumentController(ViewModel.Context)));
                ViewModel?.Reference.SetField(field, ViewModel.Context);
            }
            catch (DSLException)
            {
                return false;
            }
            return true;
        }

        async void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel != null && e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                e.Handled = true;
                var dragModel = (e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel);
                if (dragModel.DraggedKey != null && dragModel.DraggedKey.Equals(ViewModel.Key) && dragModel.DraggedDocument.Equals(ViewModel.Reference.GetDocumentController(ViewModel.Context)))
                {
                    return;
                }
                var data = dragModel.DraggedKey != null ? dragModel.DraggedDocument.GetDereferencedField(dragModel.DraggedKey, null) : dragModel.DraggedDocument.GetDataDocument().GetDereferencedField(KeyStore.DataKey, null);
                if (data != null)
                {
                    var fieldData = ViewModel.Reference.DereferenceToRoot(ViewModel.Context);
                    if (!fieldData.TypeInfo.Equals(data.TypeInfo))
                    {
                        var noWifiDialog = new ContentDialog
                        {
                            Title = "Change field data type?",
                            Content = "Assigning this data will change the data type of this field.",
                            PrimaryButtonText = "OK",
                            SecondaryButtonText = "Cancel"
                        };
                        var result = await noWifiDialog.ShowAsync();
                        if (result != ContentDialogResult.Primary)
                            return;
                    }
                }
                var dropDocument = dragModel.DraggedDocument;
                ViewModel?.Reference.GetDocumentController(null).SetField(ViewModel?.Reference.FieldKey, new TextController("==fs(\"" + dropDocument.Title + " Type:Image\")"), true);
            }
        }

        public void ExpandBox()
        {
            xBackground.Height = 120;
            xBackground.VerticalAlignment = VerticalAlignment.Top;
            var kvp = this.GetFirstAncestorOfType<KeyValuePane>();
            kvp?.Expand_Value(this);
        }

        public void CollapseBox()
        {
            xBackground.Height = 60;
            xBackground.VerticalAlignment = VerticalAlignment.Center;
            var kvp = this.GetFirstAncestorOfType<KeyValuePane>();
            kvp?.Collapse_Value(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private EditableScriptViewModel _oldViewModel;
        private DocumentController _oldDataBox = null;
        private FieldBinding<FieldControllerBase> _oldBinding = null;
        private void EditableScriptView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == null || ViewModel == _oldViewModel)
            {
                return;
            }

            void fieldChanged(FieldControllerBase ss, FieldUpdatedEventArgs ee, Context c)
            {
                xFieldValue.DataContext = new DocumentViewModel(_oldDataBox);
            }
            if (_oldBinding != null)
            {
                _oldBinding.Document.RemoveFieldUpdatedListener(_oldBinding.Key, fieldChanged);
            }
            _oldViewModel = ViewModel;
            _oldBinding = new FieldBinding<FieldControllerBase>
            {
                Document = ViewModel.Reference.GetDocumentController(ViewModel.Context),
                Key = ViewModel.Reference.FieldKey,
                XamlAssignmentDereferenceLevel = XamlDereferenceLevel.DontDereference,
                Context = ViewModel.Context,
                Converter = new ObjectToStringConverter() { DereferenceData = false },
                Mode = BindingMode.TwoWay,
            };
            XTextBox.AddFieldBinding(TextBox.TextProperty, _oldBinding);
            _oldDataBox = new DataBox(new DocumentReferenceController(_oldBinding.Document.Id, _oldBinding.Key)).Document;
            xFieldValue.DataContext = new DocumentViewModel(_oldDataBox);
            _oldBinding.Document.AddFieldUpdatedListener(_oldBinding.Key, fieldChanged);
        }
    }
}
