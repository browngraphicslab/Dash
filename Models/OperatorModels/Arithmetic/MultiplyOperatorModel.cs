﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /* 
    public class MultiplyOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("5E849AB0-C402-42D7-B0DD-0FE801D95092", "Input A");
        public static readonly Key BKey = new Key("D1E0F1AD-009B-4357-A045-E7661D05DC7D", "Input B");

        //Output keys
        public static readonly Key ProductKey = new Key("DED1EE8E-D0CF-4BB6-B8BA-D3393FBB8C0C", "Product");

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};

        public override List<Key> OutputKeys { get; } = new List<Key> {ProductKey};

        public override List<FieldModel> GetNewInputFields()
        {
            return new List<FieldModel>
            {
                new NumberFieldModel(), new NumberFieldModel()
            };
        }

        public override List<FieldModel> GetNewOutputFields()
        {
            return new List<FieldModel>
            {
                new NumberFieldModel()
            };
        }

        public override void Execute(DocumentModel doc)
        {
            throw new NotImplementedException();

            //NumberFieldModel numberA = doc.Field(AKey) as NumberFieldModel;
            //Debug.Assert(numberA != null, "Input is not a number");

            //NumberFieldModel numberB = doc.Field(BKey) as NumberFieldModel;
            //Debug.Assert(numberB != null, "Input is not a number");

            //double a = numberA.Data;
            //double b = numberB.Data;
            //(doc.Field(ProductKey) as NumberFieldModel).Data = a * b;
        }
    }
    */ 
}