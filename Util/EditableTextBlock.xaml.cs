using Dash.Converters;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class EditableTextBlock
    {
        #region BINDING PROPERTIES 

        public string Text
        {
            get { return (string)xTextBlock.Text; }
            set { xTextBlock.SetValue(TextBlock.TextProperty, value); }
        }

        #endregion
        public ReferenceFieldModelController TargetFieldReference = null;
        public Context                       TargetDocContext = null;

        public EditableTextBlock()
        {
            InitializeComponent();

            //events 
            xTextBox.PointerWheelChanged += (s, e) => e.Handled = true;
            xTextBox.ManipulationDelta += (s, e) => e.Handled = true;
            xTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                    xTextBox_LostFocus(s, null);
            };

                //var colorBinding = new Binding
                //{
                //    Source = this,
                //    Path = new PropertyPath(nameof(Foreground)),
                //    Mode = BindingMode.TwoWay
                //};
                //Block.SetBinding(TextBlock.ForegroundProperty, colorBinding);
                //Box.SetBinding(TextBox.ForegroundProperty, colorBinding);
        }

        private void xTextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;

            // if this displays the contents of a document's field, then when we want to display
            // the reference expression/formula (not the final derefenceed value)
            if (TargetFieldReference != null)
            {
                var contents = TargetFieldReference.Dereference(TargetDocContext);
                var newText = this.xTextBlock.Text;
                if (contents is ReferenceFieldModelController)
                {
                    var textRef = contents as ReferenceFieldModelController;
                    var docName = new DocumentControllerToStringConverter().ConvertDataToXaml(textRef.GetDocumentController(null));
                    newText = "=" + docName.TrimStart('<').TrimEnd('>') + "." + textRef.FieldKey.Name;
                }
                if (this.xTextBox.Text != newText)
                    this.xTextBox.Text = newText;
            }
            xTextBlock.Visibility = Visibility.Collapsed;
            xTextBox.Visibility = Visibility.Visible;
            xTextBox.Focus(FocusState.Programmatic);
            xTextBox.SelectAll();
        }

        private void xTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            xTextBox.Visibility = Visibility.Collapsed;
            xTextBlock.Visibility = Visibility.Visible;

            // if this displays the contents of a documents' field, then any changes to this field must be parsed 
            // back into that document's field.
            if (TargetFieldReference != null)
            {
                var targetDoc = TargetFieldReference.GetDocumentController(TargetDocContext);
                targetDoc.ParseDocField(TargetFieldReference.FieldKey, xTextBox.Text, 
                    targetDoc.GetDereferencedField<FieldModelController>(TargetFieldReference.FieldKey, TargetDocContext));
            }
            else
                xTextBlock.Text = xTextBox.Text;
        }
    }
}
