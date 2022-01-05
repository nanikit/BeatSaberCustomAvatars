using System;
using System.Collections.Generic;
using CustomAvatar.Logging;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class TransformVariable
    {
        private readonly ILogger<TransformVariable> _logger;
        private readonly LinkedList<Transform> _transforms = new();

        public TransformVariable(TrackedNodeType type, ILogger<TransformVariable> logger)
        {
            this.type = type;

            _logger = logger;
            _logger.name = type.ToString();
        }

        public TrackedNodeType type { get; }

        public Transform value => _transforms.Last?.Value;

        public event Action<Transform> valueChanged;

        public void Push(Transform transform)
        {
            if (!transform)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (_transforms.Last?.Value == transform)
            {
                return;
            }

            _transforms.Remove(transform);
            _transforms.AddLast(transform);

            _logger.Info($"Using transform '{GetTransformHierarchy(transform)}'");

            valueChanged?.Invoke(transform);
        }

        public void Remove(Transform transform)
        {
            if (!transform)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            _transforms.Remove(transform);

            while (_transforms.Count > 0 && !_transforms.Last.Value)
            {
                _transforms.RemoveLast();
            }

            if (value)
            {
                _logger.Info($"Using transform '{GetTransformHierarchy(transform)}'");
            }
            else
            {
                _logger.Info($"No transform left in stack");
            }

            valueChanged?.Invoke(value);
        }

        private string GetTransformHierarchy(Transform transform)
        {
            List<string> hierarchy = new();

            while (transform != null)
            {
                hierarchy.Add(transform.name);
                transform = transform.parent;
            }

            hierarchy.Reverse();

            return string.Join("/", hierarchy);
        }
    }
}
