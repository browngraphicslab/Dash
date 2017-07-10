using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class ListFieldModel<T> : FieldModel
    {
        public ListFieldModel(IEnumerable<T> l)
        {
            Data = new List<T>(l);
        }

        public List<T> Data;
    }
}
