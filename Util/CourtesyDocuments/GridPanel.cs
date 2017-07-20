using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

namespace Dash
{
    class GridPanel : CourtesyDocuments.CourtesyDocument
    {
        public static readonly DocumentType GridPanelDocumentType = new DocumentType("57305127-4B20-4FAA-B958-820F77C290B8", "Grid Panel");

        public static readonly Key GridNumRowsKey = new Key("17F67B9A-A9C2-4325-BEC1-B8308B48FC39", "RowDefinitions");
        public static readonly Key GridNumColumnsKey = new Key("0319AF94-95E1-4518-BABD-8C48DF2CAA01", "ColumnDefinitions");

        public static readonly Key GridRowKey = new Key("FC447698-1C96-4014-94A5-845D411C1CD1", "Grid.Row");
        public static readonly Key GridColumnKey = new Key("E6663AA3-26E1-48D1-8A95-768EC0CFD4BC", "Grid.Column");
        public static readonly Key GridRowSpanKey = new Key("3F305CD6-343E-4155-AFEB-5530E499727C", "Grid.RowSpan");
        public static readonly Key GridColumnSpanKey = new Key("C0A16508-76AF-42B5-A3D7-D693FDD5AA84", "Grid.ColumnSpan");

        public GridPanel()
        {
            var fields = DefaultLayoutFields(new Point(0, 0), new Size(double.NaN, double.NaN),
                new DocumentCollectionFieldModelController());
            Document = new DocumentController(fields, GridPanelDocumentType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilder)
        {
            return MakeView(docController, context, isInterfaceBuilder);
        }

        protected delegate void BindingDelegate<in T>(T element, DocumentController controller, Context c) where T : FrameworkElement;

        protected static void AddBinding<T>(T element, DocumentController docController, Key k, Context context,
            BindingDelegate<T> bindingDelegate) where T : FrameworkElement
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

        protected static void BindNumRows(Grid element, DocumentController docController,
            Context context)
        {
            var numRowsFmc =
                docController.GetDereferencedField(GridNumRowsKey, context) as NumberFieldModelController;
            if (numRowsFmc != null)
            {
                int numRows = (int) numRowsFmc.Data;
                if (numRows == element.RowDefinitions.Count)
                {
                    return;
                }
                element.RowDefinitions.Clear();
                for (int i = 0; i < numRows - 1; ++i)
                {
                    RowDefinition autoRow = new RowDefinition {Height = GridLength.Auto};
                    element.RowDefinitions.Add(autoRow);
                }
                RowDefinition starRow = new RowDefinition {Height = new GridLength(1, GridUnitType.Star)};
                element.RowDefinitions.Add(starRow);
            }
        }

        protected static void BindNumColumns(Grid element, DocumentController docController,
            Context context)
        {
            var numColumnsFmc =
                docController.GetDereferencedField(GridNumColumnsKey, context) as NumberFieldModelController;
            if (numColumnsFmc != null)
            {
                int numColumns = (int)numColumnsFmc.Data;
                if (numColumns == element.ColumnDefinitions.Count)
                {
                    return;
                }
                element.ColumnDefinitions.Clear();
                for (int i = 0; i < numColumns - 1; ++i)
                {
                    ColumnDefinition autoColumn = new ColumnDefinition {Width = GridLength.Auto};
                    element.ColumnDefinitions.Add(autoColumn);
                }
                ColumnDefinition starColumn = new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)};
                element.ColumnDefinitions.Add(starColumn);
            }
        }

        protected static void BindGridRow(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridRowFmc = docController.GetField(GridRowKey) as NumberFieldModelController;
            if (gridRowFmc != null)
            {
                Grid.SetRow(element, (int)gridRowFmc.Data);
            }
        }

        protected static void BindGridColumn(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridColumnFmc = docController.GetField(GridColumnKey) as NumberFieldModelController;
            if (gridColumnFmc != null)
            {
                Grid.SetColumn(element, (int)gridColumnFmc.Data);
            }
        }

        protected static void BindGridRowSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridRowSpanFmc = docController.GetField(GridRowSpanKey) as NumberFieldModelController;
            if (gridRowSpanFmc != null)
            {
                Grid.SetRowSpan(element, (int)gridRowSpanFmc.Data);
            }
        }

        protected static void BindGridColumnSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridColumnSpanFmc = docController.GetField(GridColumnKey) as NumberFieldModelController;
            if (gridColumnSpanFmc != null)
            {
                Grid.SetColumnSpan(element, (int)gridColumnSpanFmc.Data);
            }
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilder)
        {
            context = context ?? new Context();
            context.AddDocumentContext(docController);
            Grid grid = new Grid();
            AddBinding(grid, docController, DashConstants.KeyStore.WidthFieldKey, context, BindWidth);
            AddBinding(grid, docController, DashConstants.KeyStore.HeightFieldKey, context, BindHeight);
            //AddBinding(grid, docController, DashConstants.KeyStore.PositionFieldKey, context, BindPosition);
            AddBinding(grid, docController, GridNumRowsKey, context, BindNumRows);
            AddBinding(grid, docController, GridNumRowsKey, context, BindNumColumns);

            var col = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context)
                ?.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            Debug.Assert(col != null);
            foreach (var documentController in col.GetDocuments())
            {
                var element = documentController.MakeViewUI(context, isInterfaceBuilder);
                //Set column, row, and span
                AddBinding(element, documentController, GridRowKey, context, BindGridRow);
                AddBinding(element, documentController, GridColumnKey, context, BindGridColumn);
                AddBinding(element, documentController, GridRowSpanKey, context, BindGridRowSpan);
                AddBinding(element, documentController, GridColumnKey, context, BindGridColumnSpan);
                grid.Children.Add(element);
            }
            return grid;
        }
    }

    public static class GridDocumentExtensions
    {
        public static void SetGridRow(this DocumentController document, int row)
        {
            document.SetField(GridPanel.GridRowKey, new NumberFieldModelController(row), true);
        }

        public static void SetGridColumn(this DocumentController document, int column)
        {
            document.SetField(GridPanel.GridColumnKey, new NumberFieldModelController(column), true);
        }

        public static void SetGridRowSpan(this DocumentController document, int rowSpan)
        {
            document.SetField(GridPanel.GridRowSpanKey, new NumberFieldModelController(rowSpan), true);
        }

        public static void SetGridColumnSpan(this DocumentController document, int columnSpan)
        {
            document.SetField(GridPanel.GridColumnSpanKey, new NumberFieldModelController(columnSpan), true);
        }

        public static void SetGridNumColumns(this DocumentController document, int numColumns)
        {
            document.SetField(GridPanel.GridNumColumnsKey, new NumberFieldModelController(numColumns), true);
        }

        public static void SetGridNumRows(this DocumentController document, int numRows)
        {
            document.SetField(GridPanel.GridNumRowsKey, new NumberFieldModelController(numRows), true);
        }

        public static void AddChild(this DocumentController document, DocumentController child, Context context = null)
        {
            var children = document.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
            Debug.Assert(children != null);
            children.AddDocument(child);
        }
    }
}
