using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public static class ContentController<T> where T: EntityBase
    {
        #region Caches

        private static ConcurrentDictionary<string, IController<T>> _controllers = new ConcurrentDictionary<string, IController<T>>();

        private static ConcurrentDictionary<string, T> _models = new ConcurrentDictionary<string, T>();


        #endregion

        static ContentController()
        {
            //var t = typeof(T);
            //Debug.Assert(t == typeof(FieldModel) || t == typeof(DocumentModel) || t == typeof(KeyModel));
        }

        #region Controllers

        /// <summary>
        /// Adds a controller to the current list of 
        /// </summary>
        /// <param name="newController"></param>
        public static void AddController(IController<T> newController)
        {
            // get the newController's id and make sure it isn't null
            var newControllerId = newController.Id;
            Debug.Assert(newControllerId != null);

            // if the newController is already saved, make sure we are not overwriting the current reference
            if (_controllers.ContainsKey(newControllerId))
            {
                var savedController = _controllers[newControllerId];
                Debug.Assert(!ReferenceEquals(savedController, newController) && savedController.Id == newController.Id,
                    "If we overwrite a reference to a saved controller bindings to the saved controller will no longer exist");
            }
            else
            {
                // otherwise add the new controller to the saved controllers
                _controllers[newControllerId] = newController;
            }
            AddModel(newController.Model);
        }

        /// <summary>
        /// Gets the requested controllers by it's id, checking to make sure that the controller is of the requested type
        /// </summary>
        public static TControllerType GetController<TControllerType>(string controllerId) where TControllerType : IController<T>
        {
            if (!_controllers.ContainsKey(controllerId)) return null;

            var controller = _controllers[controllerId];
            return controller as TControllerType;
            //Debug.Assert(false,
            //"The requested controller is not of the desired controller type and does not inhereit from the desired controller type");
            //Debug.Assert(false, "No controller exists with the passed in id");
        }

        /// <summary>
        /// Gets the requested controllers by it's id, checking to make sure that the controller is of the requested type
        /// </summary>
        public static IEnumerable<TControllerType> GetControllers<TControllerType>() where TControllerType :  IController<T>
        {
            return _controllers.Values.OfType<TControllerType>();
        }

        /// <summary>
        /// Returns the requested controller if it exists otherwise returns null
        /// </summary>
        /// <param name="controllerId"></param>
        /// <returns></returns>
        public static IController<T> GetController(string controllerId)
        {
            if (_controllers.ContainsKey(controllerId))
            {
                return _controllers[controllerId];
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
        public static IEnumerable<TControllerType> GetControllers<TControllerType>(IEnumerable<string> controllerIds) where TControllerType :  IController<T>
        {
            // convert controller id's to a list to avoid multiple enumeration
            controllerIds = controllerIds.ToList();

			/*
			foreach (var controller in _controllers)
	        {
				Debug.WriteLine("CONTROLLERS: " + _controllers);
		        Debug.WriteLine(controller);
			}
			*/
            Debug.Assert(controllerIds.All(_controllers.ContainsKey));

            // get any controllers which exist and are of type TControllerType
            var successfulControllers = controllerIds.Select(controllerId => _controllers[controllerId]);

            // TODO try and get missing controllers from the server

            Debug.Assert(controllerIds.Count() == successfulControllers.Count(), "Not all of the controllers which were passed in were of the requested type," +
                                                                               "Or the id was not found in the list of controllers");

            var typedControllers = successfulControllers.OfType<TControllerType>();

            Debug.Assert(controllerIds.Count() == typedControllers.Count(), "Not all of the controllers which were passed in were of the requested type," +
                                                                                 "Or the id was not found in the list of controllers");

            return typedControllers;
        }

        /// <summary>
        /// Checks to see if a controller with the passed in id already exists in the <see cref="ContentController"/>
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public static bool HasController(string controllerId)
        {
            return _controllers.ContainsKey(controllerId);
        }

        #endregion

        #region Models

        /// <summary>
        /// Adds a model to the current list of models
        /// </summary>
        /// <param name="newModel"></param>
        private static void AddModel(T newModel)
        {
            // get the new Model's id and make sure it isn't null
            var newModelId = newModel.Id;
            Debug.Assert(newModelId != null);

            // if the newModel is already saved, make sure we are not overwriting the current reference
            if (_models.ContainsKey(newModelId))
            {
                var savedModel = _models[newModelId];
                Debug.Assert(!ReferenceEquals(savedModel, newModel) && savedModel.Id == newModel.Id,
                    "We probably don't want to overwrite references to a saved model");
            }
            else
            {
                // otherwise add the new new Model to the saved models
                _models[newModelId] = newModel;
            }

        }

        /// <summary>
        /// Gets the requested model's by it's id, checking to make sure that the model is of the requested type
        /// </summary>
        public static TModelType GetModel<TModelType>(string modelId) where TModelType : EntityBase
        {
            if (_models.ContainsKey(modelId))
            {
                var model = _models[modelId];
                if (model is TModelType)
                {
                    return model as TModelType;
                }
                Debug.Assert(false,
                    "The requested model is not of the desired model type and does not inhereit from the desired model type");
            }
            Debug.Assert(false, "No model exists with the passed in id");
            return null;
        }

        /// <summary>
        /// Returns the requested model if it exists otherwise returns null
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public static T GetModel(string modelId)
        {
            if (_models.ContainsKey(modelId))
            {
                return _models[modelId];
            }
            Debug.Assert(false, "No model exists with the passed in id");
            return null;
        }

        /// <summary>
        /// Checks to see if a model with the passed in id already exists in the <see cref="ContentController"/>
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public static bool HasModel(string modelId)
        {
            return _models.ContainsKey(modelId);
        }

        /// <summary>
        /// to remove a contorller from this content controller
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static bool RemoveController(string Id)
        {
            _controllers.TryRemove(Id, out var outC);
            _models.TryRemove(Id, out var outM);
            return outC != null && outM != null;
        }

        /// <summary>
        /// method to delete all controllers and models.  Should really almost never be called
        /// </summary>
        public static void ClearAllControllersAndModels()
        {
            _controllers = new ConcurrentDictionary<string, IController<T>>();
            _models = new ConcurrentDictionary<string, T>();
        }

        /// <summary>
        /// Resets the content controller so that it can be reused with other databases loaded
        /// </summary>
        public static void Reset()
        {
            ClearAllControllersAndModels();
        }

        /// <summary>
        /// Gets the requested models by their ids, checking to make sure that the models are of the requested type
        /// </summary>
        /// <typeparam name="TModelType"></typeparam>
        /// <param name="modelIds"></param>
        /// <returns></returns>
        public static IEnumerable<TModelType> GetModels<TModelType>(IEnumerable<string> modelIds) where TModelType : EntityBase
        {
            // convert controller id's to a list to avoid multiple enumeration
            modelIds = modelIds.ToList();

            // get any controllers which exist and are of type TControllerType
            var succesfulModels =
                modelIds
                    .Where(modelId => _models.ContainsKey(modelId))
                    .Select(modelId => _models[modelId])
                    .OfType<TModelType>().ToList();

            // TODO try and get missing models from the server

            Debug.Assert(modelIds.Count() == succesfulModels.Count, "Not all of the models which were passed in were of the requested type," +
                                                                    "Or the id was not found in the list of models");

            return succesfulModels;
        }

        public static bool CheckAllModels()
        {
            return App.Instance.Container.GetRequiredService<IModelEndpoint<T>>()
                .CheckAllDocuments(_models.Values);
        }


        #endregion
    }
}
