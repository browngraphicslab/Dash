using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    ///     A container which represents a single row in the list created by the <see cref="KeyValuePane" />
    /// </summary>
    public class KeyFieldContainer
    {
        public KeyController Key { get; }
        public bool IsPrimary { get; }

        public BoundController Controller { get; set; }

        // Type of field, ex) Text, Image, Number  
        public string Type { get; }

        public GridLength TypeColumnWidth { get; set; }
        public GridLength PrimaryKeyColumnWidth { get; set; }

        public KeyFieldContainer(KeyController key, BoundController controller, bool isPrimary,
            GridLength typeColumnWidth)
        {
            Key = key;
            Controller = controller;
            Type = controller.FieldModelController.TypeInfo.ToString();
            IsPrimary = isPrimary;
            TypeColumnWidth = typeColumnWidth;
            PrimaryKeyColumnWidth = typeColumnWidth == new GridLength(0) ? typeColumnWidth : new GridLength(20);
        }
    }
}