using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public interface IListController
    {
        TypeInfo ListSubTypeInfo { get; }

        bool Remove(FieldControllerBase fmc);
        void AddBase(FieldControllerBase fmc);
        void AddRange(IEnumerable<FieldControllerBase> fmcs);
        void Clear();
        void Insert(int index, FieldControllerBase element);

        void Set(IEnumerable<FieldControllerBase> fmcs);

        void SetValue(int index, FieldControllerBase field);
        FieldControllerBase GetValue(int index);

        int Count { get; }

        FieldControllerBase AsField();
        IEnumerable<FieldControllerBase> AsEnumerable();
    }
}
