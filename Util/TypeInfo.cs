using DashShared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{

    public class TypeInfoHelper
    {
        /// <summary>
        /// Creates a FieldModelController from a given DTO.
        /// </summary>
        /// <param name="fieldModelDTO"></param>
        /// <param name="listType"></param>
        /// <returns></returns>
        public static FieldModelController CreateFieldModelController(FieldModelDTO fieldModelDTO, TypeInfo listType = TypeInfo.None)
        {
            var x = CreateFieldModelControllerHelper(fieldModelDTO, listType);
            x.FieldModel.Id = fieldModelDTO.Id;
            return x;
        }

        /// <summary>
        /// Creates an empty FieldModelController of a given type. Generally, this is not very useful.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="listType"></param>
        /// <returns></returns>
        public static FieldModelController CreateFieldModelController(TypeInfo t, TypeInfo listType = TypeInfo.None)
        {
            switch (t)
            {
                case TypeInfo.Text:
                    return new TextFieldModelController("");
                case TypeInfo.Number:
                    return new NumberFieldModelController();
                case TypeInfo.Image:
                    return new ImageFieldModelController();
                case TypeInfo.Collection:
                    return new DocumentCollectionFieldModelController(new List<DocumentController>());
                case TypeInfo.Document:
                    return new DocumentFieldModelController(null);
                case TypeInfo.Reference:
                    return new ReferenceFieldModelController("", null);
                case TypeInfo.Operator://TODO What should this do?
                    return null;
                case TypeInfo.Point:
                    return new PointFieldModelController(0, 0);
                case TypeInfo.List:
                    switch (listType)//TODO support list of list?
                    {
                        case TypeInfo.Number:
                            return new ListFieldModelController<NumberFieldModelController>();
                        case TypeInfo.Image:
                            return new ListFieldModelController<ImageFieldModelController>();
                        case TypeInfo.Document:
                            return new ListFieldModelController<DocumentFieldModelController>();
                        case TypeInfo.Point:
                            return new ListFieldModelController<PointFieldModelController>();
                        case TypeInfo.Text:
                            return new ListFieldModelController<TextFieldModelController>();
                        case TypeInfo.Reference:
                            return new ListFieldModelController<ReferenceFieldModelController>();
                        case TypeInfo.Collection:
                            return new ListFieldModelController<DocumentCollectionFieldModelController>();
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        // TODO: DocumentFieldModelController is broken (DTO's data cant be a controller, need to make a controller from data first)
        //       the DTO for TypeInfo.Document is weird--is a Document Field Model a Document Model or a Field as far as storing goes?
        public static FieldModelController CreateFieldModelControllerHelper(FieldModelDTO fieldModelDTO, TypeInfo listType = TypeInfo.None)
        {
            var data = fieldModelDTO.Data;
            switch (fieldModelDTO.Type)
            {
                case TypeInfo.Text:
                    return new TextFieldModelController(data as string);
                case TypeInfo.Number:
                    return new NumberFieldModelController((double)data);
                case TypeInfo.Image:
                    return new ImageFieldModelController(data as Uri);
                case TypeInfo.Collection:
                    return new DocumentCollectionFieldModelController(data as List<DocumentController>);
                case TypeInfo.Document:
                    throw new NotImplementedException();
                case TypeInfo.Reference:
                    throw new NotImplementedException();
                case TypeInfo.Operator://TODO What should this do?
                    return null;
                case TypeInfo.Point:
                    return new PointFieldModelController((Point)data);
                case TypeInfo.List:
                    switch (listType)//TODO support list of list?
                    {
                        case TypeInfo.Number:
                            return new ListFieldModelController<NumberFieldModelController>(data as IEnumerable<NumberFieldModelController>);
                        case TypeInfo.Image:
                            return new ListFieldModelController<ImageFieldModelController>(data as IEnumerable<ImageFieldModelController>);
                        case TypeInfo.Document:
                            return new ListFieldModelController<DocumentFieldModelController>(data as IEnumerable<DocumentFieldModelController>);
                        case TypeInfo.Point:
                            return new ListFieldModelController<PointFieldModelController>(data as IEnumerable<PointFieldModelController>);
                        case TypeInfo.Text:
                            return new ListFieldModelController<TextFieldModelController>(data as IEnumerable<TextFieldModelController>);
                        case TypeInfo.Reference:
                            return new ListFieldModelController<ReferenceFieldModelController>(data as IEnumerable<ReferenceFieldModelController>);
                        case TypeInfo.Collection:
                            return new ListFieldModelController<DocumentCollectionFieldModelController>(data as IEnumerable<DocumentCollectionFieldModelController>);
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        private static readonly Dictionary<Type, TypeInfo> TypeDict = new Dictionary<Type, TypeInfo>
        {
            [typeof(NumberFieldModelController)] = TypeInfo.Number,
            [typeof(TextFieldModelController)] = TypeInfo.Text,
            [typeof(PointFieldModelController)] = TypeInfo.Point,
            [typeof(ListFieldModelController<>)] = TypeInfo.List,
            [typeof(DocumentCollectionFieldModelController)] = TypeInfo.Collection,
            [typeof(DocumentFieldModelController)] = TypeInfo.Document
        };

        public static TypeInfo TypeToTypeInfo(Type type)
        {
            if (TypeDict.ContainsKey(type))
                return TypeDict[type];

            return TypeInfo.None;
        }
    }
}
