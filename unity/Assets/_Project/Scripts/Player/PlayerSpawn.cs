using AtelierVerse.Core;
using AtelierVerse.World;
using UnityEngine;

namespace AtelierVerse.Player
{
    /// <summary>
    /// 이 기기의 캐릭터를 지금 맵의 시작 위치에 맞춘다(18일차). 맵이 열리면 그 맵의 시작 위치를 몸(CharacterMotor)에 알리고
    /// 그 자리로 보내며, 바닥의 표식(SpawnMarker)을 그 자리에 놓는다. 맵에 정한 시작 위치가 없으면 씬의 처음 자리를 쓴다.
    /// 지금 선 자리를 시작 위치로 삼는 일(SetHere)과 처음 자리로 되돌리는 일(Clear)도 여기서 하며, 값은 맵 파일에 저장된다.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class PlayerSpawn : MonoBehaviour
    {
        public const string SetMessage = "여기를 이 맵의 시작 위치로 정했습니다";
        public const string ClearedMessage = "시작 위치를 처음 자리로 되돌렸습니다";
        public const string OutsideMessage = "맵의 범위 안에서만 시작 위치를 정할 수 있습니다";
        public const string FailedMessage = "시작 위치를 저장하지 못했습니다";

        // 공중에서 정하면 아래의 바닥(이나 블록 위)을 시작 위치로 삼는다. 그때 아래를 살피는 거리.
        private const float GroundProbeUp = 0.3f;
        private const float GroundProbeDown = 40f;

        private CharacterMotor motor;
        private MapAutoSave autoSave;
        private SpawnMarker marker;
        private string knownMapId;

        /// <summary>지금 맵에 시작 위치를 따로 정했는지.</summary>
        public bool HasCustomSpawn => autoSave != null && autoSave.HasSpawn;

        private void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            autoSave = FindAnyObjectByType<MapAutoSave>();
            marker = FindAnyObjectByType<SpawnMarker>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (autoSave != null) autoSave.MapChanged += OnMapChanged;
        }

        private void OnDisable()
        {
            if (autoSave != null) autoSave.MapChanged -= OnMapChanged;
        }

        private void Start()
        {
            // 맵이 없는 씬에서도 표식은 처음 자리에 둔다.
            if (autoSave == null) ShowMarker();
        }

        /// <summary>
        /// 지금 선 자리와 바라보는 쪽을 이 맵의 시작 위치로 정한다. 공중에 떠 있으면 아래의 바닥이나 블록 위가 된다.
        /// 맵의 범위 밖이면 정하지 않고 알린다.
        /// </summary>
        public bool SetHere()
        {
            if (autoSave == null) return false;

            Vector3 position = GroundBelow(transform.position);
            float yaw = transform.eulerAngles.y;
            if (!autoSave.SetSpawn(position, yaw))
            {
                Notice.Post(OutsideMessage, NoticeKind.Warning);
                return false;
            }

            Notice.Post(SetMessage);
            return true;
        }

        /// <summary>시작 위치를 씬의 처음 자리로 되돌린다.</summary>
        public bool Clear()
        {
            if (autoSave == null) return false;

            if (!autoSave.ClearSpawn())
            {
                Notice.Post(FailedMessage, NoticeKind.Error);
                return false;
            }

            Notice.Post(ClearedMessage);
            return true;
        }

        /// <summary>
        /// 맵이 바뀌거나 맵의 정보가 고쳐졌다. 시작 위치를 몸에 알리고 표식을 옮긴다.
        /// 다른 맵이 열린 것이면(처음 열릴 때도) 캐릭터를 시작 위치로 보낸다.
        /// </summary>
        private void OnMapChanged()
        {
            bool switched = knownMapId != autoSave.MapId;
            knownMapId = autoSave.MapId;

            if (autoSave.HasSpawn) motor.SetSpawn(autoSave.SpawnPosition, autoSave.SpawnYaw);
            else motor.ResetSpawn();

            ShowMarker();
            if (switched) motor.Respawn();
        }

        private void ShowMarker()
        {
            if (marker != null) marker.Show(motor.SpawnPosition, motor.SpawnYaw);
        }

        /// <summary>발아래의 바닥이나 블록 위의 자리. 아래에 아무것도 없으면 지금 자리 그대로다. 자기 몸은 세지 않는다.</summary>
        private Vector3 GroundBelow(Vector3 feet)
        {
            int others = ~(1 << gameObject.layer);
            Vector3 origin = feet + Vector3.up * GroundProbeUp;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeUp + GroundProbeDown, others, QueryTriggerInteraction.Ignore))
            {
                feet.y = hit.point.y;
            }

            return feet;
        }
    }
}
