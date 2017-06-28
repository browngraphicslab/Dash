using System;
using Dash.Models.OperatorModels.Set;

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
            if (model is ReferenceFieldModel)
            {
                return new ReferenceFieldModelController(model as ReferenceFieldModel);
            }
            if (model is OperatorFieldModel)
            {
                var opFM = model as OperatorFieldModel;
                switch (opFM.Type)
                {
                    case "Divide":
                        return new DivideOperatorFieldModelController(model as OperatorFieldModel);
                    case "ImageToUri":
                        return new ImageOperatorFieldModelController(model as OperatorFieldModel);
                    case "Intersection":
                        return new IntersectionOperatorModelController(model as OperatorFieldModel);
                    // TODO add shit to this 
                }
                //return new OperatorFieldModelController(model as OperatorFieldModel); 
            }
            throw new ArgumentException("We do not have a conversion yet for the passed in model");
        }

    }
}
