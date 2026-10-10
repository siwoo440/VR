using System;
using System.Collections.Generic;
using AtelierVerse.Core;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>저장 상태. 화면의 저장 표시가 이 값을 보여 준다.</summary>
    public enum SaveState
    {
        Idle,
        Pending,
        Saved,
        Failed,
    }

    /// <summary>
    /// 블록 세계를 이 기기의 맵 파일과 맞춘다. 시작할 때 마지막으로 연 맵을 불러오고, 맵이 하나도 없으면 씬의 블록을 첫 맵으로 삼아 저장한다.
    /// 블록이 바뀌면 잠시 뒤에 저장하고, 앱을 끝내거나 씬을 떠날 때 남은 변경을 저장한다.
    /// 맵은 여럿일 수 있다(17일차). 다른 맵을 열고, 새 맵을 만들고, 이름을 바꾸고, 지우는 일을 여기서 하며,
    /// 맵을 바꿀 때는 지금 맵의 남은 변경을 먼저 저장한다. 파일의 목록과 이름은 MapLibrary가 다룬다.
    /// 지금 맵의 설명과 시작 위치(캐릭터가 처음 서는 자리)도 여기서 고친다(18일차).
    /// </summary>
    public class MapAutoSave : MonoBehaviour
    {
        public const string OpenedMessage = "맵을 열었습니다";
        public const string CreatedMessage = "새 맵을 만들었습니다";
        public const string RenamedMessage = "맵의 이름을 바꿨습니다";
        public const string InfoSavedMessage = "맵 정보를 저장했습니다";
        public const string DeletedMessage = "맵을 지웠습니다 · 파일은 휴지통 폴더에 남아 있습니다";
        public const string TooManyMessage = "맵을 더 만들 수 없습니다";
        public const string SaveFirstFailedMessage = "지금 맵을 저장하지 못해 다른 맵으로 바꾸지 않았습니다";

        [SerializeField] private BlockWorld world;
        [SerializeField] private float delay = 1f;
        [SerializeField] private string mapName = "시험 작업실";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private GameObject sampleScenery;

        private MapDocument header;
        private bool pending;
        private float dueTime;

        /// <summary>저장 상태나 안내 문구가 바뀌면 알린다.</summary>
        public event Action Changed;

        /// <summary>다른 맵이 열리거나 지금 맵의 이름이 바뀌면 알린다.</summary>
        public event Action MapChanged;

        public SaveState State { get; private set; } = SaveState.Idle;

        /// <summary>마지막 저장·불러오기의 안내 문구. 화면에 그대로 보여 준다.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>마지막 불러오기에서 건너뛴 블록 수 등의 결과.</summary>
        public MapLoadReport LastLoad { get; private set; }

        /// <summary>마지막으로 읽지 못한 파일을 옮겨 둔 경로. 없으면 null이다.</summary>
        public string SetAsidePath { get; private set; }

        /// <summary>옛 판의 파일을 새 판으로 올려 읽었을 때 원래 파일을 남겨 둔 경로. 없으면 null이다.</summary>
        public string BackupPath { get; private set; }

        /// <summary>지금 열려 있는 맵의 번호표.</summary>
        public string MapId { get; private set; } = MapLibrary.DefaultId;

        /// <summary>지금 열려 있는 맵의 이름. 화면에는 MapLibrary.DisplayName을 거쳐 보인다.</summary>
        public string MapName => header != null ? header.name ?? string.Empty : mapName;

        /// <summary>지금 열려 있는 맵의 설명.</summary>
        public string Description => header != null ? header.description ?? string.Empty : string.Empty;

        /// <summary>지금 맵에 시작 위치를 따로 정했는지. 정하지 않았으면 씬의 처음 자리에서 시작한다.</summary>
        public bool HasSpawn => header != null && header.spawn != null && header.spawn.custom;

        /// <summary>지금 맵의 시작 위치(발이 닿는 자리). HasSpawn일 때만 뜻이 있다.</summary>
        public Vector3 SpawnPosition => HasSpawn ? header.spawn.position : Vector3.zero;

        /// <summary>지금 맵의 시작 방향(좌우 각도). HasSpawn일 때만 뜻이 있다.</summary>
        public float SpawnYaw => HasSpawn ? header.spawn.yaw : 0f;

        public string FilePath => MapLibrary.PathOf(MapId);

        public bool HasPendingChanges => pending;

        public BlockWorld World => world;

        private void Awake()
        {
            if (world == null) world = GetComponent<BlockWorld>();
            if (world == null) world = FindAnyObjectByType<BlockWorld>();
        }

        private void OnEnable()
        {
            if (world != null) world.Changed += MarkDirty;
        }

        private void OnDisable()
        {
            if (world != null) world.Changed -= MarkDirty;
            if (pending) SaveNow();
        }

        private void Start()
        {
            if (world == null)
            {
                Debug.LogError("[Atelier Verse] MapAutoSave에 블록 세계가 없습니다.", this);
                enabled = false;
                return;
            }

            MapId = MapLibrary.ResolveCurrent();
            header = MapDocument.Create(mapName, world.BoundsMin, world.BoundsMax);
            if (loadOnStart) LoadOrAdopt();
            ShowScenery();
            MapChanged?.Invoke();
        }

        private void Update()
        {
            if (pending && Time.unscaledTime >= dueTime) SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && pending) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (pending) SaveNow();
        }

        /// <summary>
        /// 지금 맵의 파일이 있으면 불러와 씬의 블록을 대신하고, 없으면 지금 씬의 블록을 첫 맵으로 저장한다.
        /// 읽지 못하는 파일은 옆으로 옮겨 두고 씬의 블록으로 시작한다.
        /// </summary>
        public void LoadOrAdopt()
        {
            string path = FilePath;
            if (MapStorage.TryLoad(path, out MapDocument document, out MapFileError error))
            {
                Apply(document, path);
                return;
            }

            if (error == MapFileError.Missing)
            {
                SaveNow();
                return;
            }

            SetAsidePath = MapStorage.SetAside(path);
            Debug.LogWarning($"[Atelier Verse] 맵 파일을 읽지 못해 옆으로 옮겼습니다({error}): {SetAsidePath ?? path}", this);
            Set(SaveState.Failed, Describe(error));
            Notice.Post($"{Describe(error)}. 파일은 옆으로 옮겨 두었습니다", NoticeKind.Error);
            MarkDirty();
        }

        /// <summary>지금 바로 저장한다. 실패하면 상태를 실패로 두고 다음 변경 때 다시 시도한다.</summary>
        public bool SaveNow()
        {
            if (world == null) return false;

            MapDocument document = world.Export(header);
            try
            {
                MapStorage.Save(document, FilePath);
            }
            catch (Exception exception) when (exception is System.IO.IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 맵을 저장하지 못했습니다: {exception.Message}", this);
                pending = true;
                dueTime = Time.unscaledTime + Mathf.Max(delay, 5f);
                Set(SaveState.Failed, "저장하지 못했습니다");
                Notice.Post("맵을 저장하지 못했습니다. 잠시 뒤 다시 시도합니다", NoticeKind.Error);
                return false;
            }

            header = document;
            pending = false;
            Set(SaveState.Saved, "저장했습니다");
            return true;
        }

        /// <summary>이 기기의 맵 목록. 지금 맵의 남은 변경을 먼저 저장해, 목록의 블록 수와 시각이 지금 상태와 맞게 한다.</summary>
        public List<MapInfo> ListMaps()
        {
            if (pending) SaveNow();
            return MapLibrary.List();
        }

        /// <summary>
        /// 다른 맵을 연다. 지금 맵의 남은 변경을 먼저 저장하고, 그 맵의 블록으로 통째로 바꾼다(되돌리기 기록도 비워진다).
        /// 맵이 없거나 읽지 못하거나 지금 맵을 저장하지 못하면 바꾸지 않고 false를 돌려준다. 이미 열려 있는 맵이면 아무 일도 하지 않는다.
        /// </summary>
        public bool Open(string id)
        {
            if (world == null || !MapLibrary.IsValidId(id)) return false;
            if (id == MapId) return true;

            string path = MapLibrary.PathOf(id);
            if (!MapStorage.TryLoad(path, out MapDocument document, out MapFileError error))
            {
                Notice.Post(Describe(error), NoticeKind.Error);
                return false;
            }

            if (pending && !SaveNow())
            {
                Notice.Post(SaveFirstFailedMessage, NoticeKind.Error);
                return false;
            }

            Switch(id, document, path);
            return true;
        }

        /// <summary>
        /// 빈 맵을 새로 만들어 연다. 이름을 주지 않으면 "새 맵"이며, 같은 이름이 있으면 뒤에 번호가 붙는다.
        /// 만든 맵의 번호표를 돌려주고, 만들지 못하면 null이다.
        /// </summary>
        public string CreateNew(string name = null)
        {
            if (world == null) return null;

            if (pending && !SaveNow())
            {
                Notice.Post(SaveFirstFailedMessage, NoticeKind.Error);
                return null;
            }

            string id;
            try
            {
                id = MapLibrary.Create(name, world.BoundsMin, world.BoundsMax);
            }
            catch (Exception exception) when (exception is System.IO.IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Atelier Verse] 새 맵을 만들지 못했습니다: {exception.Message}", this);
                Notice.Post("새 맵을 만들지 못했습니다", NoticeKind.Error);
                return null;
            }

            if (id == null)
            {
                Notice.Post($"{TooManyMessage}(가장 많이 {MapLibrary.MaxMaps}개)", NoticeKind.Warning);
                return null;
            }

            return Open(id) ? id : null;
        }

        /// <summary>맵의 이름을 바꾼다. 지금 열려 있는 맵이면 바로 저장한다. 이름이 비어 있거나 바꾸지 못하면 false다.</summary>
        public bool Rename(string id, string name)
        {
            return UpdateInfo(id, name, null);
        }

        /// <summary>
        /// 맵의 이름과 설명을 바꾼다. 지금 열려 있는 맵이면 바로 저장한다. description이 null이면 설명은 그대로 둔다.
        /// 이름이 비어 있거나 바꾸지 못하면 false다.
        /// </summary>
        public bool UpdateInfo(string id, string name, string description)
        {
            string cleaned = MapLibrary.CleanName(name);
            if (!MapLibrary.IsValidId(id) || cleaned.Length == 0) return false;

            if (id != MapId) return MapLibrary.UpdateInfo(id, cleaned, description);

            string nameBefore = header.name;
            string descriptionBefore = header.description;
            header.name = cleaned;
            if (description != null) header.description = MapLibrary.CleanDescription(description);
            if (!SaveNow())
            {
                header.name = nameBefore;
                header.description = descriptionBefore;
                return false;
            }

            MapChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 지금 맵의 시작 위치를 정하고 바로 저장한다. position은 발이 닿는 자리, yaw는 바라보는 좌우 각도다.
        /// 맵의 범위(블록을 놓을 수 있는 상자) 밖이면 정하지 않고 false를 돌려준다.
        /// </summary>
        public bool SetSpawn(Vector3 position, float yaw)
        {
            if (world == null || header == null || !InsideBounds(position)) return false;

            MapSpawn before = header.spawn;
            header.spawn = MapDocument.Sanitize(new MapSpawn { custom = true, position = BlockMap.Quantize(position), yaw = Mathf.Round(yaw * 10f) / 10f });
            return SaveSpawn(before);
        }

        /// <summary>지금 맵의 시작 위치를 정하지 않은 상태(씬의 처음 자리)로 되돌리고 바로 저장한다.</summary>
        public bool ClearSpawn()
        {
            if (world == null || header == null) return false;
            if (!HasSpawn) return true;

            MapSpawn before = header.spawn;
            header.spawn = new MapSpawn();
            return SaveSpawn(before);
        }

        private bool SaveSpawn(MapSpawn before)
        {
            if (!SaveNow())
            {
                header.spawn = before;
                return false;
            }

            MapChanged?.Invoke();
            return true;
        }

        /// <summary>자리가 맵의 범위(상자) 안인지. 바닥의 높이와 꼭대기의 높이도 안으로 본다.</summary>
        private bool InsideBounds(Vector3 position)
        {
            Vector3 min = world.BoundsMin;
            Vector3 max = world.BoundsMax;
            const float slack = 0.01f;
            return position.x >= min.x - slack && position.x <= max.x + slack
                && position.y >= min.y - slack && position.y <= max.y + slack
                && position.z >= min.z - slack && position.z <= max.z + slack;
        }

        /// <summary>
        /// 맵을 지운다(파일을 휴지통 폴더로 옮긴다). 지금 열려 있는 맵을 지우면 가장 최근에 저장한 다른 맵을 열고,
        /// 다른 맵이 없으면 빈 맵을 새로 만들어 연다. 지우지 못하면 false다.
        /// </summary>
        public bool Delete(string id)
        {
            if (!MapLibrary.IsValidId(id)) return false;
            if (id != MapId) return MapLibrary.Trash(id) != null;

            // 휴지통에 마지막 상태가 남도록 남은 변경을 먼저 저장한다.
            if (pending && !SaveNow()) return false;
            if (MapLibrary.Trash(id) == null) return false;

            foreach (MapInfo other in MapLibrary.List())
            {
                string path = MapLibrary.PathOf(other.Id);
                if (!other.Readable || !MapStorage.TryLoad(path, out MapDocument document, out _)) continue;

                Switch(other.Id, document, path);
                return true;
            }

            // 남은 맵이 없다. 빈 맵으로 이어 간다. 파일을 쓰지 못해도 빈 맵으로 바꾸고 다음 저장을 기다린다.
            string fresh = MapLibrary.NewId();
            MapDocument empty = MapDocument.Create(MapLibrary.DefaultName, world.BoundsMin, world.BoundsMax);
            Switch(fresh, empty, MapLibrary.PathOf(fresh));
            SaveNow();
            return true;
        }

        /// <summary>지금 맵을 id의 맵으로 바꾸고 그 문서의 블록을 올린다.</summary>
        private void Switch(string id, MapDocument document, string path)
        {
            MapId = id;
            MapLibrary.RememberCurrent(id);
            SetAsidePath = null;
            BackupPath = null;
            Apply(document, path);
            ShowScenery();
            MapChanged?.Invoke();
        }

        /// <summary>읽은 문서를 블록 세계에 올리고 저장 표시를 맞춘다. 옛 판의 파일이면 사본을 남기고 곧 다시 저장한다.</summary>
        private void Apply(MapDocument document, string path)
        {
            header = document;
            LastLoad = world.Import(document);
            pending = false;

            if (LastLoad.Skipped > 0)
            {
                string skipped = $"블록 {LastLoad.Skipped}개를 읽지 못했습니다";
                Set(SaveState.Saved, skipped);
                Notice.Post(skipped, NoticeKind.Warning);
            }
            else
            {
                Set(SaveState.Saved, "불러왔습니다");
            }

            if (document.WasUpgraded)
            {
                BackupPath = MapStorage.Backup(path, $".v{document.LoadedVersion}.bak");
                MarkDirty();
            }
        }

        /// <summary>
        /// 씬에 처음부터 놓여 있는 꾸밈(나무, 지붕)은 블록이 아니어서 맵 파일에 들어가지 않는다.
        /// 처음부터 있던 맵에서만 보이고, 새로 만든 맵은 빈 바닥으로 시작하도록 감춘다.
        /// </summary>
        private void ShowScenery()
        {
            if (sampleScenery == null) return;

            bool show = MapId == MapLibrary.DefaultId;
            if (sampleScenery.activeSelf != show) sampleScenery.SetActive(show);
        }

        private void MarkDirty()
        {
            pending = true;
            dueTime = Time.unscaledTime + Mathf.Max(0f, delay);
            if (State != SaveState.Failed) Set(SaveState.Pending, "저장 대기 중");
        }

        private void Set(SaveState state, string message)
        {
            State = state;
            Message = message;
            Changed?.Invoke();
        }

        private static string Describe(MapFileError error)
        {
            switch (error)
            {
                case MapFileError.Missing: return "맵 파일이 없습니다";
                case MapFileError.NewerVersion: return "새 판의 맵 파일이라 읽지 못했습니다";
                case MapFileError.WrongFormat: return "이 서비스의 맵 파일이 아닙니다";
                case MapFileError.ReadFailed: return "맵 파일을 열지 못했습니다";
                default: return "맵 파일을 읽지 못했습니다";
            }
        }
    }
}
