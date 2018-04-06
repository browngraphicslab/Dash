using System;
using DashShared;

namespace Dash
{
    public class BaseControllerFactory
    {
        /*
        private static Dictionary<Type,Type> _dict = new Dictionary<Type, Type>()
        {
            {typeof(DocumentModel), typeof(DocumentController) }
        };
        */
        public static ControllerT CreateFromModel<ControllerT>(EntityBase model)
        {
            /*
            //List<Type> derivedClassList = typeof(IController<>).GetTypeInfo().Assembly.GetTypes().Where(type => type.IsInstanceOfType(typeof(IController<>))).ToList();
            var  derivedClassList = typeof(IController<>).GetTypeInfo().Assembly.GetTypes().ToList();

            var k = typeof(IControllerBase).GetTypeInfo().Assembly.GetTypes() .Where(type => typeof(IControllerBase).IsAssignableFrom(type)).ToList();

            var a = k[6];

            //var b = FindDerivedTypes(typeof(IController<>).GetTypeInfo().Assembly, typeof(IController<>));
            */

            //var val = _dict[typeof(ModelT)];
            // return (IController<ModelT>)Activator.CreateInstance(val, new object[]{ model });

            var controller = (ControllerT)Activator.CreateInstance(typeof(ControllerT), new object[] { model });
            return controller;
        }

    }
}
