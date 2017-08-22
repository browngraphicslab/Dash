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
        public TextBox Box
        {
            get { return xTextBox; }
        }

        public TextBlock Block
        {
            get { return xTextBlock; }
        }

        #region BINDING PROPERTIES 

        public string Text
        {
            get { return (string)Block.Text; }
            set { Block.SetValue(TextBlock.TextProperty, value); }
        }

        #endregion
        ReferenceFieldModelController NativeRef = null;
        Context                       NativeContext = null;

        public EditableTextBlock():this(null, null)
        {
        }
        public EditableTextBlock(ReferenceFieldModelController refToText, Context nativeContext) 
        {
            NativeRef     = refToText;
            NativeContext = nativeContext;

            InitializeComponent();

            //events 
            Box.PointerWheelChanged += (s, e) => e.Handled = true;
            Box.ManipulationDelta   += (s, e) => e.Handled = true;
            Box.KeyDown += Box_KeyDown;
            
            //var colorBinding = new Binding
            //{
            //    Source = this,
            //    Path = new PropertyPath(nameof(Foreground)),
            //    Mode = BindingMode.TwoWay
            //};
            //Block.SetBinding(TextBlock.ForegroundProperty, colorBinding);
            //Box.SetBinding(TextBox.ForegroundProperty, colorBinding);

        }

        private void Box_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                xTextBox_LostFocus(sender, null);
            }
        }

        private void xTextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;

            // if this displays the contents of a documents' field, then when we edit the field
            // we want to display the reference expression/formula (not the final value) so that it can be edited
            if (NativeRef != null)
            {
                var contents = NativeRef.Dereference(NativeContext);
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
            Block.Visibility = Visibility.Collapsed;
            Box.Visibility = Visibility.Visible;
            Box.Focus(FocusState.Programmatic);
            Box.SelectAll();
        }

        private void xTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Box.Visibility = Visibility.Collapsed;
            Block.Visibility = Visibility.Visible;

            // if this displays the contents of a documents' field, then any changes to this field must be parsed back into 
            // that document's field.
            //var refField = NativeRef as ReferenceFieldModelController;
            //if (refField != null)
            //{
            //    refField.GetDocumentController(NativeContext).ParseDocField(refField.FieldKey,
            //             Box.Text, NativeRef.GetDocumentController(NativeContext).GetDereferencedField<FieldModelController>(NativeRef.FieldKey, NativeContext));
            //}
            //else
                Block.Text = Box.Text;
        }
    }
}
