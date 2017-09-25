using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;
using Flurl.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class FieldControllerFactory : BaseControllerFactory
    {
        public static FieldControllerBase CreateFromModel(FieldModel model)
        {
            var t = model.GetType();

            var type = t.GetTypeInfo().GetCustomAttribute<FieldModelType>();

            if (type == null)
            {
                throw new NotImplementedException("The type wasn't set if the field model class");
            }

            FieldControllerBase controller = null;

            switch (type.GetType())
            {
                case FieldTypeEnum.DocumentCollection:
                    controller = new DocumentCollectionFieldModelController(model as DocumentCollectionFieldModel);
                    break;
                case FieldTypeEnum.Point:
                    controller = new PointFieldModelController(model as PointFieldModel);
                    break;
            }

            return controller;
        }

        public static FieldModelController<T> CreateTypedFromModel<T>(T model) where T:FieldModel
        {
            return CreateFromModel(model) as FieldModelController<T>;
        }
    }
}
