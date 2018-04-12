using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class DBSearchOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("88549C01-5CFA-4580-A357-D7BE895B11DE", "DB Search Operator Box");

        public DBSearchOperatorBox(ReferenceController refToOp)
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

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var data = docController.GetField(KeyStore.DataKey);
            var opfmc = (data as ReferenceController);
            OperatorView opView = new OperatorView { DataContext = opfmc.GetFieldReference()};
            var opDoc = opfmc.GetDocumentController(null);
            var stack = new Windows.UI.Xaml.Controls.StackPanel();
            stack.Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical;
            var returnBox = new Windows.UI.Xaml.Controls.TextBox();
            returnBox.Style = Application.Current.Resources["xSearchTextBox"] as Style;
            returnBox.Height = 50;
            returnBox.Text = "";
            returnBox.AcceptsReturn = true;
            returnBox.VerticalAlignment = VerticalAlignment.Top;
            returnBox.TextChanged += ((sender, e) =>
            {
                //DBTest.ResetCycleDetection();
                //if (opDoc != null)
                //    opDoc.SetField(Controllers.Operators.DBSearchOperatorController.ReturnDocKey,
                //        new TextController((sender as Windows.UI.Xaml.Controls.TextBox).Text), false);
            });
            var searchBox = new Windows.UI.Xaml.Controls.TextBox();
            searchBox.Style = Application.Current.Resources["xSearchTextBox"] as Style;
            searchBox.Height = 50;
            searchBox.Text = "";
            searchBox.AcceptsReturn = true;
            searchBox.VerticalAlignment = VerticalAlignment.Top;
            searchBox.TextChanged += ((sender, e) =>
            {
                //DBTest.ResetCycleDetection();
                //if (opDoc != null)
                //    opDoc.SetField(Controllers.Operators.DBSearchOperatorController.FieldPatternKey,
                //        new TextController((sender as Windows.UI.Xaml.Controls.TextBox).Text), false);
            });

            //var scopeDoc = new TextingBox(opDoc.GetField(Controllers.Operators.DBSearchOperatorController.SearchForDocKey));

            stack.Children.Add(searchBox);
            stack.Children.Add(returnBox);
            //stack.Children.Add(scopeDoc.makeView(scopeDoc.Document, context));
            stack.HorizontalAlignment = HorizontalAlignment.Stretch;
            stack.VerticalAlignment = VerticalAlignment.Top;
            opView.OperatorContent = stack;
            
            return opView;
        }
    }
}
