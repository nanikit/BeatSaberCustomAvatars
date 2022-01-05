using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class AvatarOffsetSetter : MonoBehaviour
    {
        private TrackedNodeType _type;
        private PlayerAvatarManager _playerAvatarManager;

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(TrackedNodeType type, PlayerAvatarManager playerAvatarManager)
        {
            _type = type;
            _playerAvatarManager = playerAvatarManager;
        }

        private void Start()
        {
            if (_playerAvatarManager != null)
            {
                _playerAvatarManager.avatarChanged += OnAvatarChanged;
                _playerAvatarManager.avatarScaleChanged += OnAvatarScaleChanged;
            }

            UpdateOffset();
        }

        private void OnDestroy()
        {
            if (_playerAvatarManager != null)
            {
                _playerAvatarManager.avatarChanged -= OnAvatarChanged;
                _playerAvatarManager.avatarScaleChanged -= OnAvatarScaleChanged;
            }
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            UpdateOffset();
        }

        private void OnAvatarScaleChanged(float scale)
        {
            UpdateOffset();
        }

        private void UpdateOffset()
        {
            SpawnedAvatar currentlySpawnedAvatar = _playerAvatarManager?.currentlySpawnedAvatar;

            if (!currentlySpawnedAvatar)
            {
                return;
            }

            AvatarPrefab prefab = currentlySpawnedAvatar.prefab;
            Pose offset;

            switch (_type)
            {
                case TrackedNodeType.Head:
                    offset = prefab.headOffset;
                    break;

                case TrackedNodeType.LeftHand:
                    offset = prefab.leftHandOffset;
                    break;

                case TrackedNodeType.RightHand:
                    offset = prefab.rightHandOffset;
                    break;

                case TrackedNodeType.Waist:
                    offset = prefab.waistOffset;
                    break;

                case TrackedNodeType.LeftFoot:
                    offset = prefab.leftFootOffset;
                    break;

                case TrackedNodeType.RightFoot:
                    offset = prefab.rightFootOffset;
                    break;

                default:
                    return;
            }

            transform.localPosition = offset.position;
            transform.localRotation = offset.rotation;
        }
    }
}
