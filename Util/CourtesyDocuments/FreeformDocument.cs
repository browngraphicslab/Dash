using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Dash;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class FreeFormDocument : CourtesyDocument
    {
        private static string PrototypeId = "A5614540-0A50-40F3-9D89-965B8948F2A2";

        public FreeFormDocument(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            Document = GetLayoutPrototype().MakeDelegate();
            var layoutDocumentCollection = new ListController<DocumentController>(layoutDocuments);
            var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
            Document.SetFields(fields, true); //TODO add fields to constructor parameters     

            Document.SetField(KeyStore.IconTypeFieldKey, new NumberController((double)IconTypeEnum.Api), true);
        }

        public FreeFormDocument() : this(new List<DocumentController>()) { }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var layoutDocCollection = new ListController<DocumentController>(new List<DocumentController>());
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), layoutDocCollection);
            var prototypeDocument = new DocumentController(fields, DashConstants.TypeStore.FreeFormDocumentLayout, PrototypeId);
            return prototypeDocument;
        }

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            throw new NotImplementedException("We don't have the dataDocument here and right now this is never called anyway");
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument,  Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null)
        {

            var grid = new Grid();
            SetupBindings(grid, docController, context);
            LayoutDocuments(docController, context, grid, keysToFrameworkElementsIn);

            grid.Clip = new RectangleGeometry();
            grid.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                grid.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };

            var c = new Context(context);

            void OnDocumentFieldUpdatedHandler(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c2)
            {
                var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
                var collFieldArgs = dargs.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                if (collFieldArgs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                {
                    AddDocuments(collFieldArgs.ChangedDocuments, c, grid, keysToFrameworkElementsIn);
                }
                else
                {
                    LayoutDocuments((DocumentController) sender, c, grid, keysToFrameworkElementsIn);
                }
            }

            grid.Loaded += delegate
            {
                docController.AddFieldUpdatedListener(KeyStore.DataKey, OnDocumentFieldUpdatedHandler);
            };
            grid.Unloaded += delegate
            {
                docController.RemoveFieldUpdatedListener(KeyStore.DataKey, OnDocumentFieldUpdatedHandler);
            };
            
            return grid;
        }

        private static void LayoutDocuments(DocumentController docController, Context context, Grid grid, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null)
        {
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetElements();
            grid.Children.Clear();
            AddDocuments(layoutDocuments, context, grid, keysToFrameworkElementsIn);
        }

        private static void AddDocuments(List<DocumentController> docs, Context context, Grid grid,  Dictionary<KeyController, FrameworkElement> keysToFrameworkElements=null)
        {
            foreach (var layoutDocument in docs)
            {
                var layoutView = layoutDocument.MakeViewUI(context, keysToFrameworkElements);
                // TODO this is a hack because the horizontal and vertical alignment of our layouts are by default stretch
                // TODO as set in SetDefaultLayouts, we should really be checking to see if this should be left and top, but for now
                // TODO it helps the freeformdocument position elements correctly
                layoutDocument.SetHorizontalAlignment(HorizontalAlignment.Left);
                layoutDocument.SetVerticalAlignment(VerticalAlignment.Top);
                BindPosition(layoutView, layoutDocument, context);
                grid.Children.Add(layoutView);
            }
        }

        private static ListController<DocumentController> GetLayoutDocumentCollection(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<ListController<DocumentController>>(context);
        }
    }

}