using System;
using DashShared;
using DashShared.Models;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.DateTime)]
    public class DateTimeModel : FieldModel
    {
        public DateTime Data;

        public DateTimeModel(DateTime data, string id = null) : base(id)
        {
            Data = data;
        }

        public override string ToString()
        {
            return $"DateTimeFieldModel: {Data}";
        }
    }
}
