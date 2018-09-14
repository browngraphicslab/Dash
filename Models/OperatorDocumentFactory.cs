using System;
using System.Collections.Generic;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Provides static utilities for creating Documents that contain an OperatorFieldModel.
    /// </summary>
    public static class OperatorDocumentFactory
    {

        /// <summary>
        /// Takes in an operator field model controller, an optional title, and an optional function which sets the layout for the operator
        /// the optional function normally takes the form (rfmc => new CourtesyDocument(rfmc)) where the courtesy document is defining
        /// a custom view for the operator
        /// </summary>
        public static DocumentController CreateOperatorDocument(OperatorController opController, string title = null)
        {
            // set the operator and title field
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.OperatorKey] = new ListController<OperatorController>(new OperatorController[] { opController }),
                [KeyStore.TitleKey] = new TextController(title ?? "")
            };

            // create a new document to hold the operator
            var doc = new DocumentController(fields, DashConstants.TypeStore.OperatorType);

            var layoutPropertyInfo = typeof(OperatorController).GetProperty(nameof(OperatorController.LayoutFunc));

            var layoutFunc = (Func<ReferenceController, CourtesyDocument>) layoutPropertyInfo.GetValue(opController);

            // set the layout on the operator using the passed in func or a default OperatorBox with no custom content
            SetOperatorLayout(layoutFunc ?? (rfmc => new OperatorBox(rfmc)), doc);

            return doc;
        }

        /// <summary>
        /// Helper method to set the layout of the operator, sets the layout to the output of a courtesy document
        /// </summary>
        /// <param name="layoutFunc"></param>
        /// <param name="docContainingOp"></param>
        public static void SetOperatorLayout(Func<ReferenceController, CourtesyDocument> layoutFunc, DocumentController docContainingOp)
        {
            var layoutDoc = layoutFunc(new DocumentReferenceController(docContainingOp, KeyStore.OperatorKey)).Document;
            docContainingOp.SetActiveLayout(layoutDoc, true, true);

        }
    }
}
