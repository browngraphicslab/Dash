using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash.Views;
using DashShared;

namespace Dash
{
    class FilterOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("30651C8A-C3EC-4CF6-999B-F8F1ED094D65", "Filter Operator Box");

        public FilterOperatorBox(ReferenceFieldModelController refToOp)
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
            var data = docController.GetField(DashConstants.KeyStore.DataKey);
            var opfmc = (data as ReferenceFieldModelController);
            OperatorView opView = new OperatorView {DataContext = opfmc.FieldReference};
            var filterView = new FilterView();
            opView.OperatorContent = filterView;
            if (isInterfaceBuilderLayout) return new SelectableContainer(opView, docController);
            return opView;
        }
    }
    class DBSearchOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("88549C01-5CFA-4580-A357-D7BE895B11DE", "DB Search Operator Box");

        public DBSearchOperatorBox(ReferenceFieldModelController refToOp)
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
            var data = docController.GetField(DashConstants.KeyStore.DataKey);
            var opfmc = (data as ReferenceFieldModelController);
            OperatorView opView = new OperatorView { DataContext = opfmc.FieldReference };
            var opDoc = opfmc.FieldReference.GetDocumentController(null);
            var stack = new Windows.UI.Xaml.Controls.StackPanel();
            stack.Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical;
            var returnBox = new Windows.UI.Xaml.Controls.TextBox();
            returnBox.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            returnBox.Height = 50;
            returnBox.Text = "";
            returnBox.AcceptsReturn = true;
            returnBox.VerticalAlignment = VerticalAlignment.Top;
            returnBox.TextChanged += ((sender, e) =>
            {
                DBTest.ResetCycleDetection();
                if (opDoc != null)
                    opDoc.SetField(Controllers.Operators.DBSearchOperatorFieldModelController.ReturnDocKey,
                        new TextFieldModelController((sender as Windows.UI.Xaml.Controls.TextBox).Text), false);
            });
            var searchBox = new Windows.UI.Xaml.Controls.TextBox();
            searchBox.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            searchBox.Height = 50;
            searchBox.Text = "";
            searchBox.AcceptsReturn = true;
            searchBox.VerticalAlignment = VerticalAlignment.Top;
            searchBox.TextChanged += ((sender, e) =>
            {
                DBTest.ResetCycleDetection();
                if (opDoc != null)
                    opDoc.SetField(Controllers.Operators.DBSearchOperatorFieldModelController.FieldPatternKey, 
                        new TextFieldModelController((sender as Windows.UI.Xaml.Controls.TextBox).Text), false);
            });

            var scopeDoc = new TextingBox(opDoc.GetField(Controllers.Operators.DBSearchOperatorFieldModelController.SearchForDocKey));

            stack.Children.Add(searchBox);
            stack.Children.Add(returnBox);
            stack.Children.Add(scopeDoc.makeView(scopeDoc.Document, context));
            stack.HorizontalAlignment = HorizontalAlignment.Stretch;
            stack.VerticalAlignment = VerticalAlignment.Top;
            opView.OperatorContent = stack;

            if (isInterfaceBuilderLayout) return new SelectableContainer(opView, docController);
            return opView;
        }
    }
}
