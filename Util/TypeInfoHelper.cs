using System;
using System.Collections.Generic;
using Windows.Foundation;
using DashShared;
using DashShared.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            [typeof(DocumentController)] = TypeInfo.Document,
            [typeof(KeyController)] = TypeInfo.Key
        };

        /*
        // TODO move this to FieldModel
        public static FieldModel CreateFieldModel(FieldModel fieldModelDTO, TypeInfo listType = TypeInfo.None)
        {
           return CreateFieldModelHelper(fieldModelDTO, listType);
        }*/


        public static FieldModel CreateFieldModelHelper(TypeInfo type,object data, TypeInfo listType = TypeInfo.None)
        {
            try
            {

                switch (type)
                {
                    case TypeInfo.Text:
                        return new TextFieldModel(data.ToString());
                    case TypeInfo.Number:
                        return new NumberFieldModel(JsonConvert.DeserializeObject<double>(data.ToString()));
                    case TypeInfo.Image:
                        return new ImageFieldModel((Uri)data);
                    case TypeInfo.Collection:
                        return new DocumentCollectionFieldModel(JsonConvert.DeserializeObject<List<string>>(data.ToString()));
                    case TypeInfo.Document:
                        //TODO FIX THIS
                        throw new NotImplementedException();
                        //return new DocumentFieldModel(data.ToString());
                    case TypeInfo.DocumentReference:
                        DocumentFieldReference docFieldRefence = JsonConvert.DeserializeObject<DocumentFieldReference>(data.ToString());
                        return new DocumentReferenceFieldModel(docFieldRefence.DocumentId, docFieldRefence.FieldKey.Model.Id);
                    case TypeInfo.PointerReference:
                        throw new NotImplementedException();
                    case TypeInfo.Operator: //TODO What should this do?
                        throw new NotImplementedException();
                        //var typeAndCompound = data as Tuple<string, bool>;
                        //return new OperatorFieldModel(typeAndCompound.Item1, typeAndCompound.Item2, fieldModelDTO.Id);
                    case TypeInfo.Point:
                        return new PointFieldModel(JsonConvert.DeserializeObject<Point>(data.ToString()));
                    case TypeInfo.List:
                        return new ListFieldModel(new List<string>(), TypeInfo.Text);
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
                    case TypeInfo.None:
                        throw new NotImplementedException();
                    case TypeInfo.Ink:
                        return new InkFieldModel(data.ToString());
                    case TypeInfo.RichTextField:
                        return new RichTextFieldModel(JsonConvert.DeserializeObject<RichTextFieldModel.RTD>(data.ToString()));
                    case TypeInfo.Rectangle:
                        return new RectFieldModel(JsonConvert.DeserializeObject<Rect>(data.ToString()));
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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