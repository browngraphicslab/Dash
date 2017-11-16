﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public static class ModelExtensions
    {
        public static DocumentController NewController(this DocumentModel model)
        {
            var controller =  DocumentControllerFactory.CreateFromModel(model);
            return controller;
        }

        public static FieldControllerBase NewController(this FieldModel model)
        {
           var controller =  FieldControllerFactory.CreateFromModel(model);
            return controller;
        }

        public static KeyController NewController(this KeyModel model)
        {
            var controller = KeyControllerFactory.CreateFromModel(model);
            return controller;
        }

        public static DocumentController GetController(this DocumentModel model)
        {
            return ContentController<DocumentModel>.GetController<DocumentController>(model.Id);
        }

        public static FieldControllerBase GetController(this FieldModel model)
        {
            return ContentController<FieldModel>.GetController<FieldControllerBase>(model.Id);
        }

        public static KeyController GetController(this KeyModel model)
        {
            return ContentController<FieldModel>.GetController<KeyController>(model.Id);
        }
    }
}
