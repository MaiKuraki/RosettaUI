﻿using System;
using System.Linq;

namespace RosettaUI
{
    /// <summary>
    ///     動的に内容が変化するElement
    ///     UI実装側はbuildの結果をいれるプレースホルダー的な役割
    /// </summary>
    public class DynamicElement : ElementGroup
    {
        public static DynamicElement Create<T>(Func<T> readStatus, Func<T, Element> buildWithStatus, string displayName = null)
            where T : IEquatable<T>
        {
            var status = readStatus();

            return new DynamicElement(
                () => buildWithStatus(status),
                e =>
                {
                    var newStatus = readStatus();
                    var rebuild = !newStatus.Equals(status);
                    if (rebuild) status = newStatus;
                    return rebuild;
                },
                displayName
            );
        }

        
        
        private readonly Func<Element> _build;
        private readonly string _displayName;
        private readonly Func<DynamicElement, bool> _rebuildIf;


        public DynamicElement(Func<Element> build, Func<DynamicElement, bool> rebuildIf, string displayName = null)
        {
            _build = build;
            _rebuildIf = rebuildIf;
            _displayName = displayName;

            BuildElement();
        }

        public override string DisplayName => string.IsNullOrEmpty(_displayName) ? base.DisplayName : _displayName;

        public Element Element => elements?.FirstOrDefault();


        private void BuildElement()
        {
            SetElements(new[] {_build?.Invoke()});
        }

        public override void Update()
        {
            if (_rebuildIf?.Invoke(this) ?? false)
            {
                Element?.Destroy();
                BuildElement();
                RebuildChildren();
            }
            else
            {
                if (Enable) UpdateInternal();
            }
        }
    }
}