using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.rtf_title)]
    public class RichTextTitleOperatorController : OperatorController
    {
        public RichTextTitleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public RichTextTitleOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Rich Text Title", new Guid("B56DC556-7B88-495B-880B-1E3D420A1F5B"));

        //Input keys
        public static readonly KeyController RichTextKey = KeyStore.DocumentTextKey;// new KeyController(new Guid("E0105956-B0F8-4552-9420-CA7572C94657"), "Rich Text");

        //Output keys
        public static readonly KeyController ComputedTitle = new KeyController("Computed Title");


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(RichTextKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedTitle] = TypeInfo.Text,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            string computedTitle = null;

            var value = inputs[RichTextKey];
            if (value is TextController rtc)
            {
                computedTitle = rtc.Data.Split(
                    new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.None
                ).FirstOrDefault();
                var regex = new Regex("HYPERLINK \"[^\"].*\"");
                computedTitle = regex.Replace(computedTitle, "");
            }

            int maxTitleLength = 35;
            if (computedTitle?.Length > maxTitleLength)
            {
                var shortenedTitle = "";
                foreach (var word in computedTitle.Split(' '))
                {
                    if ((shortenedTitle + word).Length < maxTitleLength)
                    {
                        shortenedTitle += " " + word;
                    }
                    else
                    {
                        break;
                    }
                }
                computedTitle = shortenedTitle.Length <= 1 ? computedTitle.Substring(0, maxTitleLength) + "..." : shortenedTitle.Substring(1) + "...";
            }

            computedTitle = (computedTitle ?? "").Replace((char)160, ' ');
            outputs[ComputedTitle] = new TextController(computedTitle);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new RichTextTitleOperatorController();
        }
    }
}
