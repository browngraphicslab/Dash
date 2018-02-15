using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash;
using Dash.Views;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Given a reference to an operator field model, constructs a document type that displays that operator.
    /// </summary>
    public class OperatorBox : CourtesyDocument
    {
        public OperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(250,100), refToOp);
            Document = new DocumentController(fields, DashConstants.TypeStore.OperatorBoxType);
            if (refToOp.DereferenceToRoot<OperatorController>(null).IsCompound())
            {
                DocumentController controller = refToOp.GetDocumentController(null);
                controller.SetField(KeyStore.CollectionKey, new ListController<DocumentController>(), true);
            }
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
            Context context)
        {
            return MakeView(docController, context, null);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElements = null)
        {
            return MakeOperatorView(docController, context, keysToFrameworkElements);
        }

        /// <summary>
        /// Helper method for creating operator views which lets the callee supply a custom operator UI through customLayout
        /// </summary>
        /// <returns></returns>
        public static FrameworkElement MakeOperatorView(DocumentController docController,
            Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElements, Func<FrameworkElement> customLayout = null)
        {

            var data = docController.GetField(KeyStore.DataKey);
            var opfmc = data as ReferenceController;
            Debug.Assert(opfmc != null, "We assume that documents containing operators contain a reference to the required operator doc in the data key");
            Debug.Assert(opfmc.GetFieldReference() is DocumentFieldReference, "We assume that the operator view contains a reference to the operator as a key on a document");
            var opView = new OperatorView(keysToFrameworkElements)
            {
                DataContext = opfmc.GetFieldReference(),
            };

            if (customLayout != null)
            {
                opView.OperatorContent = customLayout.Invoke();
            }

            SetupBindings(opView, docController, context);

            if (keysToFrameworkElements != null) keysToFrameworkElements[opfmc?.FieldKey] = opView;
            
            return opView;
        }


    }
}