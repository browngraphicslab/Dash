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
                case TypeInfo.Point:
                    controller = new PointController(model as PointModel);
                    break;
                case TypeInfo.Operator:
                    controller = MakeOperatorController(model as OperatorModel);
                    break;
                case TypeInfo.List:
                    controller = MakeListFieldController(model as ListModel);
                    break;
                case TypeInfo.Document:
                    controller = new DocumentController(model as DocumentModel, false);
                    break;
                case TypeInfo.Ink:
                    controller = new InkController(model as InkModel);
                    break;
                case TypeInfo.Number:
                    controller = new NumberController(model as NumberModel);
                    break;
                case TypeInfo.DocumentReference:
                    controller = new DocumentReferenceController(model as DocumentReferenceModel);
                    break;
                case TypeInfo.PointerReference:
                    controller = new PointerReferenceController(model as PointerReferenceModel);
                    break;
                case TypeInfo.Rectangle:
                    controller = new RectController(model as RectModel);
                    break;
                case TypeInfo.Text:
                    controller = new TextController(model as TextModel);
                    break;
                case TypeInfo.RichText:
                    controller = new RichTextController(model as RichTextModel);
                    break;
                case TypeInfo.Image:
                    controller = new ImageController(model as ImageModel);
                    break;
                case TypeInfo.Key:
                    controller = new KeyController(model as KeyModel);
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

            Debug.Assert(controller != null);

            return controller;
        }

        private static FieldControllerBase MakeListFieldController(ListModel model)
        {
            FieldControllerBase controller = null;

            switch (model.SubTypeInfo)
            {
                case TypeInfo.None:
                    Debug.Fail("this shouldnt happen????");
                    break;
                case TypeInfo.Number:
                    controller = new ListController<NumberController>(model);
                    break;
                case TypeInfo.Text:
                    controller = new ListController<TextController>(model);
                    break;
                case TypeInfo.Image:
                    controller = new ListController<ImageController>(model);
                    break;
                case TypeInfo.Document:
                    controller = new ListController<DocumentController>(model);
                    break;
                case TypeInfo.PointerReference:
                    controller = new ListController<PointerReferenceController>(model);
                    break;
                case TypeInfo.DocumentReference:
                    controller = new ListController<DocumentReferenceController>(model);
                    break;
                case TypeInfo.Operator:
                    controller = new ListController<OperatorController>(model);
                    break;
                case TypeInfo.Point:
                    controller = new ListController<PointController>(model);
                    break;
                case TypeInfo.List:
                    Debug.Fail("idk why you got here");
                    break;
                case TypeInfo.Ink:
                    controller = new ListController<InkController>(model);
                    break;
                case TypeInfo.RichText:
                    controller = new ListController<RichTextController>(model);
                    break;
                case TypeInfo.Rectangle:
                    controller = new ListController<RectController>(model);
                    break;
                case TypeInfo.Reference:
                    controller = new ListController<ReferenceController>(model);
                    break;
                case TypeInfo.Key:
                    controller = new ListController<KeyController>(model);
                    break;
                case TypeInfo.Any:
                    //Debug.Fail("idk why you got here");
                    controller = new ListController<FieldControllerBase>(model);
                    break;
                default:
                    break;
            }
            return controller;
        }

        private static OperatorController MakeOperatorController(OperatorModel model)
        {
            OperatorController controller = null;
            switch (model.Type)
            {
                case OperatorType.RichTextTitle:
                    controller = new RichTextTitleOperatorController(model);
                    break;
                case OperatorType.CollectionTitle:
                    controller = new CollectionTitleOperatorController(model);
                    break;
                case OperatorType.GroupTitle:
                    controller = new GroupTitleOperatorController(model);
                    break;
                case OperatorType.Add:
                    controller = new AddOperatorController(model);
                    break;
                case OperatorType.Zip:
                    controller = new ZipOperatorController(model);
                    break;
                //case OperatorType.CollectionMap:
                //    controller = new CollectionMapOperator(model);
                //    break;
                case OperatorType.Intersection:
                    controller = new IntersectionOperatorController(model);
                    break;
                case OperatorType.Union:
                    controller = new UnionOperatorController(model);
                    break;
                case OperatorType.Map:
                    controller = new MapOperatorController(model);
                    break;
                case OperatorType.ImageToUri:
                    controller = new ImageOperatorController(model);
                    break;
                case OperatorType.ExecDish:
                    controller = new ExecDishOperatorController(model);
                    break;
                case OperatorType.ParseSearchStringToDish:
                    controller = new ParseSearchStringToDishOperatorController(model);
                    break;
                case OperatorType.ExecuteDishToString:
                    controller = new GetScriptValueAsStringOperatorController(model);
                    break;
                case OperatorType.SimplifiedSearch:
                    controller = new SimplifiedSearchOperatorController(model);
                    break;
                case OperatorType.GetKeys:
                    controller = new GetKeysOperatorController(model);
                    break;
                case OperatorType.DocumentAppend:
                    controller = new DocumentAppendOperatorController(model);
                    break;
                case OperatorType.Concat:
                    controller = new ConcatOperatorController(model);
                    break;
                case OperatorType.Divide:
                    controller = new DivideOperatorController(model);
                    break;
                case OperatorType.Search:
                    controller = new SearchOperatorController(model);
                    break;
                case OperatorType.Api:
                    controller = new ApiOperatorController(model);
                    break;
                case OperatorType.Compound:
                    controller = new CompoundOperatorController(model as CompoundOperatorModel);
                    break;
                case OperatorType.Subtract:
                    controller = new SubtractOperatorController(model);
                    break;
                case OperatorType.Multiply:
                    controller = new MultiplyOperatorController(model);
                    break;
                case OperatorType.Regex:
                    controller = new RegexOperatorController(model);
                    break;
                case OperatorType.Melt:
                    controller = new MeltOperatorController(model);
                    break;
                case OperatorType.SentenceAnalyzer:
                    controller = new ExtractSentencesOperatorController(model);
                    break;
                case OperatorType.ExtractKeywords:
                    controller = new ExtractKeywordsOperatorController(model);
                    break;
                case OperatorType.ImageRecognition:
                    controller = new ImageToCognitiveServices(model);
                    break;
                case OperatorType.ExecuteHtmlJavaScript:
                    controller = new ExecuteHtmlJavaScriptController(model);
                    break;
                case OperatorType.ImageToColorPalette:
                    controller = new ImageToColorPalette(model);
                    break;
                case OperatorType.CollectionMap:
                    break;
                case OperatorType.Quizlet:
                    controller = new QuizletOperator(model);
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


        public static FieldControllerBase CreateDefaultFieldController(TypeInfo t, TypeInfo listType = TypeInfo.Document)
        {
            FieldControllerBase controller = null;

            switch (t)
            {
                case TypeInfo.Point:
                    controller = new PointController(0, 0);
                    break;
                case TypeInfo.Operator:
                    throw new NotImplementedException();
                case TypeInfo.List:
                    return MakeDefaultListFieldController(listType);
                case TypeInfo.Document:
                    controller = new DocumentController();
                    break;
                case TypeInfo.Ink:
                    controller = new InkController();
                    break;
                case TypeInfo.Number:
                    controller = new NumberController();
                    break;
                case TypeInfo.DocumentReference:
                case TypeInfo.PointerReference:
                    throw new NotImplementedException();
                case TypeInfo.Rectangle:
                    controller = new RectController(0, 0, 0, 0);
                    break;
                case TypeInfo.Text:
                    controller = new TextController("");
                    break;
                case TypeInfo.RichText:
                    controller = new RichTextController();
                    break;
                case TypeInfo.Image:
                    controller = new ImageController(new Uri("DEFAULT URI"));
                    break;
                case TypeInfo.None:
                case TypeInfo.Reference:
                case TypeInfo.Any:
                    throw new NotImplementedException("Shouldn't get here");
            }

            Debug.Assert(controller != null);

            return controller;
        }

        private static FieldControllerBase MakeDefaultListFieldController(TypeInfo listType)
        {
            FieldControllerBase controller = null;

            switch (listType)
            {
                case TypeInfo.None:
                    Debug.Fail("this shouldnt happen????");
                    break;
                case TypeInfo.Number:
                    controller = new ListController<NumberController>();
                    break;
                case TypeInfo.Text:
                    controller = new ListController<TextController>();
                    break;
                case TypeInfo.Image:
                    controller = new ListController<ImageController>();
                    break;
                case TypeInfo.Document:
                    controller = new ListController<DocumentController>();
                    break;
                case TypeInfo.PointerReference:
                    controller = new ListController<PointerReferenceController>();
                    break;
                case TypeInfo.DocumentReference:
                    controller = new ListController<DocumentReferenceController>();
                    break;
                case TypeInfo.Operator:
                    controller = new ListController<OperatorController>();
                    break;
                case TypeInfo.Point:
                    controller = new ListController<PointController>();
                    break;
                case TypeInfo.List:
                    Debug.Fail("idk why you got here");
                    break;
                case TypeInfo.Ink:
                    controller = new ListController<InkController>();
                    break;
                case TypeInfo.RichText:
                    controller = new ListController<RichTextController>();
                    break;
                case TypeInfo.Rectangle:
                    controller = new ListController<RectController>();
                    break;
                case TypeInfo.Reference:
                    controller = new ListController<ReferenceController>();
                    break;
                case TypeInfo.Key:
                    controller = new ListController<KeyController>();
                    break;
                case TypeInfo.Any:
                    Debug.Fail("idk why you got here");
                    break;
                default:
                    break;
            }
            return controller;
        }


    }
}
