using System;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    public interface IAvatarInput
    {
        event Action inputChanged;

        bool TryGetTransform(TrackedNodeType type, out Transform transform);

        bool TryGetFingerCurl(TrackedNodeType type, out FingerCurl fingerCurl);
    }
}
