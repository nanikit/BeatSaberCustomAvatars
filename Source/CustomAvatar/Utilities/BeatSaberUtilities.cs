﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Configuration;
using CustomAvatar.HarmonyPatches;
using CustomAvatar.Tracking;
using System;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Utilities
{
    internal class BeatSaberUtilities : IInitializable, IDisposable
    {
        public static readonly float kDefaultPlayerEyeHeight = MainSettingsModelSO.kDefaultPlayerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerArmSpan = MainSettingsModelSO.kDefaultPlayerHeight;

        public Vector3 roomCenter => _mainSettingsModel.roomCenter;
        public Quaternion roomRotation => Quaternion.Euler(0, _mainSettingsModel.roomRotation, 0);
        public float playerHeight => _playerDataModel.playerData.playerSpecificSettings.playerHeight;
        public float playerEyeHeight => playerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;

        public event Action<Vector3, Quaternion> roomAdjustChanged;
        public event Action<float> playerHeightChanged;

        private readonly MainSettingsModelSO _mainSettingsModel;
        private readonly PlayerDataModel _playerDataModel;
        private readonly Settings _settings;
        private readonly IVRPlatformHelper _vrPlatformHelper;
        private readonly Transform _transform = new GameObject().transform;

        internal BeatSaberUtilities(MainSettingsModelSO mainSettingsModel, PlayerDataModel playerDataModel, Settings settings, IVRPlatformHelper vrPlatformHelper)
        {
            _mainSettingsModel = mainSettingsModel;
            _playerDataModel = playerDataModel;
            _settings = settings;
            _vrPlatformHelper = vrPlatformHelper;
        }

        public void Initialize()
        {
            _mainSettingsModel.roomCenter.didChangeEvent += OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent += OnRoomRotationChanged;

            PlayerData_playerSpecificSettings.playerHeightChanged += OnPlayerHeightChanged;
        }

        public void Dispose()
        {
            _mainSettingsModel.roomCenter.didChangeEvent -= OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent -= OnRoomRotationChanged;

            PlayerData_playerSpecificSettings.playerHeightChanged -= OnPlayerHeightChanged;
        }

        /// <summary>
        /// Gets the current player's height, taking into account whether the floor is being moved with the room or not.
        /// </summary>
        public float GetRoomAdjustedPlayerEyeHeight()
        {
            if (_settings.moveFloorWithRoomAdjust)
            {
                return playerEyeHeight - _mainSettingsModel.roomCenter.value.y;
            }

            return playerEyeHeight;
        }

        /// <summary>
        /// Similar to the various implementations of <see cref="IVRPlatformHelper.AdjustControllerTransform(XRNode, Transform, Vector3, Vector3)"/> except it updates a pose instead of adjusting a transform.
        /// </summary>
        public void AdjustPlatformSpecificControllerPose(DeviceUse use, ref Pose pose)
        {
            if (use != DeviceUse.LeftHand && use != DeviceUse.RightHand) return;

            XRNode node = use == DeviceUse.LeftHand ? XRNode.LeftHand : XRNode.RightHand;
            _transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _vrPlatformHelper.AdjustControllerTransform(node, _transform, Vector3.zero, Vector3.zero);

            Quaternion rotation = _transform.rotation;
            Vector3 position = _transform.position;
            if (use == DeviceUse.LeftHand)
            {
                rotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
                position = new Vector3(-position.x, position.y, position.z);
            }
            pose.rotation *= rotation;
            pose.position += pose.rotation * position;
        }

        private void OnRoomCenterChanged()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }

        private void OnRoomRotationChanged()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }

        private void OnPlayerHeightChanged(float playerHeight)
        {
            playerHeightChanged?.Invoke(playerHeight);
        }
    }
}
