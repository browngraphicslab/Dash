﻿using DashShared;
using System;
using Dash.Controllers.Operators;

namespace Dash
{
    /// <summary>
    /// An implementation of FieldModelController, DateTimeController models a controller that stores Data of type DateTime
    /// Used in updating the modified-time field of a document. 
    /// </summary>
    public sealed class DateTimeController : FieldModelController<DateTimeModel>
    {
        public DateTimeModel DateTimeFieldModel => Model as DateTimeModel;


        public override TypeInfo TypeInfo => TypeInfo.DateTime;
        
        //CONSTRUCTORS

        /*
         * Default parameterless constructor sets Data to the current local time. 
         */
        public DateTimeController() : this(DateTime.Now) { }

        /*
         * Primary constructor recieves data of type DateTime and uses it to construct a new DateTimeModel. Default value is set to 1/1/0001 0:00:00.
         */
        public DateTimeController(DateTime data = new DateTime()) : base(new DateTimeModel(data)) { }

        /*
         * Constructor that receives only an instance of DateTimeModel
         */
        public DateTimeController(DateTimeModel dateTimeFieldModel) : base(dateTimeFieldModel) { }
        
        //END CONSTRUCTORS

        /*
         * Effectively a conditional mutator for the instance's Data field, where 'value' must be of type DateTime 
         */
        public override bool TrySetValue(object value)
        {
            var data = value as DateTime?;
            if (value is DateTime)
            {
                /*
                 * Following 'if' statement called only if the user inputs an invalid time when editing the note extension of modified-time
                 * If this occurs, a new DateTime (1/1/0001 0:00:00), rather than the inputted date time, is returned from the converter
                 * Gets caught in the following if statement and returns false, restoring the time to its last-updated value.
                 */
                if (value.Equals(new DateTime()))
                {
                    return false;
                }
                Data = data.Value;
                return true;
            }
            return false;
        }

        /*
         * Given a context, returns the controller's Data field, of type DateTime
         */
        public override object GetValue() => Data;

        /*
         * Returns a new instance of DateTimeController initialized with the default constructor
         */
        public override FieldControllerBase GetDefaultController() => new DateTimeController();

        /*
         * Returns a copy of this instance of DateTimeController (Data field is preserved)
         */
        public override FieldControllerBase Copy() => new DateTimeController(Data);

        /*
         * Accessor/mutator for the controller's Data field, of type DateTime
         */
        public DateTime Data
        {
            get => DateTimeFieldModel.Data;
            //DateTimeController.Set less commonly referenced. More of an overloaded setter. Instead, see DateTimeController.TrySetValue(object value)
            set
            {
                if (DateTimeFieldModel.Data != value)
                {
            DateTime data = DateTimeFieldModel.Data;
            var newEvent = new UndoCommand(() => Data = value, () => Data = data);

            DateTimeFieldModel.Data = value;
            UpdateOnServer(newEvent);
            OnFieldModelUpdated(null);
                }
            }
        }

        public override string ToString() => DateTimeFieldModel.Data.ToString("G");

        /*
         * Returns a StringSearchModel based on the text query submitted and the contents of this instance's Data (DateTime)
         */
        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            return matcher.Matches(Data.ToString("G"));
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<DateOperator>() + $"(\"{Data}\")";
        }
    }
}
