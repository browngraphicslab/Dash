using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;
using System.Diagnostics;
using Dash.Controllers.Operators;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using Dash.Converters;

namespace Dash
{
    class DBFilterOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("88549C01-5CFA-4580-A357-D7BE895B11DE", "DB Search Operator Box");

        public DBFilterOperatorBox(ReferenceFieldModelController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), refToOp);
            Document = new DocumentController(fields, DocumentType);
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
            return MakeView(docController, context, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(KeyStore.DataKey);
            var opfmc = (data as ReferenceFieldModelController);
            var opDoc = opfmc.FieldReference.GetDocumentController(null);

            var stack = new StackPanel() { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top };
            var opView = new OperatorView { DataContext = opfmc.FieldReference, OperatorContent = stack };
            var chart = new Dash.DBFilterChart() { Width = 200, Height = 250, NumberBars = 4, OpDoc = opDoc };
            stack.Children.Add(chart);
           

            opDoc.DocumentFieldUpdated += (sender, args) => chart.UpdateChart();

            if (isInterfaceBuilderLayout)
                return new SelectableContainer(opView, docController);
            return opView;
        }

    }
}
