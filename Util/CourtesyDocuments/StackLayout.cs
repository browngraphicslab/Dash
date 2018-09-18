using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.Foundation;

namespace Dash
{
    /// <summary>
    /// Constructs a nested stackpanel that displays the fields of all documents in the list
    /// docs.
    /// </summary>
    public class StackLayout : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Layout");
        public static KeyController StyleKey = new KeyController("Style", "943A801F-A4F4-44AE-8390-31630055D62F");
        private static readonly string PrototypeId = "1CEB0635-0B57-452A-93F9-F43C66EEF911";

        public StackLayout(IEnumerable<DocumentController> docs, bool horizontal = false, Point where = new Point(), Size size = new Size())
        {
            var fields = DefaultLayoutFields(where, size != new Size() ? size : new Size(double.NaN, double.NaN), new ListController<DocumentController>(docs));
            fields.Add(StyleKey, new TextController(horizontal ? "Horizontal" : "Vertical"));
            SetupDocument(DocumentType, PrototypeId, "StackLayout Prototype Layout", fields);
        }

        /// <summary>
        /// Genereates the grid view to contain the stacked elements.
        /// </summary>
        /// <param name="docController"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var stack = new RelativePanel();
            var stackFieldData =
                docController.GetDereferencedField(KeyStore.DataKey, context)
                    as ListController<DocumentController>;

            SetupBindings(stack, docController, context);
            var styleField = docController.GetDereferencedField(StyleKey, context) as TextController;
            var horizontal = styleField != null && styleField.Data == "Horizontal";
            // create a dynamic gridview that wraps content in borders
            if (stackFieldData != null)
            {
                FrameworkElement prev = null;
                foreach (var stackDoc in stackFieldData.GetElements())
                {
                    var item = stackDoc.MakeViewUI(context);
                    if (item != null)
                    {
                        stack.Children.Add(item);
                        if (horizontal)
                        {
                            RelativePanel.SetAlignTopWithPanel(item, true);
                            RelativePanel.SetAlignBottomWithPanel(item, true);
                        }
                        else
                        {
                            RelativePanel.SetAlignLeftWithPanel(item, true);
                            RelativePanel.SetAlignRightWithPanel(item, true);
                        }
                        if (prev != null)
                        {
                            if (horizontal)
                                RelativePanel.SetRightOf(item, prev);
                            else RelativePanel.SetBelow(item, prev);
                        }
                        else
                        {
                            if (horizontal)
                                RelativePanel.SetAlignLeftWithPanel(item, true);
                            else RelativePanel.SetAlignTopWithPanel(item, true);
                        }
                        prev = item;
                        if (horizontal)
                            BindHeight(item, docController, null);
                        else
                            BindWidth(item, docController, null);
                    }
                }
                if (prev != null)
                    if (horizontal)
                        RelativePanel.SetAlignRightWithPanel(prev, true);
                    else RelativePanel.SetAlignBottomWithPanel(prev, true);
            }
            // stack.SizeChanged += (object sender, SizeChangedEventArgs e) =>
            // {
                //foreach (var child in stack.Children)
                //    if (child is FrameworkElement fe)
                //    {
                //        if (fe.DataContext is DocumentViewModel dview)
                //        {
                //            dview.LayoutDocument.SetActualSize(new Point(fe.ActualWidth, fe.ActualHeight));
                //        }
                //        else if (fe.DataContext is CollectionViewModel cview)
                //        {
                //            cview.ContainerDocument.SetActualSize(new Point(fe.ActualWidth, fe.ActualHeight));
                //        }
                //    }
            // };

            return stack;
        }
        
    }
}
