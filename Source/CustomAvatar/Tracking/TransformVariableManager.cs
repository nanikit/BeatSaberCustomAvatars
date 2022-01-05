using CustomAvatar.Logging;
using Zenject;

namespace CustomAvatar.Tracking
{
    internal class TransformVariableManager
    {
        public TransformVariableManager(DiContainer container)
        {
            origin = new(TrackedNodeType.Origin, container.Resolve<ILogger<TransformVariable>>());
            head = new(TrackedNodeType.Head, container.Resolve<ILogger<TransformVariable>>());
            leftHand = new(TrackedNodeType.LeftHand, container.Resolve<ILogger<TransformVariable>>());
            rightHand = new(TrackedNodeType.RightHand, container.Resolve<ILogger<TransformVariable>>());
        }

        public TransformVariable origin { get; }

        public TransformVariable head { get; }

        public TransformVariable leftHand { get; }

        public TransformVariable rightHand { get; }
    }
}
