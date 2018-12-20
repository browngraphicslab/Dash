using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Given a reference to an operator field model, constructs a document type that displays that operator.
    /// </summary>
    public class OperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.OperatorBoxType;
        private static readonly string PrototypeId = "A30A067F-01A2-4044-AB70-5ADB987DA04B";

        public OperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(250,100), refToOp);
            SetupDocument(DocumentType, PrototypeId, "OperatorBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController)
        {
            return MakeOperatorView(docController);
        }

        /// <summary>
        /// Helper method for creating operator views which lets the callee supply a custom operator UI through customLayout
        /// </summary>
        /// <returns></returns>
        public static FrameworkElement MakeOperatorView(DocumentController docController,Func<FrameworkElement> customLayout = null)
        {

            var data = docController.GetField(KeyStore.DataKey);
            var opfmc = data as ReferenceController;
            Debug.Assert(opfmc != null, "We assume that documents containing operators contain a reference to the required operator doc in the data key");
            Debug.Assert(opfmc.GetFieldReference() is DocumentFieldReference, "We assume that the operator view contains a reference to the operator as a key on a document");
            var opView = new OperatorView()
            {
                DataContext = opfmc.GetFieldReference().Resolve(null),
            };

            if (customLayout != null)
            {
                opView.OperatorContent = customLayout.Invoke();
            }
            
            return opView;
        }


    }
}
