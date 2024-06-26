﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DashShared;
using Type = Zu.TypeScript.TsTypes.Type;

namespace Dash
{
    public abstract class CachedEndpoint : IModelEndpoint<FieldModel>
    {
        private readonly Dictionary<string, FieldControllerBase> _cache = new Dictionary<string, FieldControllerBase>();

        private readonly System.Threading.Timer _cleanupTimer;

        protected CachedEndpoint()
        {
            //_cleanupTimer = new Timer(Cleanup, null, 30 * 1000, 30 * 1000);
        }

        public async Task Cleanup()
        {
            //TODO Make this use ids so we don't create a ton of field models for no reason
            var docs = (await GetDocumentsByQuery(new EverythingQuery<FieldModel>())).Where(fm => !_cache.ContainsKey(fm.Id)).ToList();
            await DeleteModels(docs);
            Debug.WriteLine($"Cleanup removed {docs.Count} items");
        }

        protected abstract Task AddModel(FieldModel newDocument);
        protected abstract Task UpdateModel(FieldModel documentToUpdate);
        public abstract Task<FieldModel> GetDocument(string id);
        public abstract Task<List<FieldModel>> GetDocuments(IEnumerable<string> ids);
        public abstract Task<List<U>> GetDocuments<U>(IEnumerable<string> ids) where U : EntityBase;
        public abstract Task DeleteModel(FieldModel document);
        public abstract Task DeleteModels(IEnumerable<FieldModel> documents);
        public abstract Task DeleteAllModels();
        public abstract Task<List<FieldModel>> GetDocumentsByQuery(IQuery<FieldModel> query);
        public abstract Task<List<U>> GetDocumentsByQuery<U>(IQuery<FieldModel> query) where U : EntityBase;
        public abstract Task Close();
        public abstract Task<bool> HasDocument(FieldModel model);
        public abstract bool CheckAllDocuments(IEnumerable<FieldModel> documents);
        public abstract Dictionary<string, string> GetBackups();
        public abstract void SetBackupInterval(int millis);
        public abstract void SetNumBackups(int numBackups);

        public Task AddDocument(Controller<FieldModel> controller)
        {
            Debug.Assert(!_cache.ContainsKey(controller.Id));
            _cache[controller.Id] = (FieldControllerBase)controller;
            return AddModel(controller.Model);
        }

        public Task UpdateDocument(Controller<FieldModel> controller)
        {
            return UpdateModel(controller.Model);
        }

        public Task DeleteDocument(Controller<FieldModel> controller)
        {
            Debug.Assert(_cache.ContainsKey(controller.Id));
            _cache.Remove(controller.Id);
            return DeleteModel(controller.Model);
        }

        public Task DeleteDocuments(IEnumerable<Controller<FieldModel>> controllers)
        {
            foreach (var controller in controllers)
            {
                Debug.Assert(_cache.ContainsKey(controller.Id));
                _cache.Remove(controller.Id);
            }
            return DeleteModels(controllers.Select(cont => cont.Model));
        }

        public Task DeleteAllDocuments()
        {
            return DeleteAllModels();
        }

        public async Task<FieldControllerBase> GetControllerAsync(string id)
        {
            if (_cache.TryGetValue(id, out FieldControllerBase field))
            {
                return field;
            }

            var model = await GetDocument(id);
            if (model == null) return null;
            field = FieldControllerFactory.CreateFromModel(model);
            _cache[id] = field;
            await field.InitializeAsync();
            return field;

        }

        public async Task<V> GetControllerAsync<V>(string id) where V : FieldControllerBase
        {
            var field = await GetControllerAsync(id);
            return field as V;
        }

        public async Task<IList<FieldControllerBase>> GetControllersAsync(IEnumerable<string> ids)
        {
            var list = ids as IList<string> ?? ids.ToList();
            var fields = new List<FieldControllerBase>(list.Count);
            var missingIds = new List<string>();
            var missingIdxs = new List<int>();

            for (var i = 0; i < list.Count; i++)
            {
                var id = list[i];
                if (_cache.TryGetValue(id, out var field))
                {
                    fields.Add(field);
                }
                else
                {
                    fields.Add(null);
                    missingIds.Add(id);
                    missingIdxs.Add(i);
                }
            }

            if (missingIds.Any())
            {
                var foundFields = await GetDocuments(missingIds);
                var foundFieldsDict = foundFields.ToDictionary(fm => fm.Id, fm => fm);
                var initFields = new List<FieldControllerBase>();
                for (int i = 0; i < missingIds.Count; i++)
                {
                    FieldControllerBase field;
                    //This is necessary because the same item might be in ids more than once,
                    //so we could have put it in the cache in a previous iteration
                    if (_cache.TryGetValue(missingIds[i], out var getField))
                    {
                        field = getField;
                    } else
                    {
                        var f = foundFieldsDict[missingIds[i]];
                        field = FieldControllerFactory.CreateFromModel(f);
                        _cache[missingIds[i]] = field;
                        initFields.Add(field);
                    }
                    fields[missingIdxs[i]] = field;
                }
                //TODO This can/should be a Task.WhenAll
                initFields.ForEach(async (f) => await f.InitializeAsync());
            }

            return fields;
        }

        public async Task<IList<V>> GetControllersAsync<V>(IEnumerable<string> ids) where V : FieldControllerBase
        {
            var fields = await GetControllersAsync(ids);
            var typedFields = fields.OfType<V>().ToList();
            if (typedFields.Count != fields.Count)
            {
                throw new ArgumentException("Some of the fields were of the wrong type", nameof(ids));
            }

            return typedFields;
        }

        public Task<IList<FieldControllerBase>> GetControllersByQueryAsync(IQuery<FieldModel> query)
        {
            return GetControllersByQueryAsync<FieldControllerBase>(query);
        }

        public async Task<IList<V>> GetControllersByQueryAsync<V>(IQuery<FieldModel> query) where V : FieldControllerBase
        {
            var fields = await GetDocumentsByQuery(query);
            var controllers = new List<V>();

            foreach (var fieldModel in fields)
            {
                if (!_cache.TryGetValue(fieldModel.Id, out var controller))
                {
                    controller = FieldControllerFactory.CreateFromModel(fieldModel);
                    _cache[fieldModel.Id] = controller;
                    await controller.InitializeAsync();
                }

                if (controller is V vField)
                {
                    controllers.Add(vField);
                }
            }

            return controllers;
        }

        public FieldControllerBase GetController(string id)
        {
            return GetControllerAsync(id).Result;
        }

        public IList<FieldControllerBase> GetControllers(IEnumerable<string> ids)
        {
            return GetControllersAsync(ids).Result;
        }

        public V GetController<V>(string id) where V : FieldControllerBase
        {
            return GetControllerAsync<V>(id).Result;
        }

        public IList<V> GetControllers<V>(IEnumerable<string> ids) where V : FieldControllerBase
        {
            return GetControllersAsync<V>(ids).Result;
        }
    }
}
