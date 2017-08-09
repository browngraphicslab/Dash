using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class IOReference
    {
        public FieldReference FieldReference { get; set; }
        public bool IsOutput { get; set; }

        public TypeInfo Type;
        //public bool IsReference { get; set; }

        public PointerRoutedEventArgs PointerArgs { get; set; }

        public FrameworkElement FrameworkElement { get; set; }
        public DocumentView ContainerView { get; set; }

        public FieldModelController FMController { get; set; }

        public KeyController FieldKey { get; set; }
        public IOReference(KeyController fieldKey, FieldModelController controller, FieldReference fieldReference, bool isOutput, TypeInfo type, PointerRoutedEventArgs args, FrameworkElement e, DocumentView container)
        {
            FieldKey = fieldKey;
            FMController = controller;
            FieldReference = fieldReference;
            IsOutput = isOutput;
            Type = type;
            PointerArgs = args;
            FrameworkElement = e;
            ContainerView = container;
        }
    }
}