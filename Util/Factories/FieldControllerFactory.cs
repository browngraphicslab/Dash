using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dash.Controllers;
using DashShared;
using TypeInfo = DashShared.TypeInfo;

namespace Dash
{
    public class FieldControllerFactory : BaseControllerFactory
    {
        /// <summary>
        /// Create a controller from a model based on the type of the model.
        /// Note that the controller returned from this method isn't necessarily initialized, so you must await InitializeAsync on it.
        /// For this reason, it should only really be called in database methods
        /// </summary>
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
                controller = DocumentController.CreateFromServer(model as DocumentModel);
                break;
            case TypeInfo.Ink:
                controller = new InkController(model as InkModel);
                break;
            case TypeInfo.Number:
                controller = new NumberController(model as NumberModel);
                break;
            case TypeInfo.DocumentReference:
                controller = DocumentReferenceController.CreateFromServer(model as DocumentReferenceModel);
                break;
            case TypeInfo.PointerReference:
                controller = PointerReferenceController.CreateFromServer(model as PointerReferenceModel);
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
            case TypeInfo.Html:
                controller = new HtmlController(model as HtmlModel);
                break;
            case TypeInfo.Image:
                controller = new ImageController(model as ImageModel);
                break;
            case TypeInfo.Pdf:
                controller = new PdfController(model as PdfModel);
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
            case TypeInfo.Color:
                controller = new ColorController(model as ColorModel);
                break;
            case TypeInfo.None:
                throw new Exception("Shoudlnt get here");
            case TypeInfo.Reference:
                throw new Exception("Shoudlnt get here");
            case TypeInfo.Any:
                throw new Exception("Shoudlnt get here");
            default:
                throw new ArgumentException("Parameter doesn't match any known types", nameof(model));
            }

            Debug.Assert(controller != null);

            controller.MarkFromServer();
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
                controller = ListController<NumberController>.CreateFromServer(model);
                break;
            case TypeInfo.Text:
                controller = ListController<TextController>.CreateFromServer(model);
                break;
            case TypeInfo.Image:
                controller = ListController<ImageController>.CreateFromServer(model);
                break;
            case TypeInfo.Video:
                controller = ListController<VideoController>.CreateFromServer(model);
                break;
            case TypeInfo.Audio:
                controller = ListController<AudioController>.CreateFromServer(model);
                break;
            case TypeInfo.Document:
                controller = ListController<DocumentController>.CreateFromServer(model);
                break;
            case TypeInfo.PointerReference:
                controller = ListController<PointerReferenceController>.CreateFromServer(model);
                break;
            case TypeInfo.DocumentReference:
                controller = ListController<DocumentReferenceController>.CreateFromServer(model);
                break;
            case TypeInfo.Operator:
                controller = ListController<OperatorController>.CreateFromServer(model);
                break;
            case TypeInfo.Point:
                controller = ListController<PointController>.CreateFromServer(model);
                break;
            case TypeInfo.List:
                Debug.Fail("Lists of lists are not currently supported");
                break;
            case TypeInfo.Ink:
                controller = ListController<InkController>.CreateFromServer(model);
                break;
            case TypeInfo.RichText:
                controller = ListController<RichTextController>.CreateFromServer(model);
                break;
            case TypeInfo.Rectangle:
                controller = ListController<RectController>.CreateFromServer(model);
                break;
            case TypeInfo.Reference:
                controller = ListController<ReferenceController>.CreateFromServer(model);
                break;
            case TypeInfo.Key:
                controller = ListController<KeyController>.CreateFromServer(model);
                break;
            case TypeInfo.DateTime:
                controller = ListController<DateTimeController>.CreateFromServer(model);
                break;
            case TypeInfo.Bool:
                controller = ListController<BoolController>.CreateFromServer(model);
                break;
            case TypeInfo.Color:
                controller = ListController<ColorController>.CreateFromServer(model);
                break;
            case TypeInfo.Any:
                controller = ListController<FieldControllerBase>.CreateFromServer(model);
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
            var opToBuild = OperatorTypes.First(opType => ((KeyController)opType.GetField("TypeKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).KeyModel.Equals(model.Type));
            return (OperatorController)Activator.CreateInstance(opToBuild, model);
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
            case TypeInfo.Html:
                controller = new HtmlController("");
                break;
            case TypeInfo.Pdf:
                controller = new PdfController(new Uri("DEFAULT URI"));
                break;
            case TypeInfo.Bool:
                controller = new BoolController();
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
                Debug.Fail("This shouldn't happen");
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
            case TypeInfo.Bool:
                controller = new ListController<BoolController>();
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

        private static Dictionary<Type, TypeInfo> _typeDictionary;

        static FieldControllerFactory()
        {
            _typeDictionary = new Dictionary<Type, TypeInfo>
            {
                [typeof(TextController)] = TypeInfo.Text,
                [typeof(ImageController)] = TypeInfo.Image,
                [typeof(VideoController)] = TypeInfo.Video,
                [typeof(AudioController)] = TypeInfo.Audio,
                [typeof(RichTextController)] = TypeInfo.RichText,
                [typeof(PointController)] = TypeInfo.Point,
                [typeof(PointerReferenceController)] = TypeInfo.PointerReference,
                [typeof(DocumentReferenceController)] = TypeInfo.DocumentReference,
                [typeof(ReferenceController)] = TypeInfo.Reference,
                [typeof(DocumentController)] = TypeInfo.Document,
                [typeof(FieldControllerBase)] = TypeInfo.Any,
                [typeof(RectController)] = TypeInfo.Rectangle,
                [typeof(KeyController)] = TypeInfo.Key,
                [typeof(BaseListController)] = TypeInfo.List,
                [typeof(NumberController)] = TypeInfo.Number,
                [typeof(BoolController)] = TypeInfo.Bool,
            };
        }

        public static TypeInfo GetTypeInfo<T>() where T : FieldControllerBase
        {
            var type = typeof(T);
            while (!_typeDictionary.ContainsKey(type))
            {
                type = type.BaseType;
            }

            return _typeDictionary[type];
        }


    }
}
