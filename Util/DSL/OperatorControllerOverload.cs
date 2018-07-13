using System;
using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class OperatorControllerOverload
    {
        public List<KeyValuePair<KeyController, IOInfo>> ParamTypes { get; }
        public Type OperatorType { get; }

        public OperatorControllerOverload(List<KeyValuePair<KeyController, IOInfo>> paramTypes, Type operatorType)
        {
            ParamTypes = paramTypes;
            OperatorType = operatorType;
        }

        public List<int> GetDistances(List<FieldControllerBase> checkAgainst)
        {
            if (checkAgainst.Count > ParamTypes.Count) return null;
            var distances = new List<int>(checkAgainst.Count);
            for (var i = 0; i < checkAgainst.Count; i++)
            {
                var checkType = checkAgainst[i]?.TypeInfo;
                var thisType = ParamTypes[i].Value.Type;

                if (checkType == thisType)
                {
                    distances.Add(0);
                }
                else if (checkType != null && thisType.HasFlag(checkType))
                {
                    distances.Add(1);
                }
                else
                {
                    return null;
                }
            }

            for (var i = checkAgainst.Count; i < ParamTypes.Count; ++i)
            {
                if (ParamTypes[i].Value.IsRequired)
                {
                    return null;
                }
            } 

            return distances;
        }
    }
}