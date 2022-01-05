using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class ManualOffsetSetter : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;
        private Settings _settings;

        public TrackedNodeType type { get; set; }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(PlayerAvatarManager playerAvatarManager, Settings settings)
        {
            _playerAvatarManager = playerAvatarManager;
            _settings = settings;
        }

        // TODO: switch to some kind of event-based system
        private void Update()
        {
            SpawnedAvatar spawnedAvatar = _playerAvatarManager.currentlySpawnedAvatar;

            if (!spawnedAvatar)
            {
                return;
            }

            AvatarPrefab avatarPrefab = spawnedAvatar.prefab;

            Settings.AvatarSpecificSettings avatarSettings = _settings.GetAvatarSettings(avatarPrefab.fileName);

            if (!avatarSettings.useAutomaticCalibration)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            Pose pose;

            switch (type)
            {
                case TrackedNodeType.Waist:
                    Quaternion rotation = Quaternion.Euler(0, (float)_settings.automaticCalibration.waistTrackerPosition, 0);
                    pose = new Pose(Quaternion.Inverse(rotation) * new Vector3(0, 0, _settings.automaticCalibration.pelvisOffset), rotation);
                    break;

                case TrackedNodeType.LeftFoot:
                case TrackedNodeType.RightFoot:
                    pose = new Pose(new Vector3(0, -_settings.automaticCalibration.legOffset, 0), Quaternion.identity);
                    break;

                default:
                    return;
            }

            transform.localPosition = pose.position;
            transform.localRotation = pose.rotation;
        }
    }
}
