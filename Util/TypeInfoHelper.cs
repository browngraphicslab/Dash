using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Dash.Controllers;
using DashShared;
using Newtonsoft.Json;

namespace Dash
{
    public class TypeInfoHelper
    {
        private static readonly Dictionary<Type, TypeInfo> TypeDict = new Dictionary<Type, TypeInfo>
        {
            [typeof(NumberController)] = TypeInfo.Number,
            [typeof(TextController)] = TypeInfo.Text,
            [typeof(PointController)] = TypeInfo.Point,
            [typeof(ListController<>)] = TypeInfo.List,
            [typeof(DocumentController)] = TypeInfo.Document,
            [typeof(KeyController)] = TypeInfo.Key,
            [typeof(OperatorController)] = TypeInfo.Operator,
            [typeof(VideoController)] = TypeInfo.Video,
            [typeof(AudioController)] = TypeInfo.Audio,
            [typeof(BoolController)] = TypeInfo.Bool,
            [typeof(ColorController)] = TypeInfo.Color,
            [typeof(RectController)] = TypeInfo.Rectangle,
            [typeof(DateTimeController)] = TypeInfo.DateTime,
            [typeof(DocumentReferenceController)] = TypeInfo.DocumentReference,

            [typeof(FieldControllerBase)] = TypeInfo.Any
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
                        return new TextModel(data.ToString());
                    case TypeInfo.Number:
                        return new NumberModel(JsonConvert.DeserializeObject<double>(data.ToString()));
                    case TypeInfo.Image:
                        return new ImageModel((Uri)data);
                    //TODO tfs: fix this
                    //case TypeInfo.Collection:
                    //    return new DocumentCollectionFieldModel(JsonConvert.DeserializeObject<List<string>>(data.ToString()));
                    case TypeInfo.Document:
                        //TODO tfs: FIX THIS
                        throw new NotImplementedException();
                        //return new DocumentFieldModel(data.ToString());
                    case TypeInfo.DocumentReference:
                        DocumentFieldReference docFieldRefence = JsonConvert.DeserializeObject<DocumentFieldReference>(data.ToString());
                        return new DocumentReferenceModel(docFieldRefence.DocumentController.Id, docFieldRefence.FieldKey.Id, false);
                    case TypeInfo.PointerReference:
                        throw new NotImplementedException();
                    case TypeInfo.Operator: //TODO What should this do?
                        throw new NotImplementedException();
                        //var typeAndCompound = data as Tuple<string, bool>;
                        //return new OperatorFieldModel(typeAndCompound.Item1, typeAndCompound.Item2, fieldModelDTO.Id);
                    case TypeInfo.Point:
                        return new PointModel(JsonConvert.DeserializeObject<Point>(data.ToString()));
                    case TypeInfo.List:
                        return new ListModel(new List<string>(), TypeInfo.Text);
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
                    //        return new ListFieldModelController<ListController<DocumentController>>(
                    //            data as IEnumerable<ListController<DocumentController>>);
                    //    default:
                    //        return null;
                    //}
                    case TypeInfo.None:
                        throw new NotImplementedException();
                    case TypeInfo.Ink:
                        return new InkModel(data.ToString());
                    case TypeInfo.RichText:
                        return new RichTextModel(JsonConvert.DeserializeObject<RichTextModel.RTD>(data.ToString()));
                    case TypeInfo.Rectangle:
                        return new RectModel(JsonConvert.DeserializeObject<Rect>(data.ToString()));
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
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
