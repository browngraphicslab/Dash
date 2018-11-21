using System;
using DashShared;
using System.Diagnostics;
using Dash.Controllers.Operators;

namespace Dash
{
    /// <summary>
    /// Controls data represeting an image in a Document.
    /// </summary>
    public class ImageController : FieldModelController<ImageModel>
    {
        // == CONSTRUCTORS ==
        public ImageController() : base(new ImageModel())
        {
        }

        public ImageController(Uri path, string data = null) : base(new ImageModel(path, data))
        {
        }

        public ImageController(ImageModel imageFieldModel) : base(imageFieldModel)
        {

        }

        // == METHODS ==

        /// <summary>
        ///     The <see cref="ImageFieldModel" /> associated with this <see cref="ImageController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ImageModel ImageFieldModel => Model as ImageModel;

        /// <summary>
        ///     The uri which this image is sourced from. This is a wrapper for <see cref="ImageModel.Data" />
        /// </summary>
        public Uri ImageSource
        {
            get => ImageFieldModel.Data;
            set
            {
                if (ImageFieldModel.Data != value)
                {
                    Uri data = ImageFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    ImageFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public Uri Data
        {
            get => ImageSource;
            set => ImageSource = value;
        }

        // == OVERRIDEN FROM PARENT ==

        public override StringSearchModel SearchForString(string searchString, Search.SearchOptions options)
        {
            var data = (Model as ImageModel)?.Data;
            if (searchString == null)
                return new StringSearchModel(data.AbsoluteUri);

            return options.Matches(data.AbsoluteUri);

            var reg = new System.Text.RegularExpressions.Regex(searchString);
            if (data != null && (data.AbsoluteUri.ToLower().Contains(searchString.ToLower()) || reg.IsMatch(data.AbsoluteUri)))
            {
                return new StringSearchModel(data.AbsoluteUri);
            }
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<ImageOperator>() + $"(\"{Data}\")";
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ImageController(new Uri("ms-appx:///Assets/DefaultImage.png"));
        }

        public override object GetValue(Context context)
        {
            return Data;
        }

        public override bool TrySetValue(object value)
        {
            Debug.Assert(value is Uri);
            if (value is Uri u)
            {
                Data = u;
                return true;
            }
            return false;
        }

        public override TypeInfo TypeInfo => TypeInfo.Image;

        public override string ToString()
        {
            return ImageFieldModel.Data.AbsoluteUri;
        }

        public override FieldControllerBase Copy()
        {
            return new ImageController(ImageFieldModel.Data, ImageFieldModel.ByteData);
        }


    }
}
