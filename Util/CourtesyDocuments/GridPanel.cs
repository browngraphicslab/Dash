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
    class GridPanel : CourtesyDocument
    {
        public static readonly DocumentType GridPanelDocumentType = new DocumentType("57305127-4B20-4FAA-B958-820F77C290B8", "Grid Panel");

        public static readonly Key HorizontalAlignmentKey = new Key("B43231DA-5A22-45A3-8476-005A62396686", "Horizontal Alignment");
        public static readonly Key VerticalAlignmentKey = new Key("227B9887-BC09-40E4-A3F0-AD204D00E48D", "Vertical Alignment");

        public static readonly Key GridRowsTypeKey = new Key("17F67B9A-A9C2-4325-BEC1-B8308B48FC39", "RowDefinitionTypes");
        public static readonly Key GridRowsValueKey = new Key("3761458D-757E-4350-8BF5-FC42D3DCF70F", "RowDefinitionValues");
        public static readonly Key GridColumnsTypeKey = new Key("7B698361-0F0E-4322-983C-055989376C72", "ColumnDefinitionTypes");
        public static readonly Key GridColumnsValueKey = new Key("8AA607A7-1FED-4D4F-A606-1DDF4F86B7E9", "ColumnDefinitionValues");

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

        protected static void BindRowDefinitions(Grid element, DocumentController docController,
            Context context)
        {
            var rowTypes =
                docController.GetDereferencedField(GridRowsTypeKey, context) as ListFieldModelController<NumberFieldModelController>;
            var rowValues =
                docController.GetDereferencedField(GridRowsValueKey, context) as ListFieldModelController<NumberFieldModelController>;
            if (rowTypes == null || rowValues == null) return;
            var typeData = rowTypes.TypedData;
            var valueData = rowValues.TypedData;
            Debug.Assert(typeData.Count == valueData.Count);
            element.RowDefinitions.Clear();
            for (int i = 0; i < typeData.Count; ++i)
            {
                RowDefinition row = new RowDefinition
                {
                    Height = new GridLength(valueData[i].Data, (GridUnitType) typeData[i].Data)
                };
                element.RowDefinitions.Add(row);
            }
        }

        protected static void BindColumnDefinitions(Grid element, DocumentController docController,
            Context context)
        {
            var columnTypes =
                docController.GetDereferencedField(GridColumnsTypeKey, context) as ListFieldModelController<NumberFieldModelController>;
            var columnValues =
                docController.GetDereferencedField(GridColumnsValueKey, context) as ListFieldModelController<NumberFieldModelController>;
            if (columnTypes == null || columnValues == null) return;
            var typeData = columnTypes.TypedData;
            var valueData = columnValues.TypedData;
            Debug.Assert(typeData.Count == valueData.Count);
            element.ColumnDefinitions.Clear();
            for (int i = 0; i < typeData.Count; ++i)
            {
                Debug.WriteLine("Test");
                Debug.WriteLine((int)GridUnitType.Auto);
                Debug.WriteLine((int)GridUnitType.Star);
                Debug.WriteLine((GridUnitType)(int)GridUnitType.Auto);
                Debug.WriteLine((GridUnitType)(int)GridUnitType.Star);
                Debug.WriteLine((GridUnitType)typeData[i].Data);
                Debug.WriteLine(typeData[i].Data);
                ColumnDefinition column = new ColumnDefinition
                {
                    Width = new GridLength(valueData[i].Data, (GridUnitType)typeData[i].Data)
                };
                element.ColumnDefinitions.Add(column);
            }
        }

        protected static void BindHorizontalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var horizontalAlignmentFmc = docController.GetField(HorizontalAlignmentKey) as NumberFieldModelController;
            if (horizontalAlignmentFmc != null)
            {
                element.HorizontalAlignment = (HorizontalAlignment) horizontalAlignmentFmc.Data;
            }
        }

        protected static void BindVerticalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var verticalAlignmentFmc = docController.GetField(VerticalAlignmentKey) as NumberFieldModelController;
            if (verticalAlignmentFmc != null)
            {
                element.VerticalAlignment = (VerticalAlignment)verticalAlignmentFmc.Data;
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
            AddBinding(grid, docController, GridRowsTypeKey, context, BindRowDefinitions);
            AddBinding(grid, docController, GridColumnsTypeKey, context, BindColumnDefinitions);
            AddBinding(grid, docController, HorizontalAlignmentKey, context, BindHorizontalAlignment);
            AddBinding(grid, docController, VerticalAlignmentKey, context, BindVerticalAlignment);

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

        public static void SetHorizontalAlignment(this DocumentController document, HorizontalAlignment alignment)
        {
            document.SetField(GridPanel.HorizontalAlignmentKey, new NumberFieldModelController((int)alignment), true);
        }

        public static void SetVerticalAlignment(this DocumentController document, VerticalAlignment alignment)
        {
            document.SetField(GridPanel.VerticalAlignmentKey, new NumberFieldModelController((int)alignment), true);
        }

        public static void SetGridRowDefinitions(this DocumentController document, List<RowDefinition> rows)
        {
            ListFieldModelController<NumberFieldModelController> types = new ListFieldModelController<NumberFieldModelController>();
            ListFieldModelController<NumberFieldModelController> values = new ListFieldModelController<NumberFieldModelController>();
            foreach (var row in rows)
            {
                int type = (int)row.Height.GridUnitType;
                double value = row.Height.Value;
                types.Add(new NumberFieldModelController(type));
                values.Add(new NumberFieldModelController(value));
            }
            document.SetField(GridPanel.GridRowsTypeKey, types, true);
            document.SetField(GridPanel.GridRowsValueKey, values, true);
        }

        public static void SetGridColumnDefinitions(this DocumentController document, List<ColumnDefinition> columns)
        {
            ListFieldModelController<NumberFieldModelController> types = new ListFieldModelController<NumberFieldModelController>();
            ListFieldModelController<NumberFieldModelController> values = new ListFieldModelController<NumberFieldModelController>();
            foreach (var column in columns)
            {
                int type = (int)column.Width.GridUnitType;
                double value = column.Width.Value;
                types.Add(new NumberFieldModelController(type));
                values.Add(new NumberFieldModelController(value));
            }
            document.SetField(GridPanel.GridColumnsTypeKey, types, true);
            document.SetField(GridPanel.GridColumnsValueKey, values, true);
        }

        public static void AddChild(this DocumentController document, DocumentController child, Context context = null)
        {
            var children = document.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
            Debug.Assert(children != null);
            children.AddDocument(child);
        }
    }
}
