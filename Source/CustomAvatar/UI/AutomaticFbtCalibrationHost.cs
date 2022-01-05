//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using System.Collections.Generic;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Tracking;

namespace CustomAvatar.UI
{
    internal class AutomaticFbtCalibrationHost : ViewControllerHost
    {
        #region Values

        internal readonly List<object> waistTrackerOptions = new List<object> { WaistTrackerPosition.Front, WaistTrackerPosition.Left, WaistTrackerPosition.Right, WaistTrackerPosition.Back };

        #endregion

        private readonly PlayerAvatarManager _avatarManager;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;
        private readonly AvatarSpecificSettingsHost _avatarSpecificSettingsHost;
        private readonly PlayerSpaceController _playerSpace;

        internal AutomaticFbtCalibrationHost(PlayerAvatarManager avatarManager, Settings settings, CalibrationData calibrationData, AvatarSpecificSettingsHost avatarSpecificSettingsHost, PlayerSpaceController playerSpace)
        {
            _avatarManager = avatarManager;
            _settings = settings;
            _calibrationData = calibrationData;
            _avatarSpecificSettingsHost = avatarSpecificSettingsHost;
            _playerSpace = playerSpace;
        }

        protected bool calibrateFullBodyTrackingOnStart
        {
            get => _settings.calibrateFullBodyTrackingOnStart;
            set
            {
                _settings.calibrateFullBodyTrackingOnStart = value;
                NotifyPropertyChanged();
            }
        }

        protected float pelvisOffset
        {
            get => _settings.automaticCalibration.pelvisOffset;
            set
            {
                _settings.automaticCalibration.pelvisOffset = value;
                NotifyPropertyChanged();
            }
        }

        protected float footOffset
        {
            get => _settings.automaticCalibration.legOffset;
            set
            {
                _settings.automaticCalibration.legOffset = value;
                NotifyPropertyChanged();
            }
        }

        protected WaistTrackerPosition waistTrackerPosition
        {
            get => _settings.automaticCalibration.waistTrackerPosition;
            set
            {
                _settings.automaticCalibration.waistTrackerPosition = value;
                NotifyPropertyChanged();
            }
        }

        protected bool isCalibrateButtonEnabled => _areTrackersDetected && _currentAvatarSupportsAutomaticCalibration;

        protected string calibrateButtonText => _calibrationData.automaticCalibration.isCalibrated ? "Recalibrate" : "Calibrate";

        protected string calibrateButtonHoverHint => _currentAvatarSupportsAutomaticCalibration ? (_areTrackersDetected ? "Calibrate full body tracking automatically" : "No trackers detected") : "Not supported by current avatar";

        protected bool isClearButtonEnabled => _calibrationData.automaticCalibration.isCalibrated;

        private bool _areTrackersDetected => true; // _playerInput.TryGetUncalibratedPose(DeviceUse.Waist, out Pose _) || _playerInput.TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose _) || _playerInput.TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose _);

        private bool _currentAvatarSupportsAutomaticCalibration => _avatarManager.currentlySpawnedAvatar && _avatarManager.currentlySpawnedAvatar.prefab.descriptor.supportsAutomaticCalibration;

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _avatarManager.avatarLoading += OnAvatarLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            // _playerSpace.inputChanged += OnInputChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnInputChanged();
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisalbling)
        {
            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            // _playerSpace.inputChanged -= OnInputChanged;
        }

        private void OnAvatarLoading(string filePath, string name)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
        }

        private void OnInputChanged()
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
        }

        #region Actions
#pragma warning disable IDE0051

        private void OnCalibrateAutoFullBodyTrackingClicked()
        {
            _playerSpace.SaveAutomaticFullBodyCalibration();

            NotifyPropertyChanged(nameof(calibrateButtonText));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));

            _avatarSpecificSettingsHost.useAutomaticCalibration = true;
        }

        private void OnClearAutoFullBodyTrackingCalibrationDataClicked()
        {
            _playerSpace.ClearAutomaticFullBodyCalibration();

            NotifyPropertyChanged(nameof(calibrateButtonText));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

#pragma warning restore IDE0051
        #endregion
    }
}
