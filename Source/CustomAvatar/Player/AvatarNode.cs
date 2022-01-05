using System;
using CustomAvatar.Tracking;
using UnityEngine;

namespace CustomAvatar.Player
{
    internal class AvatarNode : MonoBehaviour
    {
        public Transform targetTransform { get; set; }

        public TrackedNodeType type { get; set; }

        public bool isTracking
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                isActiveChanged?.Invoke(value);
            }
        }

        public event Action<bool> isActiveChanged;
    }
}
