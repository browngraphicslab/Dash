using System;

namespace Dash
{
    public static class FieldModelControllerFactory
    {

        public static FieldModelController CreateFromModel(FieldModel model)
        {
            if (model is DocumentCollectionFieldModel)
            {
                return new DocumentCollectionFieldModelController(model as DocumentCollectionFieldModel);
            }
            if (model is TextFieldModel)
            {
                return new TextFieldModelController(model as TextFieldModel);
            }
            if (model is DocumentModelFieldModel)
            {
                return new DocumentFieldModelController(model as DocumentModelFieldModel);
            }
            if (model is NumberFieldModel)
            {
                return new NumberFieldModelController(model as NumberFieldModel);
            }
            if (model is ImageFieldModel)
            {
                return new ImageFieldModelController(model as ImageFieldModel);
            }

            throw new ArgumentException("We do not have a conversion yet for the passed in model");
        }

    }
}
