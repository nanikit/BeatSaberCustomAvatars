using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class TransformVariableSetter : MonoBehaviour
    {
        public TransformVariable transformVariable { get; set; }

        public void OnEnable()
        {
            transformVariable?.Push(transform);
        }

        public void Start()
        {
            OnEnable();
        }

        public void OnDisable()
        {
            transformVariable?.Remove(transform);
        }
    }
}
