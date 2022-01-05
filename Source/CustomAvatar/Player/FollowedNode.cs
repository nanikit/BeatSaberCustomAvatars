using System;
using CustomAvatar.Tracking;
using UnityEngine;

namespace CustomAvatar.Player
{
    internal class FollowedNode : AvatarNode
    {
        public TransformVariable transformVariable { get; set; }

        private void Start()
        {
            if (transformVariable == null)
            {
                DestroyImmediate(this);
                throw new NullReferenceException($"{nameof(transformVariable)} must be set");
            }

            transformVariable.valueChanged += OnValueChanged;
            OnValueChanged(transformVariable.value);
        }

        private void Update()
        {
            if (transformVariable.value)
            {
                transform.localPosition = transformVariable.value.localPosition;
                transform.localRotation = transformVariable.value.localRotation;
            }
            else
            {
                isTracking = false;
            }
        }

        private void OnDestroy()
        {
            transformVariable.valueChanged -= OnValueChanged;
        }

        private void OnValueChanged(Transform transform)
        {
            isTracking = transform;
        }
    }
}
