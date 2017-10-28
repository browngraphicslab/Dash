using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Dash.Controllers;
using Dash.Controllers.Operators;
using Dash.Controllers.Operators.Demo;
using DashShared;
using DashShared.Models;
using Flurl.Util;
using Microsoft.Extensions.DependencyInjection;
using TypeInfo = DashShared.TypeInfo;

namespace Dash
{
    public class FieldControllerFactory : BaseControllerFactory
    {
        public static FieldControllerBase CreateFromModel(FieldModel model)
        {
            Type t = model.GetType();
            System.Reflection.TypeInfo ti;

            FieldModelTypeAttribute type = null;
            do
            {
                ti = t.GetTypeInfo();
                type = ti.GetCustomAttribute<FieldModelTypeAttribute>();
                t = ti.BaseType;
            } while (type == null && t != null);

            if (type == null)
            {
                throw new NotImplementedException("The type wasn't set in the field model class");
            }

            FieldControllerBase controller = null;

            switch (type.GetType())
            {
                case TypeInfo.Collection:
                    controller = new DocumentCollectionFieldModelController(model as DocumentCollectionFieldModel);
                    break;
                case TypeInfo.Point:
                    controller = new PointFieldModelController(model as PointFieldModel);
                    break;
                case TypeInfo.Operator:
                    controller = MakeOperatorController(model as OperatorFieldModel);
                    break;
                case TypeInfo.List:
                    controller = MakeListFieldController(model as ListFieldModel);
                    break;
                case TypeInfo.Document:
                    controller = new DocumentFieldModelController(model as DocumentFieldModel);
                    break;
                case TypeInfo.Ink:
                    controller = new InkFieldModelController(model as InkFieldModel);
                    break;
                case TypeInfo.Number:
                    controller = new NumberFieldModelController(model as NumberFieldModel);
                    break;
                case TypeInfo.DocumentReference:
                    controller = new DocumentReferenceFieldController(model as DocumentReferenceFieldModel);
                    break;
                case TypeInfo.PointerReference:
                    controller = new PointerReferenceFieldController(model as PointerReferenceFieldModel);
                    break;
                case TypeInfo.Rectangle:
                    controller = new RectFieldModelController(model as RectFieldModel);
                    break;
                case TypeInfo.Text:
                    controller = new TextFieldModelController(model as TextFieldModel);
                    break;
                case TypeInfo.RichTextField:
                    controller = new RichTextFieldModelController(model as RichTextFieldModel);
                    break;
                case TypeInfo.Image:
                    controller = new ImageFieldModelController(model as ImageFieldModel);
                    break;
                case TypeInfo.None:
                    throw new Exception("Shoudlnt get here");
                    break;
                case TypeInfo.Reference:
                    throw new Exception("Shoudlnt get here");
                    break;
                case TypeInfo.Any:
                    throw new Exception("Shoudlnt get here");
                    break;
            }

            Debug.Assert(controller !=null);

            return controller;
        }

        private static FieldControllerBase MakeListFieldController(ListFieldModel model)
        {
            FieldControllerBase controller = null;

            switch (model.SubTypeInfo)
            {
                case TypeInfo.None:
                    Debug.Fail("this shouldnt happen????");
                    break;
                case TypeInfo.Number:
                    controller = new ListFieldModelController<NumberFieldModelController>(model);
                    break;
                case TypeInfo.Text:
                    controller = new ListFieldModelController<TextFieldModelController>(model);
                    break;
                case TypeInfo.Image:
                    controller = new ListFieldModelController<ImageFieldModelController>(model);
                    break;
                case TypeInfo.Collection:
                    controller = new ListFieldModelController<DocumentCollectionFieldModelController>(model);
                    break;
                case TypeInfo.Document:
                    controller = new ListFieldModelController<DocumentFieldModelController>(model);
                    break;
                case TypeInfo.PointerReference:
                    controller = new ListFieldModelController<PointerReferenceFieldController>(model);
                    break;
                case TypeInfo.DocumentReference:
                    controller = new ListFieldModelController<DocumentReferenceFieldController>(model);
                    break;
                case TypeInfo.Operator:
                    controller = new ListFieldModelController<OperatorFieldModelController>(model);
                    break;
                case TypeInfo.Point:
                    controller = new ListFieldModelController<PointFieldModelController>(model);
                    break;
                case TypeInfo.List:
                    Debug.Fail("idk why you got here");
                    break;
                case TypeInfo.Ink:
                    controller = new ListFieldModelController<InkFieldModelController>(model);
                    break;
                case TypeInfo.RichTextField:
                    controller = new ListFieldModelController<RichTextFieldModelController>(model);
                    break;
                case TypeInfo.Rectangle:
                    controller = new ListFieldModelController<RectFieldModelController>(model);
                    break;
                case TypeInfo.Reference:
                    controller = new ListFieldModelController<ReferenceFieldModelController>(model);
                    break;
                case TypeInfo.Any:
                    Debug.Fail("idk why you got here");
                    break;
                default:
                    break;
            }
            return controller;
        }

        private static OperatorFieldModelController MakeOperatorController(OperatorFieldModel model)
        {
            OperatorFieldModelController controller = null;
            switch (model.Type)
            {
                case OperatorType.Add:
                    controller = new AddOperatorFieldModelController(model);
                    break;
                case OperatorType.DBfilter:
                    controller = new DBFilterOperatorFieldModelController(model as DBFilterOperatorFieldModel);
                    break;
                case OperatorType.Zip:
                    controller = new ZipOperatorFieldController(model);
                    break;
                case OperatorType.Filter:
                    controller = new FilterOperatorFieldModelController(model);
                    break;
                case OperatorType.CollectionMap:
                    controller = new CollectionMapOperator(model);
                    break;
                case OperatorType.Intersection:
                    controller = new IntersectionOperatorModelController(model);
                    break;
                case OperatorType.Union:
                    controller = new UnionOperatorFieldModelController(model);
                    break;
                case OperatorType.Map:
                    controller = new MapOperatorController(model);
                    break;
                case OperatorType.ImageToUri:
                    controller = new ImageOperatorFieldModelController(model);
                    break;
                case OperatorType.DocumentAppend:
                    controller = new DocumentAppendOperatorController(model);
                    break;
                case OperatorType.Concat:
                    controller = new ConcatOperator(model);
                    break;
                case OperatorType.Divide:
                    controller = new DivideOperatorFieldModelController(model);
                    break;
                case OperatorType.Search:
                    controller = new DBSearchOperatorFieldModelController(model as DBSearchOperatorFieldModel);
                    break;
                case OperatorType.Api:
                    controller = new ApiOperatorController(model);
                    break;
                case OperatorType.Compound:
                    controller = new CompoundOperatorFieldController(model as CompoundOperatorFieldModel);
                    break;
                case OperatorType.Subtract:
                    controller = new SubtractOperatorFieldModelController(model);
                    break;
                case OperatorType.Multiply:
                    controller = new MultiplyOperatorFieldModelController(model);
                    break;
                case OperatorType.Regex:
                    controller = new RegexOperatorFieldModelController(model);
                    break;
                case OperatorType.Melt:
                    controller = new MeltOperatorFieldModelController(model);
                    break;
                case OperatorType.ExtractSentences:
                    controller = new ExtractSentencesOperatorFieldModelController(model);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return controller;
        }

        public static FieldModelController<T> CreateTypedFromModel<T>(T model) where T : FieldModel
        {
            return CreateFromModel(model) as FieldModelController<T>;
        }
    }
}
