using System;
using System.Collections;
using AtelierVerse.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵의 분위기와 바닥의 크기를 씬에 보인다(23일차): 하늘의 색(카메라의 바탕색), 해의 방향·색·세기, 둘레의 빛, 바닥의 넓이.
    /// 무엇을 보일지는 지금 맵을 다루는 쪽(MapAutoSave)이 Apply로 알려 주고, 바닥의 넓이는 블록 세계의 범위를 따라간다.
    /// 맑은 낮은 씬의 처음 모습 그대로다. 다른 하늘을 고르면 둘레의 빛을 바꾸므로, 처음 값을 기억해 두었다가 맑은 낮으로 돌아올 때 되돌린다.
    /// 씬의 둘레의 빛은 씬이 열리는 동안(Awake)에는 아직 제 값이 아니다(실행 파일에서는 0이다). 그래서 Start부터 기억하고 바꾼다.
    /// 빌드를 확인할 때는 명령줄의 -sky 이름, -floor 크기로 이번 실행에 보일 하늘과 바닥만 정할 수 있다(저장하지 않고, 범위도 바꾸지 않는다).
    /// -skyCheck는 실행 중에 밤과 맑은 낮을 차례로 보였다가 돌아와, 맑은 낮의 둘레의 빛이 씬의 처음 값인지를 로그에 적는다(이것도 저장하지 않는다).
    /// </summary>
    public class WorldEnvironment : MonoBehaviour
    {
        public const string SkyArgument = "-sky";
        public const string FloorArgument = "-floor";
        public const string SkyCheckArgument = "-skyCheck";

        // 바닥으로 쓰는 판(Plane)의 한 변. 크기 1일 때 10이다.
        private const float PlaneSize = 10f;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private BlockWorld world;
        [SerializeField] private Light sun;
        [SerializeField] private Renderer floor;

        private MaterialPropertyBlock floorBlock;
        private bool started;
        private bool sceneCaptured;
        private AmbientMode sceneAmbientMode;
        private SphericalHarmonicsL2 sceneAmbientProbe;
        private Color sceneAmbientLight;
        private float sceneAmbientIntensity;
        private bool ambientChanged;
        private string previewSky;
        private int previewFloor;
        private bool skyCheck;
        private MapEnvironment shown;

        /// <summary>보이고 있는 하늘의 이름.</summary>
        public string SkyId { get; private set; } = MapSky.DefaultId;

        /// <summary>보이고 있는 하늘의 색.</summary>
        public Color SkyColor { get; private set; }

        public float SunYaw { get; private set; } = MapSky.DefaultSunYaw;

        public float SunPitch { get; private set; } = MapSky.DefaultSunPitch;

        /// <summary>보이고 있는 바닥의 한 변.</summary>
        public int FloorSize { get; private set; } = MapSize.Default;

        public Light Sun => sun;

        public Renderer Floor => floor;

        private void Awake()
        {
            if (world == null) world = FindAnyObjectByType<BlockWorld>();
            ReadPreview(Environment.GetCommandLineArgs());
        }

        private void OnEnable()
        {
            if (world != null) world.BoundsChanged += ShowFloor;
            ShowFloor();
        }

        private void OnDisable()
        {
            if (world != null) world.BoundsChanged -= ShowFloor;
        }

        private void Start()
        {
            // 여기서부터 씬의 둘레의 빛이 제 값이다. 기억해 두고, 그 전에 받은 분위기가 있으면 둘레의 빛을 이제 맞춘다.
            started = true;
            CaptureScene();
            if (shown != null) ApplyAmbient(MapSky.Find(previewSky ?? shown.sky));

            if (skyCheck) StartCoroutine(RunSkyCheck());
        }

        /// <summary>맵의 분위기를 씬에 보인다. 모르는 하늘이나 범위를 벗어난 각도는 다듬어서 쓴다.</summary>
        public void Apply(MapEnvironment environment)
        {
            environment = MapSky.Sanitize(environment);
            shown = environment;
            SkyPreset preset = MapSky.Find(previewSky ?? environment.sky);

            SkyId = preset.id;
            SkyColor = preset.sky;
            SunYaw = environment.sunYaw;
            SunPitch = environment.sunPitch;

            if (sun != null)
            {
                sun.transform.rotation = MapSky.SunRotation(SunYaw, SunPitch);
                sun.color = preset.sunColor;
                sun.intensity = preset.sunIntensity;
            }

            // 하늘은 카메라의 바탕색이다. 꺼져 있는 카메라(쓰지 않는 조작 방식의 것)도 함께 맞춰, 조작 방식을 바꿔도 같은 하늘이 보이게 한다.
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.clearFlags == CameraClearFlags.SolidColor) camera.backgroundColor = preset.sky;
            }

            // 씬이 열리는 동안에 받았으면 둘레의 빛은 Start에서 맞춘다.
            if (started) ApplyAmbient(preset);

            if (previewSky != null)
            {
                Debug.Log($"[Atelier Verse] 하늘 {preset.label}({preset.id}), 해 {SunYaw:0}°/{SunPitch:0}° (명령줄로 이번 실행만)");
            }
        }

        /// <summary>
        /// 그늘진 면을 비추는 둘레의 빛을 맞춘다. 맑은 낮은 씬의 처음 값이고, 다른 하늘은 그 하늘의 색으로 고르게 비춘다.
        /// 한 번도 바꾸지 않았으면 맑은 낮에서는 아무것도 건드리지 않는다.
        /// </summary>
        private void ApplyAmbient(SkyPreset preset)
        {
            CaptureScene();

            if (preset.usesSceneAmbient)
            {
                if (!ambientChanged) return;

                RenderSettings.ambientMode = sceneAmbientMode;
                RenderSettings.ambientLight = sceneAmbientLight;
                RenderSettings.ambientIntensity = sceneAmbientIntensity;

                // 하늘에서 계산해 둔 빛이 기억되어 있으면 그것을 그대로 돌려놓는다. 기억한 것이 비어 있으면(0이면) 돌려놓지 않고 다시 계산하게 한다.
                // 0을 돌려놓으면 그늘이 새까맣게 된다.
                if (sceneAmbientMode == AmbientMode.Skybox && sceneAmbientProbe == default(SphericalHarmonicsL2)) DynamicGI.UpdateEnvironment();
                else RenderSettings.ambientProbe = sceneAmbientProbe;

                ambientChanged = false;
                return;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = preset.ambient;
            ambientChanged = true;
        }

        /// <summary>씬의 처음 둘레의 빛을 기억해 둔다. 맑은 낮으로 돌아올 때 되돌리는 데 쓴다. Start나 그 뒤에 불러야 제 값이 잡힌다.</summary>
        private void CaptureScene()
        {
            if (sceneCaptured) return;

            sceneCaptured = true;
            sceneAmbientMode = RenderSettings.ambientMode;
            sceneAmbientProbe = RenderSettings.ambientProbe;
            sceneAmbientLight = RenderSettings.ambientLight;
            sceneAmbientIntensity = RenderSettings.ambientIntensity;
        }

        /// <summary>
        /// 바닥의 넓이를 블록 세계의 범위에 맞춘다. 판을 늘리고, 모눈 무늬가 늘어지지 않게 무늬를 되풀이하는 횟수도 함께 맞춘다
        /// (모눈 한 칸이 늘 블록 한 변이다). 재질 자산은 건드리지 않는다.
        /// </summary>
        public void ShowFloor()
        {
            int size = previewFloor > 0 ? previewFloor : (world != null ? world.FloorSize : MapSize.Default);
            FloorSize = size;
            if (floor == null) return;

            floor.transform.localScale = Vector3.one * (size / PlaneSize);

            floorBlock ??= new MaterialPropertyBlock();
            floor.GetPropertyBlock(floorBlock);
            floorBlock.SetVector(BaseMapSt, new Vector4(size, size, 0f, 0f));
            floor.SetPropertyBlock(floorBlock);
        }

        /// <summary>
        /// 하늘을 바꿨다가 돌아오는 것을 실행 파일에서 확인한다: 밤을 보이고, 맑은 낮을 보여 둘레의 빛이 씬의 처음 값인지를 로그에 적은 뒤,
        /// 보이던 하늘로 돌아온다. 맵에는 아무것도 적지 않는다. 끝난 뒤의 화면은 이 확인을 하지 않은 실행과 같아야 한다.
        /// </summary>
        private IEnumerator RunSkyCheck()
        {
            yield return new WaitForSeconds(2f);

            string before = previewSky;

            previewSky = "night";
            Apply(shown);
            Debug.Log($"[Atelier Verse] 하늘 확인 1/3: {SkyId}, 둘레의 빛 {RenderSettings.ambientMode}");
            yield return new WaitForSeconds(1f);

            previewSky = MapSky.DefaultId;
            Apply(shown);
            yield return null;

            bool same = sceneCaptured && RenderSettings.ambientMode == sceneAmbientMode && RenderSettings.ambientProbe == sceneAmbientProbe;
            bool lit = RenderSettings.ambientProbe != default(SphericalHarmonicsL2);
            Debug.Log($"[Atelier Verse] 하늘 확인 2/3: {SkyId}, 둘레의 빛 {RenderSettings.ambientMode}, 씬의 처음과 같음 {same}, 빛이 있음 {lit}");
            yield return new WaitForSeconds(1f);

            previewSky = before;
            Apply(shown);
            Debug.Log($"[Atelier Verse] 하늘 확인 3/3: 보이던 하늘({SkyId})로 돌아옴");
        }

        /// <summary>명령줄에서 이번 실행에 보일 하늘과 바닥을 읽는다. 없거나 모르는 값이면 맵의 것을 따른다.</summary>
        private void ReadPreview(string[] arguments)
        {
            string sky = Bootstrap.ReadArgument(arguments, SkyArgument);
            previewSky = MapSky.IsKnown(sky) ? sky : null;
            skyCheck = Bootstrap.HasFlag(arguments, SkyCheckArgument);

            string floorText = Bootstrap.ReadArgument(arguments, FloorArgument);
            previewFloor = int.TryParse(floorText, out int size) && MapSize.IsAllowed(size) ? size : 0;
            if (previewFloor > 0) Debug.Log($"[Atelier Verse] 바닥 {previewFloor}×{previewFloor} (명령줄로 이번 실행만, 블록을 놓을 수 있는 범위는 맵의 것 그대로)");
        }
    }
}
