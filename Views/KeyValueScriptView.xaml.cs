using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash.Annotations;

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
                if (((EditableScriptViewModel) xFieldValue.DataContext).Reference.DereferenceToRoot(null) is ListController<DocumentController> listOfDocs)
                {
                    xFlyoutItem.Text = XTextBox.Text;
                    Flyout.ShowAt(xFieldValue);
                }
                else
                {
                    if (ViewModel?.Reference?.GetDocumentController(ViewModel.Context)?.GetField(ViewModel.Reference.FieldKey) is ReferenceController &&
                        !this.XTextBox.Text.StartsWith("=="))
                        this.XTextBox.Text = "==" + this.XTextBox.Text;
                    xFormulaColumn.Width = new GridLength(1, GridUnitType.Star);
                    xValueColumn.Width = new GridLength(0);
                    XTextBox.Focus(FocusState.Programmatic);
                }
            };
            KeyDown += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    await SetExpression(XTextBox.Text);
                    MainPage.Instance.Focus(FocusState.Programmatic);
                }

                if (e.Key == Windows.System.VirtualKey.Escape)
                    MainPage.Instance.Focus(FocusState.Programmatic);
            };
            KeyUp += (s, e) => e.Handled = true;
            XTextBox.LostFocus += (s, e) =>
            {
                CollapseBox();
            };
        }
        private async Task SetExpression(string text)
        {
            using (UndoManager.GetBatchHandle())
            {
                try
                {
                    var field = await DSL.InterpretUserInput(text,
                        scope: Scope.CreateStateWithThisDocument(
                            ViewModel.Reference.GetDocumentController(ViewModel.Context)));
                    ViewModel?.Reference.SetField(field, ViewModel.Context);
                }
                catch (DSLException ex)
                {
                }
            }
        }

        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            //if (ViewModel != null && e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            //{
            //    e.Handled = true;
            //    var dragModel = (e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel);
            //    if (dragModel.DraggedKey != null && dragModel.DraggedKey.Equals(ViewModel.Key) && dragModel.DraggedDocument.Equals(ViewModel.Reference.GetDocumentController(ViewModel.Context)))
            //    {
            //        // don't allow droping a field on itself
            //        return;
            //    }
            //    var data = dragModel.DraggedKey != null ? dragModel.DraggedDocument.GetDereferencedField(dragModel.DraggedKey, null) : 
            //                             dragModel.DraggedDocument.GetDataDocument().GetDereferencedField(KeyStore.DataKey, null);
            //    if (data != null)
            //    {
            //        var fieldData = ViewModel.Reference.DereferenceToRoot(ViewModel.Context);
            //        if (!fieldData.TypeInfo.Equals(data.TypeInfo))
            //        {
            //            var changeTypeDialog = new ContentDialog
            //            {
            //                Title = "Change field data type?",
            //                Content = "Assigning this data will change the data type of this field.",
            //                PrimaryButtonText = "OK",
            //                SecondaryButtonText = "Cancel"
            //            };
            //            var result = await changeTypeDialog.ShowAsync();
            //            if (result != ContentDialogResult.Primary)
            //                return;
            //        }
            //    }
            //    var dropDocument = dragModel.DraggedDocument;
            //    ViewModel?.Reference.GetDocumentController(null).SetField(ViewModel?.Reference.FieldKey, dropDocument.GetViewCopy(), true);
            //}
        }

        public void ExpandBox()
        {
            xBackground.Height = 120;
            xBackground.VerticalAlignment = VerticalAlignment.Top;
        }

        public void CollapseBox()
        {
            xFormulaColumn.Width = new GridLength(0);
            xValueColumn.Width = new GridLength(1, GridUnitType.Star);
            xBackground.Height = 50;
            xBackground.VerticalAlignment = VerticalAlignment.Center;
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

            void fieldChanged(DocumentController ss, DocumentController.DocumentFieldUpdatedEventArgs ee, Context c)
            {
                if (ee.Action == DocumentController.FieldUpdatedAction.Replace)
                {
                    xFieldValue.DataContext = new DocumentViewModel(_oldDataBox);
                }
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
                Mode = BindingMode.OneWay,
            };
            XTextBox.AddFieldBinding(TextBox.TextProperty, _oldBinding);
            _oldDataBox = new TableBox(new DocumentReferenceController(_oldBinding.Document, _oldBinding.Key)).Document;
            xFieldValue.DataContext = new DocumentViewModel(_oldDataBox);
            _oldBinding.Document.AddFieldUpdatedListener(_oldBinding.Key, fieldChanged);
        }
    }
}
