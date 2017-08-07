using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class IOReference
    {
        public delegate void IODragEventHandler(IOReference ioReference);

        /// <summary>
        /// Event that gets fired when an ellipse is dragged off of and a connection should be started
        /// </summary>
        public event IODragEventHandler IoDragStarted;

        /// <summary>
        /// Event that gets fired when an ellipse is dragged on to and a connection should be ended
        /// </summary>
        public event IODragEventHandler IoDragEnded;

        public FieldReference FieldReference { get; set; }
        public bool IsOutput { get; set; }
        //public bool IsReference { get; set; }

        public PointerRoutedEventArgs PointerArgs { get; set; }

        public FrameworkElement FrameworkElement { get; set; }
        public DocumentView ContainerView { get; set; }

        public FieldModelController FMController { get; set; }

        public KeyController FieldKey { get; set; }
        public IOReference(KeyController fieldKey, FieldModelController controller, FieldReference fieldReference, bool isOutput, PointerRoutedEventArgs args, FrameworkElement e, DocumentView container)
        {
            FieldKey = fieldKey;
            FMController = controller;
            FieldReference = fieldReference;
            IsOutput = isOutput;
            PointerArgs = args;
            FrameworkElement = e;
            ContainerView = container;
        }

        public void FireDragStarted()
        {
            IoDragStarted?.Invoke(this);
        }

        public void FireDragEnded()
        {
            IoDragEnded?.Invoke(this);
        }
    }
}