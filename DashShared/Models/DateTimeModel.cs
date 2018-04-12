using System;

namespace DashShared
{
    /// <summary>
    /// An implementation of FieldModel, DateTimeModel is effectively a wrapper class around Data of type DateTime. Adds custom ToString functionality. 
    /// </summary>
    [FieldModelType(TypeInfo.DateTime)]
    public class DateTimeModel : FieldModel
    {
        /*
         * The Data of type DateTime around which the class is built.
         */
        public DateTime Data;

        /*
         * Constructor receives data of type DateTime, associated with the externally accessible Data field.
         */
        public DateTimeModel(DateTime data, string id = null) : base(id)
        {
            Data = data;
        }

        /*
         * Returns a string of the following format concatenated with this instance's Data.
         */
        public override string ToString()
        {
            return $"DateTimeFieldModel: {Data}";
        }
    }
}
