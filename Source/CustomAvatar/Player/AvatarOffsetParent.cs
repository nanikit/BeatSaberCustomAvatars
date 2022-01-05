using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class AvatarOffsetParent : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(PlayerAvatarManager playerAvatarManager)
        {
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

            transform.localScale = currentlySpawnedAvatar.transform.localScale;
        }
    }
}
