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
    public class StackingPanel : CourtesyDocument
    {
        public static DocumentType StackPanelDocumentType =
            new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Panel");
        public static Key StyleKey = new Key("943A801F-A4F4-44AE-8390-31630055D62F", "Style");

        static public DocumentType DocumentType
        {
            get { return StackPanelDocumentType; }
        }

        public bool FreeForm;

        public StackingPanel(IEnumerable<DocumentController> docs, bool freeForm)
        {
            FreeForm = freeForm;
            var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, new DocumentCollectionFieldModelController(docs));
            fields.Add(StyleKey, new TextFieldModelController(freeForm ? "Free Form" : "Stacked"));
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
            if ((docController.GetDereferencedField(StyleKey, context) as TextFieldModelController).TextFieldModel.Data == "Free Form")
                return MakeFreeFormView(docController, context, isInterfaceBuilderLayout, dataDocument);
            var stack = new GridView();
            stack.Loaded += (s, e) =>
            {
                var stackViewer = stack.GetFirstDescendantOfType<ScrollViewer>();
                var stackScrollBar = stackViewer.GetFirstDescendantOfType<ScrollBar>();
                stackScrollBar.ManipulationMode = ManipulationModes.All;
                stackScrollBar.ManipulationDelta += (ss, ee) => ee.Handled = true;
            };
            var stackFieldData =
                docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context)
                    as DocumentCollectionFieldModelController;

            // create a dynamic gridview that wraps content in borders
            if (stackFieldData != null)
            {
                CreateStack(context, stack, stackFieldData, isInterfaceBuilderLayout);
                stackFieldData.OnDocumentsChanged += delegate (IEnumerable<DocumentController> documents)
                {
                    CreateStack(context, stack, stackFieldData, isInterfaceBuilderLayout);
                };
            }
            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(stack, docController, dataDocument);
            }
            return stack;
        }

        private static void CreateStack(Context context, GridView stack, DocumentCollectionFieldModelController stackFieldData, bool isInterfaceBuilderLayout)
        {
            double maxHeight = 0;
            stack.Items.Clear();
            foreach (var stackDoc in stackFieldData.GetDocuments())
            {
                Border b = new Border();
                FrameworkElement item = stackDoc.MakeViewUI(context, isInterfaceBuilderLayout);
                b.Child = item;
                maxHeight = Math.Max(maxHeight, double.IsNaN(item.Height) ? 0 : item.Height);
                stack.Items.Add(b);
            }
            foreach (Border b in stack.Items)
            {
                b.Height = maxHeight;
            }
        }

        public static FrameworkElement MakeFreeFormView(DocumentController docController, Context context, bool isInterfaceBuilderLayout, DocumentController dataDocument)
        {
            var stack = new Grid();
            stack.HorizontalAlignment = HorizontalAlignment.Left;
            stack.VerticalAlignment = VerticalAlignment.Top;
            var stackFieldData =
                docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context)
                    as DocumentCollectionFieldModelController;

            // create a dynamic gridview that wraps content in borders
            if (stackFieldData != null)
                foreach (var stackDoc in stackFieldData.GetDocuments())
                {

                    FrameworkElement item = stackDoc.MakeViewUI(context, isInterfaceBuilderLayout);
                    if (item != null)
                    {
                        var posController = GetPositionField(stackDoc, context);

                        item.HorizontalAlignment = HorizontalAlignment.Left;
                        item.VerticalAlignment = VerticalAlignment.Top;
                        BindTranslation(item, posController);
                        stack.Children.Add(item);
                    }
                }
            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(stack, docController, dataDocument);
            }
            return stack;
        }
    }
}