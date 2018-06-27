﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;

namespace Dash
{

    /// <summary>
    /// A generic document type containing a single image. The Data field on an ImageBox is a reference which eventually
    /// ends in an
    /// ImageController or an ImageController
    /// </summary>
    public class TemplateBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("21D67C5E-9A2E-42C8-975A-AD60C728DDAE", "Template Box");
        private static readonly string PrototypeId = "159D2321-FBB4-4A2D-9902-9BDE105CABEF";

        public TemplateBox(double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), new ListController<DocumentController>());
            SetupDocument(DocumentType, PrototypeId, "Template Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var grid = new Grid
            {
                Width = 300,
                Height = 400
            };
            LayoutDocuments(docController, context, grid);

            grid.Clip = new RectangleGeometry();
            grid.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                grid.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };

            var newCtxt = new Context(context);

            void OnDocumentFieldUpdatedHandler(DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args, Context secondContext)
            {
                var cfargs = args.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                if (cfargs.ListAction ==
                    ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                {
                    AddDocuments(cfargs.ChangedDocuments, newCtxt, grid);
                }
                else
                {
                    LayoutDocuments(sender, newCtxt, grid);
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

        private static ListController<DocumentController> GetLayoutDocumentCollection(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<ListController<DocumentController>>(context);
        }

        private static void AddDocuments(List<DocumentController> docs, Context context, Grid grid)
        {
            foreach (var layoutDoc in docs)
            {
                var layoutView = layoutDoc.MakeViewUI(context);
                layoutDoc.SetField(KeyStore.WidthFieldKey,
                    new NumberController(layoutDoc.GetField<PointController>(KeyStore.ActualSizeKey).Data.X), true);
                layoutView.AddFieldBinding(UIElement.RenderTransformProperty, new FieldBinding<PointController>
                {
                    Document = layoutDoc,
                    Key = KeyStore.PositionFieldKey,
                    Mode = BindingMode.OneWay,
                    Converter = new PointToTranslateTransformConverter()
                });
                layoutDoc.SetHorizontalAlignment(HorizontalAlignment.Left);
                layoutDoc.SetVerticalAlignment(VerticalAlignment.Top);
                //layoutView.RenderTransform = new TranslateTransform
                //{
                //    X = layoutDoc.GetPosition().Value.X,
                //    Y = layoutDoc.GetPosition().Value.Y
                //};
                grid.Children.Add(layoutView);
            }
        }
    }

}