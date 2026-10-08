using System;
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
    /// 블록 세계를 이 기기의 맵 파일과 맞춘다. 시작할 때 파일이 있으면 불러오고, 없으면 씬의 블록을 첫 맵으로 삼아 저장한다.
    /// 블록이 바뀌면 잠시 뒤에 저장하고, 앱을 끝내거나 씬을 떠날 때 남은 변경을 저장한다.
    /// </summary>
    public class MapAutoSave : MonoBehaviour
    {
        [SerializeField] private BlockWorld world;
        [SerializeField] private float delay = 1f;
        [SerializeField] private string mapName = "시험 작업실";
        [SerializeField] private bool loadOnStart = true;

        private MapDocument header;
        private bool pending;
        private float dueTime;

        /// <summary>저장 상태나 안내 문구가 바뀌면 알린다.</summary>
        public event Action Changed;

        public SaveState State { get; private set; } = SaveState.Idle;

        /// <summary>마지막 저장·불러오기의 안내 문구. 화면에 그대로 보여 준다.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>마지막 불러오기에서 건너뛴 블록 수 등의 결과.</summary>
        public MapLoadReport LastLoad { get; private set; }

        /// <summary>마지막으로 읽지 못한 파일을 옮겨 둔 경로. 없으면 null이다.</summary>
        public string SetAsidePath { get; private set; }

        public string FilePath => MapStorage.LocalPath;

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

            header = MapDocument.Create(mapName, world.MinCell, world.MaxCell);
            if (loadOnStart) LoadOrAdopt();
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
        /// 파일이 있으면 불러와 씬의 블록을 대신하고, 없으면 지금 씬의 블록을 첫 맵으로 저장한다.
        /// 읽지 못하는 파일은 옆으로 옮겨 두고 씬의 블록으로 시작한다.
        /// </summary>
        public void LoadOrAdopt()
        {
            string path = FilePath;
            if (MapStorage.TryLoad(path, out MapDocument document, out MapFileError error))
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
                case MapFileError.NewerVersion: return "새 판의 맵 파일이라 읽지 못했습니다";
                case MapFileError.WrongFormat: return "이 서비스의 맵 파일이 아닙니다";
                case MapFileError.ReadFailed: return "맵 파일을 열지 못했습니다";
                default: return "맵 파일을 읽지 못했습니다";
            }
        }
    }
}
