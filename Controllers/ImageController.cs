using System;
using DashShared;
using System.Diagnostics;

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
            SaveOnServer();
        }

        public ImageController(Uri path, string data = null) : base(new ImageModel(path, data))
        {
            SaveOnServer();
        }

        public ImageController(ImageModel imageFieldModel) : base(imageFieldModel)
        {
			
		}

        // == METHODS ==
        public override void Init()
        {
            // TODO: put init code here
        }

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
                    SetData(value);
                }
            }
        }

        /*
       * Sets the data property and gives UpdateOnServer an UndoCommand 
       */
        private void SetData(Uri val, bool withUndo = true)
        {
            Uri data = ImageFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            ImageFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public Uri Data
        {
            get => ImageSource;
            set => ImageSource = value;
        }

        // == OVERRIDEN FROM PARENT ==

        public override StringSearchModel SearchForString(string searchString)
        {
            var data = (Model as ImageModel)?.Data;
            if (searchString == null)
                return new StringSearchModel(data.AbsoluteUri);

            var reg = new System.Text.RegularExpressions.Regex(searchString);
            if (data != null && (data.AbsoluteUri.ToLower().Contains(searchString.ToLower()) || reg.IsMatch(data.AbsoluteUri)))
            {
                return new StringSearchModel(data.AbsoluteUri);
            }
            return StringSearchModel.False;
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
            return ImageFieldModel.Data.AbsolutePath;
        }

        public override FieldControllerBase Copy()
        {
            return new ImageController(ImageFieldModel.Data, ImageFieldModel.ByteData);
        }

        
    }
}