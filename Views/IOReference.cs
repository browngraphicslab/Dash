using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// Descriptor or bucket to hold a reference to either input or output
    /// normally used in a drag event for creating links.
    /// </summary>
    public class IOReference
    {
        /// <summary>
        /// The actual reference to a field (either input or output) on some document.
        /// </summary>
        public FieldReference FieldReference { get; }

        /// <summary>
        /// This determines the flow of data in the IOReference
        /// </summary>
        public bool IsOutput { get; }

        /// <summary>
        /// Determines the type of the data that the <see cref="FieldReference"/> is referencing
        /// </summary>
        public TypeInfo Type { get; }

        public PointerRoutedEventArgs PointerArgs { get; }

        /// <summary>
        /// Stores the Framework element which was dragged off of, or the element which
        /// was dragged on to. This lets us figure out where we want to make links to and from
        /// </summary>
        public FrameworkElement FrameworkElement { get; }

        /// <summary>
        /// The view which contains the link, this is used to set up bindings so that the end point of the link is updated
        /// when the width, height, or transform of the <see cref="ContainerView"/> changes
        /// </summary>
        public DocumentView ContainerView { get; }

        public IOReference(FieldReference fieldReference, bool isOutput, TypeInfo type, PointerRoutedEventArgs args, FrameworkElement e, DocumentView container)
        {
            FieldReference = fieldReference;
            IsOutput = isOutput;
            Type = type;
            PointerArgs = args;
            FrameworkElement = e;
            ContainerView = container;
        }
    }
}