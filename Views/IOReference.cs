using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class IOReference
    {
        public FieldReference FieldReference { get; }
        public bool IsOutput { get; }

        public TypeInfo Type { get; }
        //public bool IsReference { get; set; }

        public PointerRoutedEventArgs PointerArgs { get; }

        public FrameworkElement FrameworkElement { get; }
        public DocumentView ContainerView { get; }

        public FieldControllerBase FMController { get; }

        public KeyControllerBase FieldKey { get; }
        public IOReference(KeyControllerBase fieldKey, FieldControllerBase controller, FieldReference fieldReference, bool isOutput, TypeInfo type, PointerRoutedEventArgs args, FrameworkElement e, DocumentView container)
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