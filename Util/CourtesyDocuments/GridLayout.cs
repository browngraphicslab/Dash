using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    class GridLayout : CourtesyDocument
    {
        public static readonly DocumentType DocumentType = new DocumentType("57305127-4B20-4FAA-B958-820F77C290B8", "Grid Layout");
        private static readonly string PrototypeId = "0F5A4B9A-44BA-4FFE-9A35-0818BCD038A6";

        public static readonly KeyController GridRowsTypeKey = new KeyController("RowDefinitionTypes", "17F67B9A-A9C2-4325-BEC1-B8308B48FC39");
        public static readonly KeyController GridRowsValueKey = new KeyController("RowDefinitionValues", "3761458D-757E-4350-8BF5-FC42D3DCF70F");
        public static readonly KeyController GridColumnsTypeKey = new KeyController("ColumnDefinitionTypes", "7B698361-0F0E-4322-983C-055989376C72");
        public static readonly KeyController GridColumnsValueKey = new KeyController("ColumnDefinitionValues", "8AA607A7-1FED-4D4F-A606-1DDF4F86B7E9");

        public GridLayout(Point position, Size size)
        {
            var fields = DefaultLayoutFields(position, size, new ListController<DocumentController>());
            SetupDocument(DocumentType, PrototypeId, "GridLayout Prototype Layout", fields);
        }

        protected static void BindRowDefinitions(Grid element, DocumentController docController,
            Context context)
        { 
            var rowTypesAA =
                docController.GetDereferencedField(GridRowsTypeKey, context);
            var rowTypes = rowTypesAA as ListController<NumberController>;
            var rowValues =
                docController.GetDereferencedField(GridRowsValueKey, context) as ListController<NumberController>;
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
                docController.GetDereferencedField(GridColumnsTypeKey, context) as ListController<NumberController>;
            var columnValues =
                docController.GetDereferencedField(GridColumnsValueKey, context) as ListController<NumberController>;
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
            AddBinding(grid, docController, GridRowsTypeKey, context, BindRowDefinitions);
            AddBinding(grid, docController, GridColumnsTypeKey, context, BindColumnDefinitions);
        }


        public static FrameworkElement MakeView(DocumentController docController, Context context)

        {
            context = context ?? new Context();
            context.AddDocumentContext(docController);
            Grid grid = new Grid();
            
            SetupBindings(grid, docController, context);

            var col = docController.GetDereferencedField(KeyStore.DataKey, context)
                ?.DereferenceToRoot<ListController<DocumentController>>(context);
            Debug.Assert(col != null);
            foreach (var documentController in col.GetElements())
            {
                var element = documentController.MakeViewUI(context);
                grid.Children.Add(element);
            }
            return grid;
        }
    }

    public static class GridDocumentExtensions
    {
        public static void SetGridRowDefinitions(this DocumentController document, List<RowDefinition> rows)
        {
            Debug.Assert(document.DocumentType.Equals(GridLayout.DocumentType));
            ListController<NumberController> types = new ListController<NumberController>();
            ListController<NumberController> values = new ListController<NumberController>();
            foreach (var row in rows)
            {
                int type = (int)row.Height.GridUnitType;
                double value = row.Height.Value;
                types.Add(new NumberController(type));
                values.Add(new NumberController(value));
            }
            document.SetField(GridLayout.GridRowsTypeKey, types, true);
            document.SetField(GridLayout.GridRowsValueKey, values, true);
        }

        public static void SetGridColumnDefinitions(this DocumentController document, List<ColumnDefinition> columns)
        {
            Debug.Assert(document.DocumentType.Equals(GridLayout.DocumentType));
            ListController<NumberController> types = new ListController<NumberController>();
            ListController<NumberController> values = new ListController<NumberController>();
            foreach (var column in columns)
            {
                int type = (int)column.Width.GridUnitType;
                double value = column.Width.Value;
                types.Add(new NumberController(type));
                values.Add(new NumberController(value));
            }
            document.SetField(GridLayout.GridColumnsTypeKey, types, true);
            document.SetField(GridLayout.GridColumnsValueKey, values, true);
        }

        public static void AddChild(this DocumentController document, DocumentController child, Context context = null)
        {
            var children = document.GetDereferencedField(KeyStore.DataKey, context) as ListController<DocumentController>;
            Debug.Assert(children != null);
            if (!children.GetElements().Contains(child))
                children.Add(child);
        }
    }
}
