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
    class GridLayout : CourtesyDocument
    {
        public static readonly DocumentType GridPanelDocumentType = new DocumentType("57305127-4B20-4FAA-B958-820F77C290B8", "Grid Layout");

        public static readonly KeyController GridRowsTypeKey = new KeyController("17F67B9A-A9C2-4325-BEC1-B8308B48FC39", "RowDefinitionTypes");
        public static readonly KeyController GridRowsValueKey = new KeyController("3761458D-757E-4350-8BF5-FC42D3DCF70F", "RowDefinitionValues");
        public static readonly KeyController GridColumnsTypeKey = new KeyController("7B698361-0F0E-4322-983C-055989376C72", "ColumnDefinitionTypes");
        public static readonly KeyController GridColumnsValueKey = new KeyController("8AA607A7-1FED-4D4F-A606-1DDF4F86B7E9", "ColumnDefinitionValues");

        public GridLayout() : this(new Point(0, 0), new Size(double.NaN, double.NaN))
        {
            
        }

        public GridLayout(Point position, Size size)
        {
            var fields = DefaultLayoutFields(position, size,
                new DocumentCollectionFieldModelController());
            Document = new DocumentController(fields, GridPanelDocumentType);
        }

        public GridLayout(Point position) : this(position, new Size(double.NaN, double.NaN))
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

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilder)
        {
            return MakeView(docController, context, null, isInterfaceBuilder);
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
                ColumnDefinition column = new ColumnDefinition
                {
                    Width = new GridLength(valueData[i].Data, (GridUnitType)typeData[i].Data)
                };
                element.ColumnDefinitions.Add(column);
            }
        }

        protected static void SetupBindings(Grid grid, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(grid, docController, context);

            AddBinding(grid, docController, GridRowsTypeKey, context, BindRowDefinitions);
            AddBinding(grid, docController, GridColumnsTypeKey, context, BindColumnDefinitions);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilder)
        {
            context = context ?? new Context();
            context.AddDocumentContext(docController);
            Grid grid = new Grid();
            
            SetupBindings(grid, docController, context);

            var col = docController.GetDereferencedField(KeyStore.DataKey, context)
                ?.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            Debug.Assert(col != null);
            foreach (var documentController in col.GetDocuments())
            {
                var element = documentController.MakeViewUI(context, isInterfaceBuilder);
                grid.Children.Add(element);
            }
            if (isInterfaceBuilder)
            {
                var container = new SelectableContainer(grid, docController, dataDocument);
                SetupBindings(container, docController, context);
                return container;
            }
            return grid;
        }
    }

    public static class GridDocumentExtensions
    {
        public static void SetGridRowDefinitions(this DocumentController document, List<RowDefinition> rows)
        {
            Debug.Assert(document.DocumentType.Equals(GridLayout.GridPanelDocumentType));
            ListFieldModelController<NumberFieldModelController> types = new ListFieldModelController<NumberFieldModelController>();
            ListFieldModelController<NumberFieldModelController> values = new ListFieldModelController<NumberFieldModelController>();
            foreach (var row in rows)
            {
                int type = (int)row.Height.GridUnitType;
                double value = row.Height.Value;
                types.Add(new NumberFieldModelController(type));
                values.Add(new NumberFieldModelController(value));
            }
            document.SetField(GridLayout.GridRowsTypeKey, types, true);
            document.SetField(GridLayout.GridRowsValueKey, values, true);
        }

        public static void SetGridColumnDefinitions(this DocumentController document, List<ColumnDefinition> columns)
        {
            Debug.Assert(document.DocumentType.Equals(GridLayout.GridPanelDocumentType));
            ListFieldModelController<NumberFieldModelController> types = new ListFieldModelController<NumberFieldModelController>();
            ListFieldModelController<NumberFieldModelController> values = new ListFieldModelController<NumberFieldModelController>();
            foreach (var column in columns)
            {
                int type = (int)column.Width.GridUnitType;
                double value = column.Width.Value;
                types.Add(new NumberFieldModelController(type));
                values.Add(new NumberFieldModelController(value));
            }
            document.SetField(GridLayout.GridColumnsTypeKey, types, true);
            document.SetField(GridLayout.GridColumnsValueKey, values, true);
        }

        public static void AddChild(this DocumentController document, DocumentController child, Context context = null)
        {
            var children = document.GetDereferencedField(KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
            Debug.Assert(children != null);
            children.AddDocument(child);
        }
    }
}
