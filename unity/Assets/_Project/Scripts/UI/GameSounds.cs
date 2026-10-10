using AtelierVerse.Core;
using AtelierVerse.Player;
using AtelierVerse.World;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 게임에서 일어난 일을 듣고 어떤 소리를 낼지 정하는 곳(19일차). 소리를 정하는 일을 이 한곳에 모아,
    /// 블록 놓기·캐릭터의 몸·화면의 코드는 무슨 일이 일어났는지만 알리고 소리는 알지 못하게 한다.
    /// 자리가 있는 일(블록, 발소리)은 그 자리에서 나는 소리로, 화면의 일(단추, 창, 알림)은 자리 없는 소리로 올린다.
    /// 자리 없는 소리는 한 프레임에 하나만 낸다. 단추를 눌러 창이 닫히면서 부품이 골라지는 것처럼 여러 일이 한꺼번에 일어나면
    /// 가장 뜻이 큰 소리 하나만 남긴다.
    /// </summary>
    [RequireComponent(typeof(GameUi))]
    public class GameSounds : MonoBehaviour
    {
        // 자리 없는 소리의 차례. 앞에 있을수록 먼저다. 같은 프레임에 여럿이 올라오면 가장 앞의 것만 낸다.
        private static readonly SfxId[] Priority =
        {
            SfxId.Error, SfxId.Warn, SfxId.Shutter, SfxId.Select, SfxId.Open, SfxId.Close,
            SfxId.Undo, SfxId.Redo, SfxId.Snap, SfxId.Grab, SfxId.Release, SfxId.Rotate,
            SfxId.FlyOn, SfxId.FlyOff, SfxId.Click,
        };

        private const int NoRank = int.MaxValue;

        // 같은 종류의 블록 소리 사이에 두는 가장 짧은 틈(초). 끌어서 여러 블록을 한꺼번에 놓거나 지울 때 소리가 쏟아지지 않게 한다.
        private const float BlockSoundGap = 0.05f;

        private GameUi ui;
        private BlockWorld world;
        private BlockBuilder builder;
        private CharacterMotor motor;
        private EditHistory history;
        private HotbarModel hotbar;
        private bool wasModalOpen;
        private bool hooked;
        private int pendingRank = NoRank;
        private readonly float[] nextBlockSoundAt = new float[4];

        private void Awake()
        {
            ui = GetComponent<GameUi>();
            world = FindAnyObjectByType<BlockWorld>();
            builder = FindAnyObjectByType<BlockBuilder>();

            LocalPlayer player = FindAnyObjectByType<LocalPlayer>();
            if (player != null) motor = player.Motor;
        }

        private void OnEnable()
        {
            history = world != null ? world.History : null;
            hotbar = ui != null && ui.Hotbar != null ? ui.Hotbar.Model : null;

            if (history != null) history.Edited += OnEdited;
            if (builder != null) builder.Happened += OnBuild;
            if (hotbar != null) hotbar.SelectionChanged += OnSelectionChanged;
            if (motor != null)
            {
                motor.Stepped += OnStepped;
                motor.Jumped += OnJumped;
                motor.Landed += OnLanded;
                motor.FlyModeChanged += OnFlyModeChanged;
            }

            Notice.Posted += OnNotice;
        }

        private void OnDisable()
        {
            Notice.Posted -= OnNotice;

            if (motor != null)
            {
                motor.FlyModeChanged -= OnFlyModeChanged;
                motor.Landed -= OnLanded;
                motor.Jumped -= OnJumped;
                motor.Stepped -= OnStepped;
            }

            if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
            if (builder != null) builder.Happened -= OnBuild;
            if (history != null) history.Edited -= OnEdited;
        }

        private void Start()
        {
            HookButtons();
            wasModalOpen = ui != null && ui.IsModalOpen;
        }

        private void LateUpdate()
        {
            bool modalOpen = ui != null && ui.IsModalOpen;
            if (modalOpen != wasModalOpen)
            {
                wasModalOpen = modalOpen;
                Queue(modalOpen ? SfxId.Open : SfxId.Close);
            }

            Flush();
        }

        /// <summary>
        /// 화면의 모든 단추와 켜고 끄는 칸에 누르는 소리를 단다. 꺼져 있는 창의 것도 함께 단다.
        /// 부품 칸의 칸은 뺀다. 칸을 누르면 부품이 골라지는 소리가 따로 나기 때문이다.
        /// </summary>
        private void HookButtons()
        {
            if (hooked) return;

            hooked = true;
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInParent<HotbarView>(true) != null) continue;
                button.onClick.AddListener(OnClicked);
            }

            foreach (Toggle toggle in GetComponentsInChildren<Toggle>(true))
            {
                toggle.onValueChanged.AddListener(OnToggled);
            }
        }

        private void OnClicked()
        {
            Queue(SfxId.Click);
        }

        private void OnToggled(bool value)
        {
            Queue(SfxId.Click);
        }

        private void OnSelectionChanged(int index)
        {
            Queue(SfxId.Select);
        }

        /// <summary>맵의 블록이 바뀌었다. 놓기·지우기·칠하기·옮기기는 그 블록의 자리에서 소리가 나고, 되돌리기와 다시 실행은 자리 없이 난다.</summary>
        private void OnEdited(EditKind kind, BlockChange change)
        {
            Vector3 position = change.HasAfter ? change.After.Position : change.Before.Position;
            switch (kind)
            {
                case EditKind.Place:
                    PlayBlockSound(0, SfxId.Place, position);
                    break;
                case EditKind.Remove:
                    PlayBlockSound(1, SfxId.Remove, position);
                    break;
                case EditKind.Paint:
                    PlayBlockSound(2, SfxId.Paint, position);
                    break;
                case EditKind.Move:
                    PlayBlockSound(3, SfxId.Move, position);
                    break;
                case EditKind.Undo:
                    Queue(SfxId.Undo);
                    break;
                case EditKind.Redo:
                    Queue(SfxId.Redo);
                    break;
            }
        }

        /// <summary>블록의 자리에서 소리를 낸다. 같은 종류(slot)의 소리가 방금 났으면 이번 것은 내지 않는다.</summary>
        private void PlayBlockSound(int slot, SfxId id, Vector3 position)
        {
            float now = Time.unscaledTime;
            if (now < nextBlockSoundAt[slot]) return;

            nextBlockSoundAt[slot] = now + BlockSoundGap;
            Sfx.PlayAt(id, position);
        }

        private void OnBuild(BuildEvent happened)
        {
            switch (happened)
            {
                case BuildEvent.Rotated:
                    Queue(SfxId.Rotate);
                    break;
                case BuildEvent.Grabbed:
                    Queue(SfxId.Grab);
                    break;
                case BuildEvent.Released:
                    Queue(SfxId.Release);
                    break;
                case BuildEvent.SnapChanged:
                    Queue(SfxId.Snap);
                    break;
            }
        }

        private void OnStepped()
        {
            Sfx.PlayAt(SfxId.Step, motor.transform.position);
        }

        private void OnJumped()
        {
            Sfx.PlayAt(SfxId.Jump, motor.transform.position);
        }

        private void OnLanded(float speed)
        {
            Sfx.PlayAt(SfxId.Land, motor.transform.position);
        }

        private void OnFlyModeChanged(bool flying)
        {
            Queue(flying ? SfxId.FlyOn : SfxId.FlyOff);
        }

        /// <summary>경고와 오류 알림에만 소리를 낸다. 보통 알림은 그 알림을 부른 일(잡기, 맞추기 등)이 이미 소리를 낸다.</summary>
        private void OnNotice(string message, NoticeKind kind)
        {
            if (kind == NoticeKind.Warning) Queue(SfxId.Warn);
            else if (kind == NoticeKind.Error) Queue(SfxId.Error);
        }

        /// <summary>자리 없는 소리를 이번 프레임의 후보로 올린다. 더 앞 차례의 소리가 이미 올라와 있으면 버려진다.</summary>
        private void Queue(SfxId id)
        {
            int rank = System.Array.IndexOf(Priority, id);
            if (rank < 0) rank = Priority.Length;
            if (rank < pendingRank) pendingRank = rank;
        }

        private void Flush()
        {
            if (pendingRank == NoRank) return;

            SfxId id = pendingRank < Priority.Length ? Priority[pendingRank] : SfxId.Click;
            pendingRank = NoRank;
            Sfx.Play(id);
        }
    }
}
