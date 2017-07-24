using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash;
using DashShared;

namespace Dash
{
    /// <summary>
    /// The data field for a collection is a document collection field model controller
    /// </summary>
    public class CollectionBox : CourtesyDocument
    {

        public static DocumentType DocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Generic Collection");
        private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";


        public CollectionBox(FieldModelController refToCollection, double x = 0, double y = 0, double w = 400, double h = 400)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var docFieldModelController = new DocumentCollectionFieldModelController(new List<DocumentController>());
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), docFieldModelController);
            fields[DashConstants.KeyStore.IconTypeFieldKey] = new NumberFieldModelController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) ?? null;

            if (data != null)
            {
                var opacity = (docController.GetDereferencedField(new Key("opacity", "opacity"), context) as NumberFieldModelController)?.Data;

                double opacityValue = opacity.HasValue ? (double)opacity : 1;

                var collectionFieldModelController = data.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
                Debug.Assert(collectionFieldModelController != null);

                var collectionViewModel = new CollectionViewModel(docController, DashConstants.KeyStore.DataKey, context); //  collectionFieldModelController, context);

                var view = new CollectionView(collectionViewModel);
                view.Opacity = opacityValue;
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(view, docController);
                }
                return view;
            }
            return new Grid();
        }
    }
}