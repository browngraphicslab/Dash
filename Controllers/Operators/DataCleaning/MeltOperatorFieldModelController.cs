using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dash.Models;
using Dash.StaticClasses;
using DashShared;

namespace Dash
{
    public class MeltOperatorFieldModelController : OperatorFieldModelController
    {
        //Input Keys
        public static readonly KeyController InputCollection =
            new KeyController("DCED5F30-1C96-4BD9-8DE9-6C9DDC37C239", "Input Collection");

        public static readonly KeyController ColumnVariables =
            new KeyController("AA00C8E9-EEAB-44BC-97C1-BF82FEA1B428", "Column Variables");

        public static readonly KeyController VariableName =
            new KeyController("02D82E59-54C3-4BCD-B45B-78416FE08163", "Variable Name");

        public static readonly KeyController ValueName =
            new KeyController("A2AD0B50-7CC9-48B4-9D68-CA17AA3DC741", "Value Name");

        //Output Keys
        public static readonly KeyController OutputCollection =
            new KeyController("4ECAF1CB-5FEF-4B6D-8A84-C134BD90C750", "Output Collection");


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } =
            new ObservableDictionary<KeyController, IOInfo>()
            {
                [InputCollection] = new IOInfo(TypeInfo.Collection, true),
                [ColumnVariables] = new IOInfo(TypeInfo.List, true),
                [VariableName] = new IOInfo(TypeInfo.Text, true),
                [ValueName] = new IOInfo(TypeInfo.Text, true),

            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputCollection] = TypeInfo.Collection
            };

        public MeltOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.Melt))
        {
        }

        public MeltOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var docs = (inputs[InputCollection] as DocumentCollectionFieldModelController)?.GetDocuments();
            Debug.Assert(docs != null);
        }

        public override bool SetValue(object value)
        {
            return false;
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new MeltOperatorFieldModelController();
        }
    }
}