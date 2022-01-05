using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class CalibrationSetter : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;
        private Settings _settings;
        private CalibrationData _calibrationData;

        public TrackedNodeType type { get; set; }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(PlayerAvatarManager playerAvatarManager, Settings settings, CalibrationData calibrationData)
        {
            _playerAvatarManager = playerAvatarManager;
            _settings = settings;
            _calibrationData = calibrationData;
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

            if (avatarSettings.bypassCalibration)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            CalibrationData.FullBodyCalibration fullBodyCalibration =
                avatarSettings.useAutomaticCalibration
                    ? _calibrationData.automaticCalibration
                    : _calibrationData.GetAvatarManualCalibration(avatarPrefab.fileName);

            Pose pose;

            switch (type)
            {
                case TrackedNodeType.Waist:
                    pose = fullBodyCalibration.waist;
                    break;

                case TrackedNodeType.LeftFoot:
                    pose = fullBodyCalibration.leftFoot;
                    break;

                case TrackedNodeType.RightFoot:
                    pose = fullBodyCalibration.rightFoot;
                    break;

                default:
                    return;
            }

            transform.localPosition = pose.position;
            transform.localRotation = pose.rotation;
        }
    }
}
