using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class PlayerSpaceController : MonoBehaviour
    {
        private DiContainer _container;
        private TransformVariableManager _transformVariableManager;
        private CalibrationData _calibrationData;
        private PlayerAvatarManager _playerAvatarManager;

        private Transform _fullBodySpace;
        private Transform _calibrationSpace;

        public FollowedNode head { get; private set; }

        public FollowedNode leftHand { get; private set; }

        public FollowedNode rightHand { get; private set; }

        public TrackedNode waist { get; private set; }

        public TrackedNode leftFoot { get; private set; }

        public TrackedNode rightFoot { get; private set; }

        public Transform waistReference { get; private set; }

        public Transform leftFootReference { get; private set; }

        public Transform rightFootReference { get; private set; }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(DiContainer container, TransformVariableManager transformVariableManager, CalibrationData calibrationData, PlayerAvatarManager playerAvatarManager)
        {
            _container = container;
            _transformVariableManager = transformVariableManager;
            _calibrationData = calibrationData;
            _playerAvatarManager = playerAvatarManager;
        }

        private void Start()
        {
            FollowedNode originNode = _container.InstantiateComponent<FollowedNode>(gameObject);
            originNode.transformVariable = _transformVariableManager.origin;

            head = CreateSimpleTarget("Head", _transformVariableManager.head);
            leftHand = CreateSimpleTarget("Left Hand", _transformVariableManager.leftHand);
            rightHand = CreateSimpleTarget("Right Hand", _transformVariableManager.rightHand);

            GameObject fullBodySpace = new("Full Body Space");
            _fullBodySpace = fullBodySpace.transform;
            _fullBodySpace.SetParent(transform, false);

            waist = CreateFullBodyTarget("Waist", TrackedNodeType.Waist);
            leftFoot = CreateFullBodyTarget("Left Foot", TrackedNodeType.LeftFoot);
            rightFoot = CreateFullBodyTarget("Right Foot", TrackedNodeType.RightFoot);
        }

        public void SetCalibrationModeActive(bool active)
        {
            SpawnedAvatar spawnedAvatar = _playerAvatarManager.currentlySpawnedAvatar;

            if (spawnedAvatar && active)
            {
                AvatarPrefab prefab = spawnedAvatar.prefab;

                _calibrationSpace = new GameObject("Calibration Space").transform;
                _calibrationSpace.localScale = spawnedAvatar.transform.localScale;

                waistReference = new GameObject("Waist Reference").transform;
                waistReference.SetParent(_calibrationSpace, false);
                waistReference.localPosition = prefab.waist.position;
                waistReference.localRotation = prefab.waist.rotation;

                leftFootReference = new GameObject("Left Foot Reference").transform;
                leftFootReference.SetParent(_calibrationSpace, false);
                leftFootReference.localPosition = prefab.leftFoot.position;
                leftFootReference.localRotation = prefab.leftFoot.rotation;

                rightFootReference = new GameObject("Right Foot Reference").transform;
                rightFootReference.SetParent(_calibrationSpace, false);
                rightFootReference.localPosition = prefab.rightFoot.position;
                rightFootReference.localRotation = prefab.rightFoot.rotation;

                waist.targetTransform.SetParent(waistReference, false);
                leftFoot.targetTransform.SetParent(leftFootReference, false);
                rightFoot.targetTransform.SetParent(rightFootReference, false);
            }
            else
            {
                if (waistReference)
                {
                    waist.targetTransform.SetParent(waist.manualCalibrationOffset, false);
                    Destroy(waistReference);
                    waistReference = null;
                }

                if (leftFootReference)
                {
                    leftFoot.targetTransform.SetParent(leftFoot.manualCalibrationOffset, false);
                    Destroy(leftFootReference);
                    leftFootReference = null;
                }

                if (rightFootReference)
                {
                    rightFoot.targetTransform.SetParent(rightFoot.manualCalibrationOffset, false);
                    Destroy(rightFootReference);
                    rightFootReference = null;
                }

                Destroy(_calibrationSpace);
            }
        }

        public void SaveAutomaticFullBodyCalibration()
        {
            waist.calibrationOffset.position = transform.InverseTransformPoint(waist.transform.localPosition.x, head.transform.localPosition.y / 22.5f * 14, waist.transform.localPosition.z);
            waist.calibrationOffset.rotation = transform.rotation * Quaternion.LookRotation(Vector3.ProjectOnPlane(waist.transform.forward, transform.up), transform.up);

            leftFoot.calibrationOffset.position = transform.InverseTransformPoint(leftFoot.transform.localPosition.x, 0, leftFoot.transform.localPosition.z);
            leftFoot.calibrationOffset.rotation = transform.rotation * Quaternion.LookRotation(transform.up, Vector3.ProjectOnPlane(leftFoot.transform.up, transform.up));

            rightFoot.calibrationOffset.position = transform.InverseTransformPoint(rightFoot.transform.localPosition.x, 0, rightFoot.transform.localPosition.z);
            rightFoot.calibrationOffset.rotation = transform.rotation * Quaternion.LookRotation(transform.up, Vector3.ProjectOnPlane(rightFoot.transform.up, transform.up));

            SaveCalibration(_calibrationData.automaticCalibration);
        }

        public void ClearAutomaticFullBodyCalibration()
        {
            _calibrationData.automaticCalibration.Clear();
        }

        public void SaveManualFullBodyCalibration(SpawnedAvatar spawnedAvatar)
        {
            if (waist.isTracking)
            {
                waist.calibrationOffset.position = waistReference.position;
                waist.calibrationOffset.rotation = waistReference.rotation;
            }

            if (leftFoot.isTracking)
            {
                leftFoot.calibrationOffset.position = leftFootReference.position;
                leftFoot.calibrationOffset.rotation = leftFootReference.rotation;
            }

            if (rightFoot.isTracking)
            {
                rightFoot.calibrationOffset.position = rightFootReference.position;
                rightFoot.calibrationOffset.rotation = rightFootReference.rotation;
            }

            SaveCalibration(_calibrationData.GetAvatarManualCalibration(spawnedAvatar.prefab.fileName));
        }

        public void ClearManualFullBodyCalibration(SpawnedAvatar spawnedAvatar)
        {
            _calibrationData.GetAvatarManualCalibration(spawnedAvatar.prefab.fileName).Clear();
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {

        }

        private void OnAvatarScaleChanged(float scale)
        {

        }

        private void SaveCalibration(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.waist = waist.isTracking
                ? new Pose(waist.calibrationOffset.localPosition, waist.calibrationOffset.localRotation)
                : Pose.identity;

            calibration.leftFoot = leftFoot.isTracking
                ? new Pose(leftFoot.calibrationOffset.localPosition, leftFoot.calibrationOffset.localRotation)
                : Pose.identity;

            calibration.rightFoot = rightFoot.isTracking
                ? new Pose(rightFoot.calibrationOffset.localPosition, rightFoot.calibrationOffset.localRotation)
                : Pose.identity;
        }

        private FollowedNode CreateSimpleTarget(string name, TransformVariable transformVariable)
        {
            GameObject rootObject = new(name);

            FollowedNode node = _container.InstantiateComponent<FollowedNode>(rootObject);
            node.type = transformVariable.type;
            node.transformVariable = transformVariable;

            _container.InstantiateComponent<AvatarOffsetParent>(rootObject);

            Transform rootTransform = rootObject.transform;
            rootTransform.SetParent(transform, false);

            node.targetTransform = CreateTarget(name, rootTransform, transformVariable.type);

            return node;
        }

        private TrackedNode CreateFullBodyTarget(string name, TrackedNodeType trackedNodeType)
        {
            GameObject rootObject = new(name);

            TrackedNode node = _container.InstantiateComponent<TrackedNode>(rootObject);
            node.type = trackedNodeType;

            Transform rootTransform = rootObject.transform;
            rootTransform.SetParent(_fullBodySpace, false);

            GameObject calibrationOffsetObject = new("Calibration Offset");
            CalibrationSetter calibrationSetter = _container.InstantiateComponent<CalibrationSetter>(calibrationOffsetObject);
            calibrationSetter.type = trackedNodeType;

            Transform calibrationOffsetTransform = calibrationOffsetObject.transform;
            calibrationOffsetTransform.SetParent(rootTransform, false);

            GameObject manualOffsetObject = new("Manual Offset");
            ManualOffsetSetter manualOffsetSetter = _container.InstantiateComponent<ManualOffsetSetter>(manualOffsetObject);
            manualOffsetSetter.type = trackedNodeType;

            _container.InstantiateComponent<AvatarOffsetParent>(manualOffsetObject);

            Transform manualOffsetTransform = manualOffsetObject.transform;
            manualOffsetTransform.SetParent(calibrationOffsetTransform, false);

            node.targetTransform = CreateTarget(name, manualOffsetTransform, trackedNodeType);
            node.calibrationOffset = calibrationOffsetTransform;
            node.manualCalibrationOffset = manualOffsetTransform;

            return node;
        }

        private Transform CreateTarget(string name, Transform parent, TrackedNodeType trackedNodeType)
        {
            Transform target = new GameObject($"{name} Target").transform;
            _container.InstantiateComponent<AvatarOffsetSetter>(target.gameObject, new object[] { trackedNodeType });
            target.SetParent(parent, false);
            return target;
        }
    }
}
