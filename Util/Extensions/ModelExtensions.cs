using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public static class ModelExtensions
    {
        public static DocumentController NewController(this DocumentModel model)
        {
            return DocumentControllerFactory.CreateFromModel(model);
        }

        public static FieldControllerBase NewController(this FieldModel model)
        {
            return FieldControllerFactory.CreateFromModel(model);
        }

        public static KeyController NewController(this KeyModel model)
        {
            return KeyControllerFactory.CreateFromModel(model);
        }
    }
}
