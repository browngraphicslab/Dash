using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;

namespace Dash
{
    public static class ContentController
    {
        #region Caches

        private static ConcurrentDictionary<string, IController> _controllers = new ConcurrentDictionary<string, IController>();

        private static ConcurrentDictionary<string, EntityBase> _models = new ConcurrentDictionary<string, EntityBase>();


        #endregion

        #region Controllers

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
        public static void AddModel(EntityBase newModel)
        {
            // get the new Model's id and make sure it isn't null
            var newModelId = newModel.Id;
            Debug.Assert(newModelId != null);

            // if the newModel is already saved, make sure we are not overwriting the current reference
            if (_models.ContainsKey(newModelId))
            {
                var savedModel = _models[newModelId];
                Debug.Assert(ReferenceEquals(savedModel, newModel),
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
            else
            {
                // TODO try and get the controller from the server
            }
            Debug.Assert(false, "No model exists with the passed in id");
            return null;
        }

        /// <summary>
        /// Returns the requested model if it exists otherwise returns null
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public static EntityBase GetModel(string modelId)
        {
            if (_models.ContainsKey(modelId))
            {
                return _models[modelId];
            }
            else
            {
                // TODO try and get the model from the server
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

        #endregion

        #region FieldModels

        /// <summary>
        /// Follows a <see cref="ReferenceFieldModel"/> to the first field that it references, and returns that reference as the passed in type. On error
        /// this returns null
        /// </summary>
        public static TFieldModelType DereferenceFieldModel<TFieldModelType>(ReferenceFieldModel reference, List<DocumentController> contextList = null) where TFieldModelType : FieldModel
        {
            var firstReference = DereferenceFieldModel(reference, contextList = null);
            Debug.Assert(firstReference != null, "The passed in reference does not exist");
            var typeSafeFirstReference = firstReference as TFieldModelType;
            Debug.Assert(typeSafeFirstReference != null, "The passed in reference is not of the passed in type");
            return typeSafeFirstReference;
        }

        /// <summary>
        /// Follows a <see cref="ReferenceFieldModel"/> to the first field that it references, and returns that reference as the passed in type. On error
        /// this returns null
        /// </summary>
        public static FieldModel DereferenceFieldModel(ReferenceFieldModel reference, List<DocumentController> contextList = null)
        {
            Debug.Assert(reference != null);
            DocumentModel docModel = null;
            string fieldModelId = null;
            FieldModel fieldModel = null;
            string refDocId = MapDocumentInstanceReference(reference.DocId, contextList);

            // check if the document exists
            if (_models.ContainsKey(refDocId))
            {
                // get the document model
                docModel = _models[refDocId] as DocumentModel;
                Debug.Assert(docModel != null, "The Document Model referenced by your ReferenceFieldModel does not exist in the local cache.");

                // get the field model id from the document model
                fieldModelId = GetController<DocumentController>(refDocId).GetField(reference.FieldKey).GetId();// docModel.Fields[reference.FieldKey];
            }
            else
            {
                // TODO try and get the document model from the server
            }

            Debug.Assert(docModel != null, "The Document Model referenced by your ReferenceFieldModel does not exist in the local cache or on the server.");

            // check if the field model exists
            if (_models.ContainsKey(fieldModelId))
            {
                // get the field model from the local cache
                fieldModel = _models[fieldModelId] as FieldModel;

                Debug.Assert(fieldModel != null, "The field Model referenced by your ReferenceFieldModel does not exist in the local cache");

            }
            else
            {
                // TODO try and get the field model from the server
            }

            Debug.Assert(fieldModel != null, "The field Model referenced by your ReferenceFieldModel does not exist in the local cache or on the server.");

            return fieldModel;
        }

        public static string MapDocumentInstanceReference(string referenceDocId, List<DocumentController> contextList)
        {
            if (contextList != null)
                foreach (var doc in contextList)
                    if (doc.IsDelegateOf(referenceDocId))
                        referenceDocId = doc.GetId();
            return referenceDocId;
        }


        /// <summary>
        /// Follows a <see cref="ReferenceFieldModel"/> or chain of <see cref="ReferenceFieldModel"/> to the "root" item, which is a <see cref="FieldModel"/>
        /// </summary>
        public static FieldModel DereferenceToRootFieldModel(FieldModel reference, List<DocumentController> contextList = null)
        {
            Debug.Assert(reference != null);
            FieldModel possibleFieldModel = reference;
            while (possibleFieldModel is ReferenceFieldModel)
            {
                possibleFieldModel = DereferenceFieldModel(possibleFieldModel as ReferenceFieldModel,  contextList);
            }

            Debug.Assert(possibleFieldModel != null, "The chain of references ended in a null field");

            return possibleFieldModel;
        }

        /// <summary>
        /// Follows a <see cref="ReferenceFieldModel"/> or chain of <see cref="ReferenceFieldModel"/> to the "root" item, which is a <see cref="FieldModel"/>
        /// </summary>
        public static TFieldModelType DereferenceToRootFieldModel<TFieldModelType>(FieldModel reference) where TFieldModelType : FieldModel
        {
            var possibleFieldModel = DereferenceToRootFieldModel(reference);

            Debug.Assert(possibleFieldModel != null, "The chain of references ended in a null field");
            Debug.Assert(possibleFieldModel is TFieldModelType, "The chain of references ends in a field model which is not of the desired type");
            return possibleFieldModel as TFieldModelType;
        }

        /// <summary>
        /// Follows a <see cref="FieldModelController"/>/<see cref="FieldModelController"/> or chain of <see cref="ReferenceFieldModelController"/> to the "root" item, which is a <see cref="FieldModelController"/>
        /// </summary>
        public static FieldModelController DereferenceToRootFieldModel(FieldModelController reference, List<DocumentController> contextList = null)
        {
            var dereferencedFieldModel = DereferenceToRootFieldModel(reference.FieldModel, contextList);
            var derefrencedFieldModelController = GetController<FieldModelController>(dereferencedFieldModel.Id);
            return derefrencedFieldModelController;
        }


        /// <summary>
        /// Follows a <see cref="ReferenceFieldModelController"/>/<see cref="FieldModelController"/> or chain of <see cref="ReferenceFieldModelController"/> to the "root" item, which is a <see cref="TFieldModelControllerType"/>
        /// </summary>
        public static TFieldModelControllerType DereferenceToRootFieldModel<TFieldModelControllerType>(FieldModelController reference, List<DocumentController> contextList=null) where TFieldModelControllerType : FieldModelController
        {
            var rootFieldModelController = DereferenceToRootFieldModel(reference, contextList);
            Debug.Assert(rootFieldModelController is TFieldModelControllerType, "The chain of references ends in a field model controller which is not of the desired type");
            return rootFieldModelController as TFieldModelControllerType;
        }


        #endregion
    }
}
