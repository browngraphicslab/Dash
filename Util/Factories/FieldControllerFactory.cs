using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Dash.Controllers;
using Dash.Controllers.Operators;
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
            }

            return controller;
        }

        private static FieldControllerBase MakeListFieldController(ListFieldModel model)
        {

            //TODO
            FieldControllerBase controller = null;
            switch (model.SubTypeInfo)
            {
                case TypeInfo.Collection:
                    controller = new ListFieldModelController<DocumentCollectionFieldModelController>();
                    break;
            }

            return controller;
        }

        private static OperatorFieldModelController MakeOperatorController(OperatorFieldModel model)
        {
            //TODO
            OperatorFieldModelController controller = null;
            switch (model.Type)
            {
                case OperatorType.Add:
                    controller = new AddOperatorModelController(model);
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
