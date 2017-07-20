using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

namespace Dash
{
    class GridPanel : CourtesyDocuments.CourtesyDocument
    {
        public static DocumentType GridPanelDocumentType = new DocumentType("57305127-4B20-4FAA-B958-820F77C290B8", "Grid Panel");

        public GridPanel()
        {
            
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
        }

        protected delegate void BindingDelegate(FrameworkElement element, DocumentController controller, Context c);

        protected static void AddBinding(FrameworkElement element, DocumentController docController, Key k, Context context,
            BindingDelegate bindingDelegate)
        {
            bindingDelegate.Invoke(element, docController, context);
            docController.AddFieldUpdatedListener(k,
                delegate(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    bindingDelegate(element, sender, args.Context);//TODO Should be context or args.Context?
                });
        }

        protected static void BindWidth(FrameworkElement element, DocumentController docController, Context context)
        {
            var widthFmc = docController.GetWidthField(context);
            Binding widthBinding = new Binding
            {
                Source = widthFmc,
                Path = new PropertyPath(nameof(widthFmc.Data)),
                Mode = BindingMode.TwoWay
            };
            element.SetBinding(FrameworkElement.WidthProperty, widthBinding);
        }

        protected static void BindHeight(FrameworkElement element, DocumentController docController, Context context)
        {
            var heightFmc = docController.GetHeightField(context);
            Binding heightBinding = new Binding
            {
                Source = heightFmc,
                Path = new PropertyPath(nameof(heightFmc.Data)),
                Mode = BindingMode.TwoWay
            };
            element.SetBinding(FrameworkElement.HeightProperty, heightBinding);
        }

        protected static void BindPosition(FrameworkElement element, DocumentController docController, Context context)
        {
            var positionFmc = docController.GetPositionField(context);
            Binding positionBinding = new Binding
            {
                Source = positionFmc,
                Path = new PropertyPath(nameof(positionFmc.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new PointToTranslateTransformConverter()
            };
            element.SetBinding(UIElement.RenderTransformProperty, positionBinding);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            Grid grid = new Grid();
            AddBinding(grid, docController, DashConstants.KeyStore.WidthFieldKey, context, BindWidth);
            AddBinding(grid, docController, DashConstants.KeyStore.HeightFieldKey, context, BindHeight);
            AddBinding(grid, docController, DashConstants.KeyStore.PositionFieldKey, context, BindPosition);
            return grid;
        }
    }
}
