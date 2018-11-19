using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class ListFunctions
    {
        public static BoolController Remove(IListController list, FieldControllerBase item)
        {
            return new BoolController(list.Remove(item));
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
