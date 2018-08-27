using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    //public abstract class BaseModelEndpoint<T> : IModelEndpoint<T> where T : EntityBase
    //{
    //    public async Task TrackDownReferences(FieldModel field, HashSet<FieldModel> fields)
    //    {
    //        fields.Add(field);
    //        switch (field)
    //        {
    //            case DocumentModel doc:
    //                await TrackDownReferences(doc, fields);
    //                break;
    //            case ListModel list:
    //                await TrackDownReferences(list, fields);
    //                break;
    //            case DocumentReferenceModel dref:
    //                await TrackDownReferences(dref, fields);
    //                break;
    //            case PointerReferenceModel pref:
    //                await TrackDownReferences(pref, fields);
    //                break;
    //        }
    //    }

    //    protected virtual async Task TrackDownReferences(DocumentModel doc, HashSet<FieldModel> fields)
    //    {
    //        var subFields = new List<string>();
    //        subFields.AddRange(doc.Fields.Keys);
    //        subFields.AddRange(doc.Fields.Values);

    //        await AddReferences(fields, subFields);
    //    }

    //    protected virtual async Task TrackDownReferences(ListModel list, HashSet<FieldModel> fields)
    //    {
    //        await AddReferences(fields, list.Data);
    //    }

    //    protected virtual async Task TrackDownReferences(PointerReferenceModel pref, HashSet<FieldModel> fields)
    //    {
    //        await AddReferences(fields, new [] {pref.KeyId, pref.ReferenceFieldModelId});
    //    }

    //    protected virtual async Task TrackDownReferences(DocumentReferenceModel dref, HashSet<FieldModel> fields)
    //    {
    //        await AddReferences(fields, new[] {dref.KeyId, dref.DocumentId});
    //    }

    //    protected async Task AddReferences(HashSet<FieldModel> fields, IEnumerable<string> ids)
    //    {
    //        if (!ids.Any()) return;
    //        await GetDocuments(ids, async (args) => {
    //                var results = args.ReturnedObjects.Cast<FieldModel>().ToList();//Even if there are other types of Entity bases, they should never be in a document if they aren't field models
    //                foreach (var res in results)
    //                {
    //                    if (fields.Contains(res)) continue;
    //                    await TrackDownReferences(res, fields);
    //                }
    //            },
    //            ex => throw ex);
    //    }

    //    public abstract void AddDocument(T newDocument, Action<T> success, Action<Exception> error);
    //    public abstract void UpdateDocument(T documentToUpdate, Action<T> success, Action<Exception> error);
    //    public abstract Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error);
    //    public abstract Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error);
    //    public abstract Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase;
    //    public abstract void DeleteDocument(T document, Action success, Action<Exception> error);
    //    public abstract void DeleteDocuments(IEnumerable<T> documents, Action success, Action<Exception> error);
    //    public abstract void DeleteAllDocuments(Action success, Action<Exception> error);
    //    public abstract Task GetDocumentsByQuery(IQuery<T> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error);
    //    public abstract Task GetDocumentsByQuery<V>(IQuery<T> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase;
    //    public abstract Task Close();
    //    public abstract void HasDocument(T model, Action<bool> success, Action<Exception> error);
    //    public abstract bool CheckAllDocuments(IEnumerable<T> documents);
    //    public abstract Dictionary<string, string> GetBackups();
    //    public abstract void SetBackupInterval(int millis);
    //    public abstract void SetNumBackups(int numBackups);
    //}
}
