using System;
using System.Collections.Generic;
using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class TypeInfoHelper
    {
        private static readonly Dictionary<Type, TypeInfo> TypeDict = new Dictionary<Type, TypeInfo>
        {
            [typeof(NumberFieldModelController)] = TypeInfo.Number,
            [typeof(TextFieldModelController)] = TypeInfo.Text,
            [typeof(PointFieldModelController)] = TypeInfo.Point,
            [typeof(ListFieldModelController<>)] = TypeInfo.List,
            [typeof(DocumentCollectionFieldModelController)] = TypeInfo.Collection,
            [typeof(DocumentFieldModelController)] = TypeInfo.Document
        };

        public static FieldModel CreateFieldModelController(FieldModelDTO fieldModelDTO, TypeInfo listType = TypeInfo.None)
        {
            var x = CreateFieldModelControllerHelper(fieldModelDTO, listType);
            //x.FieldModel.Id = fieldModelDTO.Id;
            return x;
        }

        private static FieldModel CreateFieldModelControllerHelper(FieldModelDTO fieldModelDTO, TypeInfo listType = TypeInfo.None)
        {
            var data = fieldModelDTO.Data;
            switch (fieldModelDTO.Type)
            {
                case TypeInfo.Text:
                    return new TextFieldModel(data as string, fieldModelDTO.Id);
                case TypeInfo.Number:
                    return new NumberFieldModel((double) data, fieldModelDTO.Id);
                case TypeInfo.Image:
                    return new ImageFieldModel(data as Uri, fieldModelDTO.Id);
                case TypeInfo.Collection:
                    return new DocumentCollectionFieldModel(data as List<string>, fieldModelDTO.Id);
                case TypeInfo.Document:
                    return new DocumentFieldModel(data as DocumentModel, fieldModelDTO.Id);
                case TypeInfo.Reference:                 
                    return new ReferenceFieldModel(data as FieldReference, fieldModelDTO.Id);
                case TypeInfo.Operator: //TODO What should this do?
                    var typeAndCompound = data as Tuple<string, bool>;
                    return new OperatorFieldModel(typeAndCompound.Item1, typeAndCompound.Item2, fieldModelDTO.Id);
                case TypeInfo.Point:
                    return new PointFieldModel((Point) data, fieldModelDTO.Id);
                case TypeInfo.List:
                    throw new NotImplementedException();
                    //switch (listType) //TODO support list of list?
                    //{
                    //    case TypeInfo.Number:
                    //        return new ListFieldModel();<NumberFieldModelController>(
                    //            data as IEnumerable<NumberFieldModelController>);
                    //    case TypeInfo.Image:
                    //        return new ListFieldModelController<ImageFieldModelController>(
                    //            data as IEnumerable<ImageFieldModelController>);
                    //    case TypeInfo.Document:
                    //        return new ListFieldModelController<DocumentFieldModelController>(
                    //            data as IEnumerable<DocumentFieldModelController>);
                    //    case TypeInfo.Point:
                    //        return new ListFieldModelController<PointFieldModelController>(
                    //            data as IEnumerable<PointFieldModelController>);
                    //    case TypeInfo.Text:
                    //        return new ListFieldModelController<TextFieldModelController>(
                    //            data as IEnumerable<TextFieldModelController>);
                    //    case TypeInfo.Reference:
                    //        return new ListFieldModelController<ReferenceFieldModelController>(
                    //            data as IEnumerable<ReferenceFieldModelController>);
                    //    case TypeInfo.Collection:
                    //        return new ListFieldModelController<DocumentCollectionFieldModelController>(
                    //            data as IEnumerable<DocumentCollectionFieldModelController>);
                    //    default:
                    //        return null;
                    //}
                default:
                    return null;
            }
        }

        public static TypeInfo TypeToTypeInfo(Type type)
        {
            if (TypeDict.ContainsKey(type))
                return TypeDict[type];

            return TypeInfo.None;
        }
    }
}