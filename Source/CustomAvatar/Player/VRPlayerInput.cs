using System;
using CustomAvatar.Tracking;
using DynamicOpenVR.IO;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class VRPlayerInput : IAvatarInput, IInitializable, IDisposable
    {
        public const float kDefaultPlayerArmSpan = 1.7f;

        private readonly PlayerSpaceController _playerSpace;

        private SkeletalInput _leftHandAnimAction;
        private SkeletalInput _rightHandAnimAction;

        public VRPlayerInput(PlayerSpaceController playerSpace)
        {
            _playerSpace = playerSpace;
        }

        public bool allowMaintainPelvisPosition => false;


        public event Action inputChanged;

        public void Initialize()
        {
            _leftHandAnimAction = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");
        }

        public bool TryGetFingerCurl(TrackedNodeType use, out FingerCurl curl)
        {
            SkeletalInput handAnim = use switch
            {
                TrackedNodeType.LeftHand => _leftHandAnimAction,
                TrackedNodeType.RightHand => _rightHandAnimAction,
                _ => throw new InvalidOperationException($"{nameof(TryGetFingerCurl)} only supports {nameof(TrackedNodeType.LeftHand)} and {nameof(TrackedNodeType.RightHand)}"),
            };

            if (!handAnim.isActive || handAnim.summaryData == null)
            {
                curl = null;
                return false;
            }

            curl = new FingerCurl(handAnim.summaryData.thumbCurl, handAnim.summaryData.indexCurl, handAnim.summaryData.middleCurl, handAnim.summaryData.ringCurl, handAnim.summaryData.littleCurl);
            return true;
        }

        public bool TryGetTransform(TrackedNodeType type, out Transform transform)
        {
            switch (type)
            {
                case TrackedNodeType.Head:
                    transform = _playerSpace.head.targetTransform;
                    return transform && _playerSpace.head.isTracking;

                case TrackedNodeType.LeftHand:
                    transform = _playerSpace.leftHand.targetTransform;
                    return transform && _playerSpace.leftHand.isTracking;

                case TrackedNodeType.RightHand:
                    transform = _playerSpace.rightHand.targetTransform;
                    return transform && _playerSpace.rightHand.isTracking;

                case TrackedNodeType.Waist:
                    transform = _playerSpace.waist.targetTransform;
                    return transform && _playerSpace.waist.isTracking;

                case TrackedNodeType.LeftFoot:
                    transform = _playerSpace.leftFoot.targetTransform;
                    return transform && _playerSpace.leftFoot.isTracking;

                case TrackedNodeType.RightFoot:
                    transform = _playerSpace.rightFoot.targetTransform;
                    return transform && _playerSpace.rightFoot.isTracking;
            }

            transform = null;
            return false;
        }

        public void Dispose()
        {
            _leftHandAnimAction?.Dispose();
            _rightHandAnimAction?.Dispose();
        }

        private void OnNodeChanged()
        {
            inputChanged?.Invoke();
        }
    }
}
