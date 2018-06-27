using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using DashShared;

namespace Dash
{
    public class FreeFormDocument : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.FreeFormDocumentType;

        private static string PrototypeId = "A5614540-0A50-40F3-9D89-965B8948F2A2";

        public FreeFormDocument(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            var fields = DefaultLayoutFields(position, size, new ListController<DocumentController>(layoutDocuments));
            fields.Add(KeyStore.IconTypeFieldKey, new NumberController((double)IconTypeEnum.Api));
            SetupDocument(DocumentType, PrototypeId, "FreeFormDocument Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {

            var grid = new Grid();
            SetupBindings(grid, docController, context);
            LayoutDocuments(docController, context, grid);

            grid.Clip = new RectangleGeometry();
            grid.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                grid.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };

            var c = new Context(context);

            void OnDocumentFieldUpdatedHandler(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context c2)
            {
                var collFieldArgs = args.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                if (collFieldArgs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                {
                    AddDocuments(collFieldArgs.NewItems, c, grid);
                }
                else
                {
                    LayoutDocuments(sender, c, grid);
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

        private static void LayoutDocuments(DocumentController docController, Context context, Grid grid)
        {
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetElements();
            grid.Children.Clear();
            AddDocuments(layoutDocuments, context, grid);
        }

        private static void AddDocuments(List<DocumentController> docs, Context context, Grid grid)
        {
            foreach (var layoutDocument in docs)
            {
                var layoutView = layoutDocument.MakeViewUI(context);
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