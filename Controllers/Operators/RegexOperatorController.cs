﻿using System;
using System.Collections.Generic;
using DashShared;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.regex)]
    public sealed class RegexOperatorController : OperatorController
    {
        private readonly List<string> _digits = new List<string> {"1", "2", "3", "4", "5", "6", "7", "8", "9"}; 

        public RegexOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public RegexOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Regex", new Guid("DF48D210-40A9-46A2-B32A-8F3C96C6CDD7"));

        //Input keys
        public static readonly KeyController TextKey = new KeyController("Text");
        public static readonly KeyController ExpressionKey = new KeyController("Expression");

        //Output keys
        public static readonly KeyController MatchDocsKey = new KeyController("Matches");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ExpressionKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [MatchDocsKey] = TypeInfo.Document,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            string text = (inputs[TextKey] as TextController)?.Data;
            string expr = (inputs[ExpressionKey] as TextController)?.Data;

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(expr)) return;

            var reg = new Regex($@"{expr}");
            var matches = reg.Matches(text).ToList();

            var matchDocs = new ListController<DocumentController>();
            var i = 0;

            foreach (Match match in matches)
            {
                var groups = match.Groups.ToList();
                var infoDoc = new DocumentController();
                var unnamedList = new ListController<TextController>();

                infoDoc.SetField<TextController>(KeyStore.TitleKey, $"Match #{++i}", true);
                infoDoc.SetField<NumberController>(new KeyController("Index"), match.Index, true);

                foreach (Group group in groups)
                {
                    if (string.IsNullOrEmpty(group.Value.Trim())) continue;

                    if (IsNumeric(group.Name)) unnamedList.Add(new TextController(group.Value.Trim()));
                    else
                    {
                        var key = new KeyController(group.Name);
                        if (group.Name.Equals("0")) key = new KeyController("Full Match");

                        infoDoc.SetField<TextController>(key, group.Value.Trim(), true);
                    }
                }

                if (unnamedList.Count > 0) infoDoc.SetField(KeyStore.AnonymousGroupsKey, unnamedList, true);

                matchDocs.Add(infoDoc);
            }
            outputs[MatchDocsKey] = matchDocs;
        }

        private bool IsNumeric(string name) => _digits.Contains(name[0].ToString());

        public override FieldControllerBase GetDefaultController() => new RegexOperatorController(OperatorFieldModel);
    }
}
