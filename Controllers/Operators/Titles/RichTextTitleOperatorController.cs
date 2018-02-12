using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class RichTextTitleOperatorController : OperatorController
    {
        public RichTextTitleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public RichTextTitleOperatorController() : base(new OperatorModel(OperatorType.RichTextTitle))
        {
        }

        //Input keys
        public static readonly KeyController RichTextKey = NoteDocuments.RichTextNote.RTFieldKey;// new KeyController("E0105956-B0F8-4552-9420-CA7572C94657", "Rich Text");

        //Output keys
        public static readonly KeyController ComputedTitle = new KeyController("94E01AAF-DD88-4130-9EE5-18D7B8B2674C", "Computed Title");


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [RichTextKey] = new IOInfo(TypeInfo.RichText, true),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedTitle] = TypeInfo.Text,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            string computedTitle = null;

            var value = inputs[RichTextKey];
            if (value is RichTextController rtc)
            {
                computedTitle = rtc.Data.ReadableString.Split(
                    new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.None
                ).FirstOrDefault();
            }

            outputs[ComputedTitle] = new TextController(computedTitle ?? "");
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new RichTextTitleOperatorController();
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            return this;
        }
    }
}
