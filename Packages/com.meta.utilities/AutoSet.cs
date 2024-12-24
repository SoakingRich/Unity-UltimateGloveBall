// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Meta.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]                   // Meta has created one class and two childclasses of PropertyAttribute - the attribute is to be used on 'Fields'
    public class AutoSetAttribute : PropertyAttribute                // accompanying editor scripts for use with AutoSet are in editor module, scripts called AutoSetDrawer and AutoSetPostProcessor
    {
        public AutoSetAttribute(Type type = default) { }              // the intention is that you could set any field as AutoSet,   and the field will auto set finding objects on the same gameobject
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetFromParentAttribute : AutoSetAttribute
    {
        public bool IncludeInactive { get; set; } = false;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetFromChildrenAttribute : AutoSetAttribute
    {
        public bool IncludeInactive { get; set; } = false;
    }
}