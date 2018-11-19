﻿using System;
using Windows.UI;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public sealed class ColorController : FieldModelController<ColorModel>
    {
        public ColorController() : this(Colors.White) { }

        public ColorController(Color data) : base(new ColorModel(data))
        {

        }

        public ColorController(ColorModel colorFieldModel) : base(colorFieldModel) { }

        public ColorModel ColorFieldModel => Model as ColorModel;

        public override FieldControllerBase GetDefaultController() => new ColorController(Colors.White);

        public override object GetValue(Context context) => Data;

        public override bool TrySetValue(object value)
        {
            if (value is Color data)
            {
                Data = data;
                return true;
            }
            return false;
        }

        public Color Data
        {
            get => ColorFieldModel.Data;
            set
            {
                if (ColorFieldModel.Data != value)
                {
                    Color data = ColorFieldModel.Data;
                    var newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    ColorFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Color;

        public override string ToString() => Data.ToString();

        public override StringSearchModel SearchForString(string searchString)
        {
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            return (Data.ToString().Contains(searchString.ToLower()) || reg.IsMatch(Data.ToString())) ? new StringSearchModel(Data.ToString()) : StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<ColorOperator>() + $"(\"{Data}\")";
        }

        public override FieldControllerBase Copy() => new ColorController(Data);
    }
}
