using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;

namespace Dash
{
    /// <summary>
    /// A field model which encapsulates a list of Documents
    /// </summary>
    public class DocumentCollectionFieldModel : FieldModel
    {
        /// <summary>
        /// The list of <see cref="EntityBase.Id"/> which the <see cref="DocumentCollectionFieldModel"/> is encapsulating
        /// </summary>
        public List<string> Data;


        public DocumentCollectionFieldModel(IEnumerable<string> documentIds, string id = null) : base(id)
        {
            Data = documentIds.ToList();
        }

        /// <summary>
        /// Creates a new DocumentCollectionFieldModel using the passed in list of <see cref="DocumentModel"/>
        /// </summary>
        /// <param name="documents"></param>
        public DocumentCollectionFieldModel(IEnumerable<DocumentModel> documents, string id = null) : this(documents.Select(document => document.Id), id)
        {
            
        }

        /// <summary>
        /// Creates a new DocumentCollectionFieldModel using the passed in list of <see cref="DocumentModel.Id"/>
        /// </summary>
        /// <param name="documents"></param>
        public DocumentCollectionFieldModel(IEnumerable<string> documents)
        {
            Debug.Assert(documents != null);
            Data = documents.ToList();
        }

        protected override FieldModelDTO GetFieldDTOHelper() 
        {
            return new FieldModelDTO(TypeInfo.Collection, Data);
        }  
    }
}