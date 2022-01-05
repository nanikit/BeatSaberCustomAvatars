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

extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using System;
using System.Collections.Generic;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarIK : MonoBehaviour
    {
        public bool isLocomotionEnabled
        {
            get => _isLocomotionEnabled;
            set
            {
                _isLocomotionEnabled = value;
                UpdateLocomotion();
            }
        }

        private VRIK _vrik;
        private VRIKManager _vrikManager;

        private bool _fixTransforms;

        private readonly List<BeatSaberDynamicBone::DynamicBone> _dynamicBones = new();
        private readonly List<TwistRelaxer> _twistRelaxers = new();

        // create delegates for dynamic bones private methods (more efficient than continuously calling Invoke)
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBoneOnEnableDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("OnEnable");
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBoneStartDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("Start");
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBonePreUpdateDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("PreUpdate");
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBoneLateUpdateDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("LateUpdate");

        private static readonly Action<TwistRelaxer> kTwistRelaxerStartDelegate = MethodAccessor<TwistRelaxer, Action<TwistRelaxer>>.GetDelegate("Start");

        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private ILogger<AvatarIK> _logger;
        private IKHelper _ikHelper;

        private bool _isLocomotionEnabled = false;
        private Pose _previousParentPose;

        #region Behaviour Lifecycle
#pragma warning disable IDE0051

        private void Awake()
        {
            foreach (TwistRelaxer twistRelaxer in GetComponentsInChildren<TwistRelaxer>())
            {
                if (!twistRelaxer.enabled) continue;

                twistRelaxer.enabled = false;

                _twistRelaxers.Add(twistRelaxer);
            }

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>())
            {
                if (!dynamicBone.enabled) continue;

                dynamicBone.enabled = false;

                _dynamicBones.Add(dynamicBone);
            }
        }

        [Inject]
        private void Construct(IAvatarInput input, SpawnedAvatar avatar, ILogger<AvatarIK> logger, IKHelper ikHelper)
        {
            _input = input;
            _avatar = avatar;
            _logger = logger;
            _ikHelper = ikHelper;

            _logger.name = _avatar.prefab.descriptor.name;
        }

        private void Start()
        {
            _vrikManager = GetComponentInChildren<VRIKManager>();

            _vrik = _ikHelper.InitializeVRIK(_vrikManager, gameObject);

            _fixTransforms = _vrikManager.fixTransforms;
            _vrik.fixTransforms = false; // FixTransforms is manually called in Update

            _input.inputChanged += OnInputChanged;

            UpdateLocomotion();
            UpdateSolverTargets();

            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                kTwistRelaxerStartDelegate(twistRelaxer);
            }

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                kDynamicBoneOnEnableDelegate(dynamicBone);
                kDynamicBoneStartDelegate(dynamicBone);
            }
        }

        private void Update()
        {
            ApplyPlatformMotion();

            if (_fixTransforms)
            {
                _vrik.solver.FixTransforms();
            }

            // DynamicBones PreUpdate
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                kDynamicBonePreUpdateDelegate(dynamicBone);
            }
        }

        private void LateUpdate()
        {
            // VRIK must run before dynamic bones
            _vrik.UpdateSolverExternal();

            // relax after VRIK update
            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                twistRelaxer.Relax();
            }

            // update dynamic bones
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                kDynamicBoneLateUpdateDelegate(dynamicBone);
            }
        }

        private void OnDestroy()
        {
            _input.inputChanged -= OnInputChanged;
        }

