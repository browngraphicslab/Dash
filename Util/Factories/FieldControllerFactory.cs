using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dash.Controllers;
using DashShared;
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

            switch (type.GetFieldType())
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
                    controller = new DocumentController(model as DocumentModel);
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
				case TypeInfo.Video:
					controller = new VideoController(model as VideoModel);
					break;
                case TypeInfo.Audio:
                    controller = new AudioController(model as AudioModel);
                    break;
                case TypeInfo.Key:
                    controller = new KeyController(model as KeyModel);
                    break;
                case TypeInfo.DateTime:
                    controller = new DateTimeController(model as DateTimeModel);
                    break;
                case TypeInfo.Bool:
                    controller = new BoolController(model as BoolModel);
                    break;
                case TypeInfo.None:
                    throw new Exception("Shoudlnt get here");
                case TypeInfo.Reference:
                    throw new Exception("Shoudlnt get here");
                case TypeInfo.Any:
                    throw new Exception("Shoudlnt get here");
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
				case TypeInfo.Video:
					controller = new ListController<VideoController>(model);
					break;
                case TypeInfo.Audio:
                    controller = new ListController<AudioController>(model);
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
                case TypeInfo.DateTime:
                    controller = new ListController<DateTimeController>(model);
                    break;
                case TypeInfo.Bool:
                    controller = new ListController<BoolController>(model);
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

        private static IEnumerable<Type> OperatorTypes { get; } = typeof(OperatorController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(OperatorController)));

        private static OperatorController MakeOperatorController(OperatorModel model)
        {
            // TODO assert that op controllers have a private static field TypeKey
            // TODO use reflection to map keys to delegates for performance (google linq-expressions-creating-objects)
            var opToBuild = OperatorTypes.First(opType => ((KeyController) opType.GetField("TypeKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).KeyModel.Equals(model.Type));
            return (OperatorController) Activator.CreateInstance(opToBuild, model);
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
				case TypeInfo.Video:
					controller = new VideoController(new Uri("ms - appx://Dash/Assets/DefaultVideo.mp4"));
					break;
                case TypeInfo.Audio:
                    controller = new AudioController(new Uri("ms - appx://Dash/Assets/DefaultAudio.mp3"));
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
				case TypeInfo.Video:
					controller = new ListController<VideoController>();
					break;
                case TypeInfo.Audio:
                    controller = new ListController<AudioController>();
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
                    controller = new ListController<FieldControllerBase>();
                    //Debug.Fail("idk why you got here");
                    break;
                default:
                    break;
            }
            return controller;
        }


    }
}
