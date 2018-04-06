using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers
{
    public class DateTimeController : FieldModelController<DateTimeModel>
    {
        public DateTimeModel DateTimeFieldModel => Model as DateTimeModel;

        public override TypeInfo TypeInfo => TypeInfo.DateTime;

        public DateTimeController() : this(DateTime.Now.Date)
        {
            
        }

        public DateTimeController(DateTime data = new DateTime()) : base(new DateTimeModel(data))
        {
            
        }

        public DateTimeController(DateTimeModel dateTimeFieldModel) : base(dateTimeFieldModel)
        {

        }

        public override bool TrySetValue(object value)
        {
            var data = value as DateTime?;
            if (value is DateTime)
            {
                if (value.Equals(new DateTime()))
                {
                    return false;
                }
                Data = data.Value;
                return true;
            }
            return false;
        }

        public override object GetValue(Context context)
        {
            return Data;
        }

        public void AddHours(double hours)
        {
            Data = Data.AddHours(hours);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new DateTimeController(DateTime.Now.Date);
        }

        public override void Init()
        {

        }

        public DateTime Data
        {
            get => DateTimeFieldModel.Data;
            set
            {
                if (!value.Equals(DateTimeFieldModel.Data))
                {
                    DateTimeFieldModel.Data = value;
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return Data.ToString().Contains(searchString) ? new StringSearchModel(Data.ToString()) : StringSearchModel.False;
        }

        public override FieldModelController<DateTimeModel> Copy()
        {
            return new DateTimeController(Data);
        }
    }
}
