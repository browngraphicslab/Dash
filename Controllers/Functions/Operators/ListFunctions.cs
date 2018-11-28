using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class ListFunctions
    {
        public static void Add(IListController list, FieldControllerBase item)
        {
            list.AddBase(item);
        }

        public static BoolController Remove(IListController list, FieldControllerBase item)
        {
            return new BoolController(list.Remove(item));
        }

        public static void Clear(IListController list)
        {
            list.Clear();
        }

        public static BoolController Contains(IListController list, FieldControllerBase item)
        {
            return new BoolController(list.ContainsBase(item));
        }

        public static void Insert(IListController list, FieldControllerBase item, NumberController index)
        {
            list.InsertBase((int)index.Data, item);
        }

        [OperatorFunctionName("remove")]
        public static BoolController DocumentRemove(DocumentController collection, FieldControllerBase item)
        {
            if (collection.GetDereferencedField(KeyStore.DataKey) is IListController list)
            {
                return new BoolController(list.Remove(item));
            }

            return new BoolController(false);
        }
    }
}
