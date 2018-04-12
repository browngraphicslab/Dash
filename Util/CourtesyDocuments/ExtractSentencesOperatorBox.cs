﻿using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class ExtractSentencesOperatorBox : CourtesyDocument
    {
        public ExtractSentencesOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(470, 120), refToOp);
            Document = new DocumentController(fields, DashConstants.TypeStore.ExtractSentencesDocumentType);
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

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
            return OperatorBox.MakeOperatorView(documentController, context, () => new ExtractSentencesOperatorView());
        }
    }
}