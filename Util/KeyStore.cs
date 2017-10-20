﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class KeyStore
    {
        public static KeyController DocumentContextKey = new KeyController(DashConstants.KeyStore.DocumentContextKey);
        public static KeyController AbstractInterfaceKey = new KeyController(DashConstants.KeyStore.AbstractInterfaceKey);
        public static KeyController LayoutListKey = new KeyController(DashConstants.KeyStore.LayoutListKey);
        public static KeyController ActiveLayoutKey = new KeyController(DashConstants.KeyStore.ActiveLayoutKey);
        public static KeyController TitleKey = new KeyController(DashConstants.KeyStore.TitleKey);
        public static KeyController PrimaryKeyKey = new KeyController(DashConstants.KeyStore.PrimaryKeyKey);
        public static KeyController ThisKey = new KeyController(DashConstants.KeyStore.ThisKey);
        public static KeyController PrototypeKey = new KeyController(DashConstants.KeyStore.PrototypeKey);
        public static KeyController DelegatesKey = new KeyController(DashConstants.KeyStore.DelegatesKey);
        public static KeyController WidthFieldKey = new KeyController(DashConstants.KeyStore.WidthFieldKey);
        public static KeyController HeightFieldKey = new KeyController(DashConstants.KeyStore.HeightFieldKey);
        public static KeyController DataKey = new KeyController(DashConstants.KeyStore.DataKey);
        public static KeyController DocumentTextKey = new KeyController(DashConstants.KeyStore.DocumentTextKey);
        public static KeyController PositionFieldKey = new KeyController(DashConstants.KeyStore.PositionFieldKey);
        public static KeyController ScaleCenterFieldKey = new KeyController(DashConstants.KeyStore.ScaleCenterFieldKey);
        public static KeyController ScaleAmountFieldKey = new KeyController(DashConstants.KeyStore.ScaleAmountFieldKey);
        public static KeyController IconTypeFieldKey = new KeyController(DashConstants.KeyStore.IconTypeFieldKey);
        public static KeyController SystemUriKey = new KeyController(DashConstants.KeyStore.SystemUriKey);
        public static KeyController ThumbnailFieldKey = new KeyController(DashConstants.KeyStore.ThumbnailFieldKey);
        public static KeyController HeaderKey = new KeyController(DashConstants.KeyStore.HeaderKey);
        public static KeyController UserLinksKey = new KeyController(DashConstants.KeyStore.UserLinksKey);
        public static KeyController CollectionOutputKey = new KeyController(DashConstants.KeyStore.CollectionOutputKey);
        public static KeyController OperatorKey = new KeyController("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static KeyController CollectionViewTypeKey = new KeyController("EFC44F1C-3EB0-4111-8840-E694AB9DCB80", "Collection View Type");
        public static KeyController InkDataKey = new KeyController("1F6A3D2F-28D8-4365-ADA8-4C345C3AF8B6", "_InkData");

        /// <summary>
        /// Key for collection data
        /// TODO This might be better in a different class
        /// </summary>
        public static KeyController CollectionKey = new KeyController("7AE0CB96-7EF0-4A3E-AFC8-0700BB553CE2", "Collection");
    }
}
