﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentReferenceFieldModel : ReferenceFieldModel
    {
        public string DocumentId { get; set; }

        public DocumentReferenceFieldModel(string docId, string keyId, string id = null) : base(keyId, id)
        {
            DocumentId = docId;
        }

        public override bool Equals(object obj)
        {
            DocumentReferenceFieldModel refFieldModel = obj as DocumentReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return base.Equals(refFieldModel) && refFieldModel.DocumentId.Equals(DocumentId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentId.GetHashCode();
        }
    }
}
