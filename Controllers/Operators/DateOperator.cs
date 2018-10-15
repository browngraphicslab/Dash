using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.date)]
    public class DateOperator : OperatorController
    {
        public static readonly KeyController DateStringKey = KeyController.Get("DateString");


        public static readonly KeyController DateTimeKey = KeyController.Get("DateTime");


        public DateOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public DateOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Text to Date Time", new Guid("7d871c16-c815-404a-b75b-70cdd84b7daf"));

        public override FieldControllerBase GetDefaultController()
        {
            return new DateOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DateStringKey, new IOInfo(TypeInfo.Text, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DateTimeKey] = TypeInfo.DateTime,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var dateString = (TextController)inputs[DateStringKey];
            DateTimeController dateTime = Execute(dateString);
            outputs[DateTimeKey] = dateTime;

            return Task.CompletedTask;
        }

        public DateTimeController Execute(TextController dateString)
        {
            return new DateTimeController(DateTime.Parse(dateString.Data));
        }

    }
}
