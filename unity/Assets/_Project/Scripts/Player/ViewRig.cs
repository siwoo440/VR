using System;
using AtelierVerse.Core;
using UnityEngine;

namespace AtelierVerse.Player
{
    /// <summary>
    /// PC 화면의 카메라 리그: 위아래 시선, 1인칭과 3인칭 사이의 거리, 어깨 너머로 비켜서기, 벽 앞에서 멈추기.
    /// 무엇으로 조작하는지는 모르고, 조작 스크립트가 시선과 거리를 정해 준다. 시야각은 개인 설정을 따른다.
    /// VR에서는 기기가 카메라를 움직이므로 이 리그를 쓰지 않는다.
    /// </summary>
    public class ViewRig : MonoBehaviour
    {
        private const float FirstPersonLimit = 0.3f;
        private const float CameraRadius = 0.2f;

        [SerializeField] private Transform pivot;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private AvatarView avatar;
        [SerializeField] private float pitchLimit = 80f;
        [SerializeField] private float nearViewDistance = 2.5f;
        [SerializeField] private float farViewDistance = 8f;
        [SerializeField] private float zoomStep = 1f;
        [SerializeField] private float zoomSpeed = 14f;
        [SerializeField] private Vector2 shoulderOffset = new Vector2(0.55f, 0.3f);

        private Collider ownCollider;
        private float pitch;
        private float targetDistance;
        private float currentDistance;
        private float lastThirdPersonDistance;

        /// <summary>1인칭과 3인칭이 바뀔 때 알린다. 값이 true이면 1인칭이다.</summary>
        public event Action<bool> ViewModeChanged;

        public bool IsFirstPerson => targetDistance <= 0f;

        /// <summary>캐릭터 머리에서 카메라까지의 목표 거리. 0이면 1인칭이다.</summary>
        public float ViewDistance => targetDistance;

        /// <summary>위아래 시선의 각도. 아래쪽이 양수다.</summary>
        public float Pitch => pitch;

        public Camera ViewCamera => viewCamera;

        /// <summary>머리(카메라 기준점)의 위치. 손이 닿는 거리를 잴 때 쓴다.</summary>
        public Vector3 HeadPosition => pivot != null ? pivot.position : transform.position;

        private bool CanMoveCamera => viewCamera != null && viewCamera.transform != pivot;

        private void Awake()
        {
            ownCollider = GetComponentInParent<Collider>();
            lastThirdPersonDistance = nearViewDistance + zoomStep;
            if (viewCamera == null && pivot != null) viewCamera = pivot.GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            if (pivot == null)
            {
                Debug.LogError("[Atelier Verse] ViewRig에 카메라 기준점이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            GameSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplySettings;
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }

        /// <summary>위아래 시선을 정한다. 아래쪽이 양수이며 한계 각도 안으로 맞춘다.</summary>
        public void SetPitch(float lookPitch)
        {
            pitch = Mathf.Clamp(lookPitch, -pitchLimit, pitchLimit);
            if (pivot != null) pivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        /// <summary>위아래 시선을 더 돌린다.</summary>
        public void AddPitch(float delta)
        {
            SetPitch(pitch + delta);
        }

        /// <summary>1인칭과 3인칭을 서로 바꾼다. 3인칭은 마지막에 쓰던 거리로 돌아간다.</summary>
        public void ToggleView()
        {
            SetViewDistance(IsFirstPerson ? lastThirdPersonDistance : 0f);
        }

        public void SetViewDistance(float distance)
        {
            if (!CanMoveCamera) return;

            bool wasFirstPerson = IsFirstPerson;
            targetDistance = distance <= 0f ? 0f : Mathf.Clamp(distance, nearViewDistance, farViewDistance);
            if (targetDistance > 0f) lastThirdPersonDistance = targetDistance;
            if (wasFirstPerson != IsFirstPerson) ViewModeChanged?.Invoke(IsFirstPerson);
        }

        /// <summary>휠 한 칸만큼 가까이 당기거나 멀리 민다.</summary>
        public void Zoom(bool zoomIn)
        {
            SetViewDistance(StepViewDistance(targetDistance, zoomIn, nearViewDistance, farViewDistance, zoomStep));
        }

        /// <summary>
        /// 휠 한 칸을 굴렸을 때의 다음 시점 거리. 0은 1인칭이고, 1인칭에서 당기면 가까운 3인칭 거리로 넘어간다.
        /// </summary>
        public static float StepViewDistance(float current, bool zoomIn, float near, float far, float step)
        {
            if (zoomIn) return current <= near ? 0f : Mathf.Max(near, current - step);
            return current <= 0f ? near : Mathf.Min(far, current + step);
        }

        private void ApplySettings()
        {
            if (viewCamera != null) viewCamera.fieldOfView = GameSettings.FieldOfView;
        }

        /// <summary>카메라를 목표 거리로 옮긴다. 뒤에 벽이 있으면 벽 앞까지만 물러난다.</summary>
        private void UpdateCamera()
        {
            if (!CanMoveCamera) return;

            currentDistance = Mathf.MoveTowards(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

            float allowed = currentDistance;
            if (allowed > 0f
                && Physics.SphereCast(pivot.position, CameraRadius, -pivot.forward, out RaycastHit hit, allowed, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider != ownCollider)
            {
                allowed = hit.distance;
            }

            // 3인칭에서는 화면 가운데의 조준점이 자기 머리에 가리지 않도록 어깨 너머로 비켜선다.
            float shoulder = nearViewDistance > 0f ? Mathf.Clamp01(allowed / nearViewDistance) : 0f;
            viewCamera.transform.localPosition = new Vector3(shoulderOffset.x * shoulder, shoulderOffset.y * shoulder, -allowed);
            if (avatar != null) avatar.SetFirstPerson(allowed < FirstPersonLimit);
        }
    }
}
