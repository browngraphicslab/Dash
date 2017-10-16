using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Given a reference to an operator field model, constructs a document type that displays that operator.
    /// </summary>
    public class OperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType =
            new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");

        public OperatorBox(ReferenceFieldModelController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(200,100), refToOp);
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

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElements = null, bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(KeyStore.DataKey) ?? null;
            var opfmc = (data as ReferenceFieldModelController);
            OperatorView opView = new OperatorView(keysToFrameworkElements) {DataContext = opfmc.FieldReference};
            SetupBindings(opView, docController, context);
            if (isInterfaceBuilderLayout) return new SelectableContainer(opView, docController);
            return opView;
        }
    }
}