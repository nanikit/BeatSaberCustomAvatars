//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright � 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

extern alias BeatSaberFinalIK;

using System;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Represents a <see cref="AvatarPrefab"/> that has been spawned into the game.
    /// </summary>
    public class SpawnedAvatar : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="AvatarPrefab"/> used to spawn this avatar.
        /// </summary>
        public AvatarPrefab prefab { get; private set; }

        /// <summary>
        /// The <see cref="IAvatarInput"/> used for tracking.
        /// </summary>
        public IAvatarInput input { get; private set; }

        /// <summary>
        /// The avatar's scale as a ratio of it's exported scale (i.e. it is initially 1 even if the avatar was exported with a different scale).
        /// </summary>
        public float scale
        {
            get => transform.localScale.y / _initialLocalScale.y;
            set
            {
                if (value <= 0) throw new InvalidOperationException("Scale must be greater than 0");
                if (float.IsInfinity(value)) throw new InvalidOperationException("Scale cannot be infinity");

                transform.localScale = _initialLocalScale * value;
                _logger.Info("Avatar resized with scale: " + value);
            }
        }

        public float scaledEyeHeight => prefab.eyeHeight * scale;

        private ILogger<SpawnedAvatar> _logger;
        private GameScenesManager _gameScenesManager;

        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;
        private EventManager _eventManager;

        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;

        public void SetFirstPersonVisibility(FirstPersonVisibility visibility)
        {
            switch (visibility)
            {
                case FirstPersonVisibility.Visible:
                    SetChildrenToLayer(AvatarLayers.kAlwaysVisible);
                    break;

                case FirstPersonVisibility.VisibleWithExclusionsApplied:
                    SetChildrenToLayer(AvatarLayers.kAlwaysVisible);
                    ApplyFirstPersonExclusions();
                    break;

                case FirstPersonVisibility.Hidden:
                    SetChildrenToLayer(AvatarLayers.kOnlyInThirdPerson);
                    break;
            }
        }

        #region Behaviour Lifecycle
#pragma warning disable IDE0051

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalScale = transform.localScale;

            _eventManager = GetComponent<EventManager>();
            _firstPersonExclusions = GetComponentsInChildren<FirstPersonExclusion>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        [Inject]
        private void Construct(ILogger<SpawnedAvatar> logger, AvatarPrefab avatarPrefab, IAvatarInput avatarTransforms, GameScenesManager gameScenesManager)
        {
            prefab = avatarPrefab;
            input = avatarTransforms;

            _logger = logger;
            _gameScenesManager = gameScenesManager;

            _logger.name = prefab.descriptor.name;
        }

        private void Start()
        {
            name = $"SpawnedAvatar({prefab.descriptor.name})";

            if (_initialLocalPosition.sqrMagnitude > 0)
            {
                _logger.Warning("Avatar root position is not at origin; resizing by height and floor adjust may not work properly.");
            }

            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        private void OnDestroy()
        {
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;

            Destroy(gameObject);
        }

#pragma warning restore IDE0051
        #endregion

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            if (!_eventManager) return;

            if (_gameScenesManager.IsSceneInStackAndActive("MenuCore"))
            {
                _eventManager.OnMenuEnter?.Invoke();
            }
        }

        private void SetChildrenToLayer(int layer)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.gameObject.layer = layer;
            }
        }

        private void ApplyFirstPersonExclusions()
        {
            foreach (FirstPersonExclusion firstPersonExclusion in _firstPersonExclusions)
            {
                foreach (GameObject gameObj in firstPersonExclusion.exclude)
                {
                    if (!gameObj) continue;

                    _logger.Trace($"Excluding '{gameObj.name}' from first person view");
                    gameObj.layer = AvatarLayers.kOnlyInThirdPerson;
                }
            }
        }
    }
}