#pragma warning restore IDE0051
        #endregion

        internal void AddPlatformMotion(Vector3 deltaPosition, Quaternion deltaRotation, Vector3 platformPivot)
        {
            _vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, platformPivot);
        }

        private void ApplyPlatformMotion()
        {
            Transform parent = transform.parent;

            if (!parent) return;

            // TODO: this is slightly ridiculous, try to find a better way
            Vector3 deltaPosition = parent.position - _previousParentPose.position;
            Quaternion deltaRotation = parent.rotation * Quaternion.Inverse(_previousParentPose.rotation);

            _vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, parent.position);
            _previousParentPose = new Pose(parent.position, parent.rotation);
        }

        private void UpdateLocomotion()
        {
            if (!_vrik || !_vrikManager) return;

            _vrik.solver.locomotion.weight = _isLocomotionEnabled ? _vrikManager.solver_locomotion_weight : 0;

            if (_vrik.references.root)
            {
                _vrik.references.root.transform.localPosition = Vector3.zero;
            }
        }

        private void OnInputChanged()
        {
            UpdateSolverTargets();
        }

        private void UpdateSolverTargets()
        {
            if (!_vrik || !_vrikManager || _input == null) return;

            _logger.Info("Updating solver targets");

            if (_input.TryGetTransform(TrackedNodeType.Head, out Transform head))
            {
                _vrik.solver.spine.headTarget = head;
                _vrik.solver.spine.positionWeight = _vrikManager.solver_spine_positionWeight;
                _vrik.solver.spine.rotationWeight = _vrikManager.solver_spine_rotationWeight;
            }
            else
            {
                _vrik.solver.spine.headTarget = null;
                _vrik.solver.spine.positionWeight = 0;
                _vrik.solver.spine.rotationWeight = 0;
            }

            if (_input.TryGetTransform(TrackedNodeType.LeftHand, out Transform leftHand))
            {
                _vrik.solver.leftArm.target = leftHand;
                _vrik.solver.leftArm.positionWeight = _vrikManager.solver_leftArm_positionWeight;
                _vrik.solver.leftArm.rotationWeight = _vrikManager.solver_leftArm_rotationWeight;
            }
            else
            {
                _vrik.solver.leftArm.target = null;
                _vrik.solver.leftArm.positionWeight = 0;
                _vrik.solver.leftArm.rotationWeight = 0;
            }

            if (_input.TryGetTransform(TrackedNodeType.RightHand, out Transform rightHand))
            {
                _vrik.solver.rightArm.target = rightHand;
                _vrik.solver.rightArm.positionWeight = _vrikManager.solver_rightArm_positionWeight;
                _vrik.solver.rightArm.rotationWeight = _vrikManager.solver_rightArm_rotationWeight;
            }
            else
            {
                _vrik.solver.rightArm.target = null;
                _vrik.solver.rightArm.positionWeight = 0;
                _vrik.solver.rightArm.rotationWeight = 0;
            }

            if (_input.TryGetTransform(TrackedNodeType.LeftFoot, out Transform leftFoot))
            {
                _vrik.solver.leftLeg.target = leftFoot;
                _vrik.solver.leftLeg.positionWeight = _vrikManager.solver_leftLeg_positionWeight;
                _vrik.solver.leftLeg.rotationWeight = _vrikManager.solver_leftLeg_rotationWeight;
            }
            else
            {
                _vrik.solver.leftLeg.target = null;
                _vrik.solver.leftLeg.positionWeight = 0;
                _vrik.solver.leftLeg.rotationWeight = 0;
            }

            if (_input.TryGetTransform(TrackedNodeType.RightFoot, out Transform rightFoot))
            {
                _vrik.solver.rightLeg.target = rightFoot;
                _vrik.solver.rightLeg.positionWeight = _vrikManager.solver_rightLeg_positionWeight;
                _vrik.solver.rightLeg.rotationWeight = _vrikManager.solver_rightLeg_rotationWeight;
            }
            else
            {
                _vrik.solver.rightLeg.target = null;
                _vrik.solver.rightLeg.positionWeight = 0;
                _vrik.solver.rightLeg.rotationWeight = 0;
            }

            if (_input.TryGetTransform(TrackedNodeType.Waist, out Transform waist))
            {
                _vrik.solver.spine.pelvisTarget = waist;
                _vrik.solver.spine.pelvisPositionWeight = _vrikManager.solver_spine_pelvisPositionWeight;
                _vrik.solver.spine.pelvisRotationWeight = _vrikManager.solver_spine_pelvisRotationWeight;
                _vrik.solver.spine.maintainPelvisPosition = 0;
                _vrik.solver.plantFeet = false;
            }
            else
            {
                _vrik.solver.spine.pelvisTarget = null;
                _vrik.solver.spine.pelvisPositionWeight = 0;
                _vrik.solver.spine.pelvisRotationWeight = 0;
                _vrik.solver.spine.maintainPelvisPosition = _vrikManager.solver_spine_maintainPelvisPosition;
                _vrik.solver.plantFeet = _vrikManager.solver_plantFeet;
            }
        }
    }
}
