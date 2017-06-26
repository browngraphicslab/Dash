using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class ContentController
    {
        private static ConcurrentDictionary<string, IController> _controllers = new ConcurrentDictionary<string, IController>();

        /// <summary>
        /// Adds a controller to the current list of 
        /// </summary>
        /// <param name="newController"></param>
        public static void AddController(IController newController)
        {
            // get the newController's id and make sure it isn't null
            var newControllerId = newController.GetId();
            Debug.Assert(newControllerId != null);

            // if the newController is already saved, make sure we are not overwriting the current reference
            if (_controllers.ContainsKey(newControllerId))
            {
                var savedController = _controllers[newControllerId];
                Debug.Assert(ReferenceEquals(savedController, newController),
                    "If we overwrite a reference to a saved controller bindings to the saved controller will no longer exist");
            }
            else
            {
                // otherwise add the new controller to the saved controllers
                _controllers[newControllerId] = newController;
            }

        }

        /// <summary>
        /// Gets the requested controllers by it's id, checking to make sure that the controller is of the requested type
        /// </summary>
        public static TControllerType GetController<TControllerType>(string controllerId) where TControllerType : class, IController
        {
            if (_controllers.ContainsKey(controllerId))
            {
                var controller = _controllers[controllerId];
                if (controller is TControllerType)
                {
                    return controller as TControllerType;
                }
                Debug.Assert(false,
                    "The requested controller is not of the desired controller type and does not inhereit from the desired controller type");
            }
            else
            {
                // TODO try and get the controller from the server
            }
            Debug.Assert(false, "No controller exists with the passed in id");
            return null;
        }

        /// <summary>
        /// Returns the requested controller if it exists otherwise returns null
        /// </summary>
        /// <param name="controllerId"></param>
        /// <returns></returns>
        public static IController GetController(string controllerId)
        {
            if (_controllers.ContainsKey(controllerId))
            {
                return _controllers[controllerId];
            }
            else
            {
                // TODO try and get the controller from the server
            }
            Debug.Assert(false, "No controller exists with the passed in id");
            return null;
        }

        /// <summary>
        /// Gets the requested controllers by their ids, checking to make sure that the controllers are of the requested type
        /// </summary>
        /// <typeparam name="TControllerType"></typeparam>
        /// <param name="controllerIds"></param>
        /// <returns></returns>
        public static IEnumerable<TControllerType> GetControllers<TControllerType>(IEnumerable<string> controllerIds) where TControllerType : class, IController
        {
            // convert controller id's to a list to avoid multiple enumeration
            controllerIds = controllerIds.ToList();

            // get any controllers which exist and are of type TControllerType
            var successfulControllers =
                controllerIds
                    .Where(controllerId => _controllers.ContainsKey(controllerId))
                    .Select(controllerId => _controllers[controllerId])
                    .OfType<TControllerType>().ToList();

            // TODO try and get missing controllers from the server

            Debug.Assert(controllerIds.Count() == successfulControllers.Count, "Not all of the controllers which were passed in were of the requested type," +
                                                                                "Or the id was not found in the list of controllers");

            return successfulControllers;
        }

        public static IEnumerable<TControllerType> GetControllersOrCreateThem<TControllerType>(IEnumerable<string> controllerIds) where TControllerType : class, IController
        {
            throw new NotImplementedException();
        }
    }
}
