using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Constructs a nested stackpanel that displays the fields of all documents in the list
    /// docs.
    /// </summary>
    public class StackLayout : CourtesyDocument
    {
        public static DocumentType StackPanelDocumentType =
            new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Layout");
        public static KeyControllerBase StyleKey = new KeyControllerBase("943A801F-A4F4-44AE-8390-31630055D62F", "Style");

        static public DocumentType DocumentType
        {
            get { return StackPanelDocumentType; }
        }

        public bool Horizontal;

        public StackLayout(IEnumerable<DocumentController> docs, bool horizontal=false)
        {
            Horizontal = horizontal;
            var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, new DocumentCollectionFieldModelController(docs));
            fields.Add(StyleKey, new TextFieldModelController(horizontal ? "Horizontal" : "Vertical"));
            Document = new DocumentController(fields, StackPanelDocumentType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
        {
            throw new NotImplementedException("We don't have access to the data document here");
        }

        /// <summary>
        /// Genereates the grid view to contain the stacked elements.
        /// </summary>
        /// <param name="docController"></param>
        /// <param name="context"></param>
        /// <param name="isInterfaceBuilderLayout"></param>
        /// <param name="dataDocument"></param>
        /// <returns></returns>
        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout)
        {
            var stack = new RelativePanel();
            var stackFieldData =
                docController.GetDereferencedField(KeyStore.DataKey, context)
                    as DocumentCollectionFieldModelController;

            var styleField = docController.GetDereferencedField(StyleKey, context) as TextFieldModelController;
            var horizontal = styleField != null && styleField.Data == "Horizontal";
            // create a dynamic gridview that wraps content in borders
            if (stackFieldData != null)
            {
                FrameworkElement prev = null;
                foreach (var stackDoc in stackFieldData.GetDocuments())
                {
                    var item = stackDoc.MakeViewUI(context, isInterfaceBuilderLayout);
                    if (item != null)
                    {
                        stack.Children.Add(item);
                        if (horizontal)
                        {
                            RelativePanel.SetAlignTopWithPanel(item, true);
                            RelativePanel.SetAlignBottomWithPanel(item, true);
                        } else
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
                        prev = item;
                    }
                }
                if (prev != null)
                    if (horizontal)
                        RelativePanel.SetAlignRightWithPanel(prev, true);
                    else RelativePanel.SetAlignBottomWithPanel(prev, true);
            }
            if (horizontal)
            {
                stack.Height = docController.GetHeightField(context).Data;
            } else
            {
                stack.Width = docController.GetWidthField(context).Data;
            }
            SetupBindings(stack, docController, context);
            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(stack, docController, dataDocument);
            }
            return stack;
        }
    }
}