using Windows.UI.Xaml;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    ///     A container which represents a single row in the list created by the <see cref="KeyValuePane" />
    /// </summary>
    public class KeyFieldContainer
    {
        public KeyController Key { get; }

        /// <summary>
        /// used when primary checkbox is here - deprecate for now
        /// </summary>
        public bool IsPrimary { get; }


        public BoundController Controller { get; set; }

        // Type of field, ex) Text, Image, Number  
        public string Type { get; }

        public GridLength TypeColumnWidth { get; set; }
        public GridLength PrimaryKeyColumnWidth { get; set; }

        // whether the container is for a key or a field
        public bool IsField { get; set; }

        /// <summary>
        /// gonna deprecate this - this is key and field together in container, now i'm trying to separate them
        /// </summary>
        /// <param name="key"></param>
        /// <param name="controller"></param>
        /// <param name="typeColumnWidth"></param>
        public KeyFieldContainer(KeyController key, BoundController controller,
            GridLength typeColumnWidth)
        {
            Key = key;
            Controller = controller;
            Type = controller.FieldModelController.TypeInfo.ToString();
            TypeColumnWidth = typeColumnWidth;
            PrimaryKeyColumnWidth = typeColumnWidth == new GridLength(0) ? typeColumnWidth : new GridLength(20);
        }

        /// <summary>
        /// container representing a key cell or a value cell in the list created in KeyValuePane
        /// </summary>
        /// <param name="key"></param>
        /// <param name="controller"></param>
        /// <param name="isPrimary"></param>
        /// <param name="typeColumnWidth"></param>
        /// <param name="isValue"></param>
        public KeyFieldContainer(KeyController key, BoundController controller, GridLength typeColumnWidth, bool isField)
        {
            Key = key;
            Controller = controller;
            Type = controller.FieldModelController.TypeInfo.ToString();
            TypeColumnWidth = typeColumnWidth;
            
        }
    }
}