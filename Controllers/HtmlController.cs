using DashShared;

namespace Dash
{
    class HtmlController : FieldModelController<HtmlModel>
    {
        public HtmlController() : this("")
        {
        }

        public HtmlController(string data) : base(new HtmlModel(data))
        {
        }

        public HtmlController(HtmlModel htmlFieldModel) : base(htmlFieldModel)
        {
        }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public HtmlModel HtmlFieldModel => Model as HtmlModel;

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool TrySetValue(object value)
        {
            if (value is string)
            {
                Data = value as string;
                return true;
            }
            return false;
        }
        public string Data
        {
            get { return HtmlFieldModel.Data; }
            set
            {
                if (HtmlFieldModel.Data != value)
                {
                    SetData(value);
                }
            }
        }

        /*
        * Sets the data property and gives UpdateOnServer an UndoCommand 
        */
        private void SetData(string val, bool withUndo = true)
        {
            string data = HtmlFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            HtmlFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public override TypeInfo TypeInfo => TypeInfo.Html;

        public override FieldControllerBase GetDefaultController()
        {
            return new HtmlController("");
        }

        public override string ToString()
        {
            return Data;
        }

        public override StringSearchModel SearchForString(string searchString, Search.SearchOptions options)
        {
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "HtmlController";
        }

        public override FieldControllerBase Copy()
        {
            return new HtmlController(Data);
        }
    }
}
