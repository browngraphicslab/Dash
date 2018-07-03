﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Flurl.Util;
using Syncfusion.Drawing;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    class ObjectExpression : ScriptExpression
    {
        private readonly Dictionary<string, ScriptExpression> _dictionary;

        private List<KeyController> _docKeys;

        public ObjectExpression(Dictionary<string, ScriptExpression> sc)
        {
            _dictionary = sc;
        }

        public override TypeInfo Type => TypeInfo.Any;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new System.NotImplementedException();
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            DocumentController doc;
            if (_dictionary.ContainsKey(KeyStore.DataKey.Name))
            {
                FieldControllerBase dataVal = _dictionary[KeyStore.DataKey.Name].Execute(scope);

                switch (dataVal.TypeInfo)
                {
                    case TypeInfo.Text:
                        doc = new TextingBox(dataVal).Document;
                        break;
                    case TypeInfo.Image:
                        doc = new ImageBox(dataVal).Document;
                        break;
                    default:
                        doc = new DocumentController();
                        break;
                }
                
            }
            else
            {
                doc = new DocumentController();
            }

            foreach (var scriptExpression in _dictionary)
            {
                var keyString = scriptExpression.Key;
                if (keyString == KeyStore.DataKey.Name) continue;
                var key = KeyController.LookupKeyByName(keyString, true);
                var value = scriptExpression.Value.Execute(scope);
                doc.SetField(key, value, true);
            }

            return doc;
        }
    }
}
