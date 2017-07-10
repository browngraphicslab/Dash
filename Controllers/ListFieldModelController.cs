using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    class ListFieldModelController<T> : FieldModelController
    {
        public List<T> Data { get; set; }

        public ListFieldModelController(IEnumerable<T> list) : base(new ListFieldModel<T>(list))
        {
        }

        public override TypeInfo TypeInfo => TypeInfo.List;

        public TypeInfo ListSubTypeInfo => TypeInfoHelper.TypeToTypeInfo(typeof(T));

        public override FrameworkElement GetTableCellView()
        {
            throw new NotImplementedException();
        }
    }
}
