using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Type = Zu.TypeScript.TsTypes.Type;

namespace Dash
{
    public abstract class CachedEndpoint : IModelEndpoint<FieldModel>
    {
        private readonly Dictionary<string, FieldControllerBase> _cache = new Dictionary<string, FieldControllerBase>();

        public abstract Task AddDocument(FieldModel newDocument);
        public abstract Task UpdateDocument(FieldModel documentToUpdate);
        public abstract Task<FieldModel> GetDocument(string id);
        public abstract Task<List<FieldModel>> GetDocuments(IEnumerable<string> ids);
        public abstract Task<List<U>> GetDocuments<U>(IEnumerable<string> ids) where U : EntityBase;
        public abstract Task DeleteDocument(FieldModel document);
        public abstract Task DeleteDocuments(IEnumerable<FieldModel> documents);
        public abstract Task DeleteAllDocuments();
        public abstract Task<List<FieldModel>> GetDocumentsByQuery(IQuery<FieldModel> query);
        public abstract Task<List<U>> GetDocumentsByQuery<U>(IQuery<FieldModel> query) where U : EntityBase;
        public abstract Task Close();
        public abstract Task<bool> HasDocument(FieldModel model);
        public abstract bool CheckAllDocuments(IEnumerable<FieldModel> documents);
        public abstract Dictionary<string, string> GetBackups();
        public abstract void SetBackupInterval(int millis);
        public abstract void SetNumBackups(int numBackups);

        public async Task<FieldControllerBase> GetControllerAsync(string id)
        {
            if (_cache.TryGetValue(id, out FieldControllerBase field))
            {
                return field;
            }

            var model = await GetDocument(id);
            if (model == null) return null;
            field = await model.NewController();
            _cache[id] = field;
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
                for (int i = 0; i < foundFields.Count; i++)
                {
                    var field = await foundFields[i].NewController();
                    _cache[missingIds[i]] = field;
                    fields[missingIdxs[i]] = field;
                }
            }

            return fields;
        }

        public async Task<IList<V>> GetControllersAsync<V>(IEnumerable<string> ids) where V : FieldControllerBase
        {
            var fields = await GetControllersAsync(ids);
            var typedFields = fields.OfType<V>().ToList();
            if (typedFields.Count != fields.Count)
            {
                throw new ArgumentException("Some of the fields where of the wrong type", nameof(ids));
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
                    controller = await fieldModel.NewController();
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
