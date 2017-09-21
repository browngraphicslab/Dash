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
            OperatorView opView = new OperatorView { DataContext = opfmc.FieldReference };
            var opDoc = opfmc.FieldReference.GetDocumentController(null);
            var stack = new Windows.UI.Xaml.Controls.StackPanel();
            stack.Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical;
        
            var searchBox = new TextBox();
            searchBox.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            searchBox.Height = 50;
            searchBox.Text = "";
            searchBox.AcceptsReturn = true;
            searchBox.VerticalAlignment = VerticalAlignment.Top;
            searchBox.TextChanged += ((sender, e) =>
            {
                DBTest.ResetCycleDetection();
                if (opDoc != null)
                {
                    opDoc.SetField(Controllers.Operators.DBFilterOperatorFieldModelController.FieldPatternKey,
                        new TextFieldModelController((sender as Windows.UI.Xaml.Controls.TextBox).Text), false);
                }
            });

            var inputsBox = new TextBox();
            inputsBox.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            inputsBox.Height = 35;
            inputsBox.Width = 100;
            inputsBox.Text = "";
            inputsBox.AcceptsReturn = true;
            inputsBox.VerticalAlignment = VerticalAlignment.Top;
            var binding = new FieldBinding<DocumentCollectionFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = opDoc,
                Key = DBFilterOperatorFieldModelController.InputDocsKey,
                Converter = new DocumentCollectionToStringConverter(true),
                Context = new Context(opDoc)
            };
            inputsBox.AddFieldBinding(TextBox.TextProperty, binding);

            var matchBox = new TextBox();
            matchBox.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            matchBox.Height = 35;
            matchBox.Width = 100;
            matchBox.Text = "";
            matchBox.AcceptsReturn = true;
            matchBox.VerticalAlignment = VerticalAlignment.Top;
            var outDoc = opDoc.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, new Context(opDoc)).Data;
            var binding2 = new FieldBinding<DocumentCollectionFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = outDoc,
                Key = DBFilterOperatorFieldModelController.ResultsKey,
                Converter = new DocumentCollectionToStringConverter(true),
                Context = new Context(outDoc)
            }; 
            matchBox.AddFieldBinding(TextBox.TextProperty, binding2);

            stack.Children.Add(searchBox);
            stack.Children.Add(matchBox);
            stack.Children.Add(inputsBox);
            stack.HorizontalAlignment = HorizontalAlignment.Stretch;
            stack.VerticalAlignment = VerticalAlignment.Top;
            opView.OperatorContent = stack;

            if (isInterfaceBuilderLayout)
                return new SelectableContainer(opView, docController);
            return opView;
        }
    }
}
