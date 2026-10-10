using System.Collections;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.UI;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 맵의 분위기와 크기를 실제로 실행해 확인한다(23일차): 하늘, 해의 방향과 높이, 바닥의 크기, 맵마다 따로 저장되는지,
    /// 분위기가 없던 3판의 파일이 전과 같은 모습으로 열리는지, 맵 정보 창의 둘째 갈래.
    /// 값이 씬에 들어갔는지를 보며, 고른 하늘이 보기 좋은지는 그림과 사람의 눈으로 보아야 한다.
    /// </summary>
    public class EnvironmentPlayTests : PlayTestBase
    {
        private const int SceneBlockCount = 19;
        private const int GoldPart = 0;
        private const float ColorClose = 0.004f;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        private BlockWorld world;
        private MapAutoSave autoSave;
        private WorldEnvironment environment;
        private NoticeBar notice;

        [UnityTest]
        public IEnumerator 분위기를_정하지_않은_맵은_22일차까지와_같은_값으로_보인다()
        {
            yield return LoadEnvironmentScene();

            Assert.AreEqual(MapSky.DefaultId, environment.SkyId);
            Assert.AreEqual(MapSky.DefaultId, autoSave.SkyId);
            AssertColor(Color.Lerp(AtelierPalette.Ivory, AtelierPalette.Blue, 0.2f), player.Rig.ViewCamera.backgroundColor, "하늘의 색이 달라졌습니다.");

            Light sun = environment.Sun;
            Assert.IsNotNull(sun, "분위기 부품에 해가 이어져 있지 않습니다.");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(50f, -30f, 0f), sun.transform.rotation), 0.05f, "해가 서 있던 방향이 달라졌습니다.");
            AssertColor(new Color(1f, 0.96f, 0.88f), sun.color, "햇빛의 색이 달라졌습니다.");
            Assert.AreEqual(1.2f, sun.intensity, 0.001f);
            Assert.AreEqual(AmbientMode.Skybox, RenderSettings.ambientMode, "맑은 낮은 둘레의 빛을 건드리지 않아야 합니다.");

            // 바닥과 범위도 그대로다.
            Assert.AreEqual(MapSize.Default, world.FloorSize);
            Assert.AreEqual(new Vector3(-8f, 0f, -8f), world.BoundsMin);
            Assert.AreEqual(new Vector3(8f, 12f, 8f), world.BoundsMax);
            Assert.AreEqual(1.6f, environment.Floor.transform.localScale.x, 0.001f);
            Assert.AreEqual(16f, FloorTiling(), 0.001f);
            Assert.AreEqual(SceneBlockCount, world.Count);
        }

        [UnityTest]
        public IEnumerator 하늘을_고르면_하늘_색과_햇빛과_둘레의_빛이_바뀐다()
        {
            yield return LoadEnvironmentScene();
            SkyPreset night = MapSky.Find("night");

            Assert.IsTrue(autoSave.SetSky("night"));
            yield return null;

            Assert.AreEqual("night", environment.SkyId);
            AssertColor(night.sky, player.Rig.ViewCamera.backgroundColor, "하늘의 색이 밤의 것이 아닙니다.");
            AssertColor(night.sunColor, environment.Sun.color, "햇빛의 색이 밤의 것이 아닙니다.");
            Assert.AreEqual(night.sunIntensity, environment.Sun.intensity, 0.001f);
            Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
            AssertColor(night.ambient, RenderSettings.ambientLight, "둘레의 빛이 밤의 것이 아닙니다.");

            // 꺼져 있는 카메라(다른 조작 방식의 것)도 같은 하늘이어야 한다.
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.clearFlags == CameraClearFlags.SolidColor) AssertColor(night.sky, camera.backgroundColor, $"{camera.name}의 하늘이 다릅니다.");
            }

            // 모르는 하늘은 받지 않고 그대로 둔다.
            Assert.IsFalse(autoSave.SetSky("rainbow"));
            Assert.AreEqual("night", autoSave.SkyId);
        }

        [UnityTest]
        public IEnumerator 맑은_낮으로_돌아오면_둘레의_빛이_처음과_같다()
        {
            yield return LoadEnvironmentScene();
            AmbientMode modeBefore = RenderSettings.ambientMode;
            SphericalHarmonicsL2 probeBefore = RenderSettings.ambientProbe;
            float intensityBefore = RenderSettings.ambientIntensity;

            autoSave.SetSky("sunset");
            yield return Frames(2);
            autoSave.SetSky("night");
            yield return Frames(2);
            autoSave.SetSky(MapSky.DefaultId);
            yield return Frames(2);

            Assert.AreEqual(modeBefore, RenderSettings.ambientMode);
            Assert.AreEqual(intensityBefore, RenderSettings.ambientIntensity, 0.0001f);
            Assert.IsTrue(probeBefore == RenderSettings.ambientProbe, "맑은 낮으로 돌아왔는데 그늘의 밝기가 처음과 다릅니다.");
            AssertColor(MapSky.Find(MapSky.DefaultId).sky, player.Rig.ViewCamera.backgroundColor);
            AssertColor(new Color(1f, 0.96f, 0.88f), environment.Sun.color);
        }

        [UnityTest]
        public IEnumerator 해의_방향과_높이를_바꾸면_해가_그쪽에_서고_화면의_단계에_맞춘다()
        {
            yield return LoadEnvironmentScene();

            Assert.IsTrue(autoSave.SetSun(120f, 25f));
            yield return null;
            Assert.Less(Quaternion.Angle(Quaternion.Euler(25f, 120f, 0f), environment.Sun.transform.rotation), 0.05f);
            Assert.AreEqual(120f, autoSave.SunYaw, 0.001f);
            Assert.AreEqual(25f, autoSave.SunPitch, 0.001f);

            // 단계 사이의 값은 가까운 단계가 된다(방향 15도, 높이 5도).
            autoSave.SetSun(127f, 3f);
            Assert.AreEqual(120f, autoSave.SunYaw, 0.001f);
            Assert.AreEqual(MapSky.MinSunPitch, autoSave.SunPitch, 0.001f, "해가 땅 아래로 내려가면 안 됩니다.");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(MapSky.MinSunPitch, 120f, 0f), environment.Sun.transform.rotation), 0.05f);
        }

        [UnityTest]
        public IEnumerator 바닥을_넓히면_넓어진_자리에_블록을_놓을_수_있다()
        {
            yield return LoadEnvironmentScene();
            var far = new Vector3(10.5f, 0.5f, 0.5f);
            Assert.AreEqual(PlaceResult.OutOfBounds, world.CheckPlace(GoldPart, far));

            Assert.AreEqual(FloorChange.Changed, autoSave.SetFloorSize(24, out int outside));
            Assert.AreEqual(0, outside);
            yield return null;

            Assert.AreEqual(24, world.FloorSize);
            Assert.AreEqual(24, autoSave.FloorSize);
            Assert.AreEqual(new Vector3(-12f, 0f, -12f), world.BoundsMin);
            Assert.AreEqual(new Vector3(12f, 12f, 12f), world.BoundsMax);
            Assert.AreEqual(24, environment.FloorSize);
            Assert.AreEqual(2.4f, environment.Floor.transform.localScale.x, 0.001f, "보이는 바닥이 넓어지지 않았습니다.");
            Assert.AreEqual(24f, FloorTiling(), 0.001f, "모눈 한 칸이 블록 한 변이 되도록 무늬를 맞춰야 합니다.");

            Assert.AreEqual(PlaceResult.Ok, world.CheckPlace(GoldPart, far));
            Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, far));
            Assert.AreEqual(SceneBlockCount + 1, world.Count, "넓히는 동안 있던 블록이 그대로여야 합니다.");

            Assert.AreEqual(FloorChange.Same, autoSave.SetFloorSize(24, out _));
            Assert.AreEqual(FloorChange.Invalid, autoSave.SetFloorSize(20, out _));
        }

        [UnityTest]
        public IEnumerator 바깥에_블록이나_시작_위치가_남으면_바닥을_줄이지_않는다()
        {
            yield return LoadEnvironmentScene();
            Assert.AreEqual(FloorChange.Changed, autoSave.SetFloorSize(32, out _));
            Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, new Vector3(14.5f, 0.5f, 0.5f), Quaternion.identity, out int farBlock));
            Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, new Vector3(-13.5f, 0.5f, 3.5f)));
            yield return null;

            Assert.AreEqual(FloorChange.BlocksOutside, autoSave.SetFloorSize(16, out int outside));
            Assert.AreEqual(2, outside, "바깥에 남는 블록의 수를 알려야 합니다.");
            Assert.AreEqual(32, world.FloorSize, "줄이지 못했으면 그대로여야 합니다.");
            Assert.IsTrue(world.Has(farBlock), "블록을 지워서 맞추면 안 됩니다.");

            // 24로 줄여도 둘 다 바깥이다. 하나를 치우면 하나만 남는다.
            Assert.AreEqual(FloorChange.BlocksOutside, autoSave.SetFloorSize(24, out outside));
            Assert.AreEqual(2, outside);

            Assert.IsTrue(world.Remove(farBlock));
            Assert.AreEqual(FloorChange.BlocksOutside, autoSave.SetFloorSize(24, out outside));
            Assert.AreEqual(1, outside);

            // 블록을 모두 치워도, 시작 위치가 바깥에 남으면 줄이지 않는다.
            Assert.IsTrue(world.TryFindNear(new Vector3(-13.5f, 0.5f, 3.5f), 0.1f, out BlockRecord other));
            Assert.IsTrue(world.Remove(other.Id));
            Assert.IsTrue(autoSave.SetSpawn(new Vector3(13f, 0f, 0f), 0f));
            Assert.AreEqual(FloorChange.SpawnOutside, autoSave.SetFloorSize(24, out _));
            Assert.AreEqual(32, world.FloorSize);

            Assert.IsTrue(autoSave.ClearSpawn());
            Assert.AreEqual(FloorChange.Changed, autoSave.SetFloorSize(24, out _));
            Assert.AreEqual(24, world.FloorSize);
        }

        [UnityTest]
        public IEnumerator 분위기와_크기는_맵마다_따로이고_다시_켜도_남는다()
        {
            yield return LoadEnvironmentScene();
            autoSave.SetSky("night");
            autoSave.SetSun(90f, 30f);
            autoSave.SetFloorSize(32, out _);
            yield return null;

            // 새 맵은 맑은 낮과 처음 크기로 시작한다(지금 맵의 것을 이어받지 않는다).
            string second = autoSave.CreateNew("둘째 맵");
            Assert.IsNotNull(second);
            yield return Frames(2);
            Assert.AreEqual(MapSky.DefaultId, environment.SkyId);
            Assert.AreEqual(MapSize.Default, world.FloorSize);
            Assert.AreEqual(1.6f, environment.Floor.transform.localScale.x, 0.001f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(50f, -30f, 0f), environment.Sun.transform.rotation), 0.05f);
            Assert.AreEqual(AmbientMode.Skybox, RenderSettings.ambientMode);

            autoSave.SetSky("cloudy");
            Assert.IsTrue(autoSave.Open(MapLibrary.DefaultId));
            yield return Frames(2);
            Assert.AreEqual("night", environment.SkyId, "처음 맵으로 돌아왔는데 그 맵의 하늘이 아닙니다.");
            Assert.AreEqual(32, world.FloorSize);
            Assert.AreEqual(3.2f, environment.Floor.transform.localScale.x, 0.001f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(30f, 90f, 0f), environment.Sun.transform.rotation), 0.05f);
            Assert.AreEqual(SceneBlockCount, world.Count);

            // 파일에는 4판으로, 분위기와 범위가 적혀 있다.
            string saved = File.ReadAllText(MapLibrary.PathOf(MapLibrary.DefaultId));
            StringAssert.Contains($"\"version\": {MapDocument.CurrentVersion}", saved);
            StringAssert.Contains("\"sky\": \"night\"", saved);
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf(second), out MapDocument other, out _));
            Assert.AreEqual("cloudy", other.environment.sky);
            Assert.AreEqual(MapSize.Default, MapSize.FromBounds(other.bounds));

            // 다시 켜도 마지막으로 연 맵의 분위기와 크기로 시작한다.
            yield return LoadEnvironmentScene();
            Assert.AreEqual(MapLibrary.DefaultId, autoSave.MapId);
            Assert.AreEqual("night", environment.SkyId);
            AssertColor(MapSky.Find("night").sky, player.Rig.ViewCamera.backgroundColor);
            Assert.AreEqual(32, world.FloorSize);
            Assert.AreEqual(3.2f, environment.Floor.transform.localScale.x, 0.001f);
            Assert.AreEqual(90f, autoSave.SunYaw, 0.001f);
        }

        [UnityTest]
        public IEnumerator 분위기가_없던_3판의_파일은_전과_같은_모습으로_열리고_사본을_남긴다()
        {
            PrepareMapDirectory();
            string old = "{\"format\":\"atelier-verse-map\",\"version\":3,\"name\":\"셋째 판의 맵\",\"description\":\"분위기가 없던 때의 맵\","
                + "\"createdAt\":\"2026-10-10T01:00:00Z\",\"updatedAt\":\"2026-10-10T02:00:00Z\","
                + "\"bounds\":{\"min\":{\"x\":-8.0,\"y\":0.0,\"z\":-8.0},\"max\":{\"x\":8.0,\"y\":12.0,\"z\":8.0}},"
                + "\"spawn\":{\"custom\":false,\"position\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"yaw\":0.0},"
                + "\"blocks\":[{\"id\":4,\"part\":\"block.blue\",\"position\":{\"x\":1.37,\"y\":0.5,\"z\":-0.82},\"rotation\":{\"x\":0.0,\"y\":30.0,\"z\":0.0}},"
                + "{\"id\":9,\"part\":\"block.gold\",\"position\":{\"x\":-2.5,\"y\":0.5,\"z\":1.5},\"rotation\":{\"x\":0.0,\"y\":0.0,\"z\":0.0}}],"
                + "\"assemblies\":[],\"rules\":[]}";
            Directory.CreateDirectory(MapStorage.Directory);
            File.WriteAllText(MapStorage.LocalPath, old);

            yield return LoadEnvironmentScene();

            Assert.AreEqual(2, world.Count, "3판의 블록이 그대로 올라와야 합니다.");
            Assert.IsTrue(world.TryGet(4, out BlockRecord blue));
            Assert.Less(Vector3.Distance(new Vector3(1.37f, 0.5f, -0.82f), blue.Position), 0.001f);
            Assert.AreEqual("셋째 판의 맵", autoSave.MapName);

            // 모습은 3판까지와 같다.
            Assert.AreEqual(MapSky.DefaultId, environment.SkyId);
            AssertColor(Color.Lerp(AtelierPalette.Ivory, AtelierPalette.Blue, 0.2f), player.Rig.ViewCamera.backgroundColor);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(50f, -30f, 0f), environment.Sun.transform.rotation), 0.05f);
            Assert.AreEqual(MapSize.Default, world.FloorSize);

            // 원래 파일은 사본으로 남고, 다시 저장된 파일은 4판이다.
            StringAssert.EndsWith(".v3.bak", autoSave.BackupPath);
            Assert.AreEqual(old, File.ReadAllText(autoSave.BackupPath), "사본의 내용이 원래 파일과 달라졌습니다.");
            Assert.IsTrue(autoSave.SaveNow());
            Assert.IsTrue(MapStorage.TryLoad(MapStorage.LocalPath, out MapDocument document, out _));
            Assert.IsFalse(document.WasUpgraded);
            Assert.AreEqual(2, document.blocks.Count);
            Assert.AreEqual(MapSky.DefaultId, document.environment.sky);
        }

        [UnityTest]
        public IEnumerator 맵_정보_창의_둘째_갈래에서_하늘과_해와_바닥을_고른다()
        {
            yield return LoadEnvironmentScene();
            yield return OpenInfo(MapLibrary.DefaultId);
            MapInfoView info = ui.MapInfo;
            Image scrim = Find<Image>(info, "Scrim");
            float scrimAlpha = scrim.color.a;

            Assert.AreEqual(MapInfoView.InfoSection, info.CurrentSection, "창은 기본 정보부터 보여야 합니다.");
            Assert.IsTrue(Find<Transform>(info, "InfoPage").gameObject.activeSelf);
            Assert.IsFalse(Find<Transform>(info, "EnvironmentPage").gameObject.activeSelf);
            Assert.Greater(scrimAlpha, 0.1f);

            Find<Button>(info, "InfoSection1").onClick.Invoke();
            yield return null;
            Assert.AreEqual(MapInfoView.EnvironmentSection, info.CurrentSection);
            Assert.IsFalse(Find<Transform>(info, "InfoPage").gameObject.activeSelf);
            Assert.IsTrue(Find<Transform>(info, "EnvironmentPage").gameObject.activeSelf);
            Assert.AreEqual(0f, scrim.color.a, 0.001f, "분위기를 고르는 동안에는 장면의 색이 그대로 보이게 막을 걷어야 합니다.");
            Assert.IsTrue(scrim.raycastTarget, "막을 걷어도 창 밖의 눌림은 막아야 합니다.");

            // 고르는 대로 저장되므로 이름과 설명의 저장 단추는 보이지 않고 눌리지도 않는다. 사본 만들기와 목록으로는 그대로다.
            CanvasGroup save = Find<CanvasGroup>(info, "SaveInfo");
            Assert.AreEqual(0f, save.alpha, 0.001f, "분위기와 크기에서는 저장 단추가 보이면 안 됩니다.");
            Assert.IsFalse(save.interactable);
            Assert.IsFalse(save.blocksRaycasts);
            Assert.IsTrue(Find<Button>(info, "Duplicate").gameObject.activeInHierarchy);
            Assert.IsTrue(Find<Button>(info, "BackToList").gameObject.activeInHierarchy);

            // 맵의 지금 값이 골라져 있다.
            ChoiceBar sky = Find<ChoiceBar>(info, "SkyBar");
            ChoiceBar floor = Find<ChoiceBar>(info, "FloorBar");
            Slider sunYaw = Find<Slider>(info, "SunYawSlider");
            Slider sunPitch = Find<Slider>(info, "SunPitchSlider");
            Assert.AreEqual(4, sky.Count);
            Assert.AreEqual("맑은 낮", sky.GetLabel(0));
            Assert.AreEqual(0, sky.Value);
            Assert.AreEqual(0, floor.Value);
            Assert.AreEqual(MapSky.DefaultSunYaw, info.ShownSunYaw, 0.001f);
            Assert.AreEqual(MapSky.DefaultSunPitch, info.ShownSunPitch, 0.001f);
            Assert.AreEqual("330°", sunYaw.transform.parent.Find("Value").GetComponent<TMPro.TMP_Text>().text);

            // 하늘
            sky.GetButton(1).onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual("sunset", autoSave.SkyId);
            Assert.AreEqual("sunset", environment.SkyId);
            Assert.AreEqual(1, sky.Value);

            // 해: 막대의 한 칸이 한 단계다.
            sunYaw.value = 8f;
            sunPitch.value = 3f;
            yield return null;
            Assert.AreEqual(120f, autoSave.SunYaw, 0.001f);
            Assert.AreEqual(25f, autoSave.SunPitch, 0.001f);
            Assert.AreEqual("120°", sunYaw.transform.parent.Find("Value").GetComponent<TMPro.TMP_Text>().text);
            Assert.AreEqual("25°", sunPitch.transform.parent.Find("Value").GetComponent<TMPro.TMP_Text>().text);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(25f, 120f, 0f), environment.Sun.transform.rotation), 0.05f);

            // 바닥
            floor.GetButton(2).onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(32, world.FloorSize);
            Assert.AreEqual("바닥을 32×32로 바꿨습니다", notice.Message);
            Assert.AreEqual(2, floor.Value);
            Assert.AreEqual(MapInfoView.EnvironmentSection, info.CurrentSection, "고른 뒤에도 보던 갈래에 있어야 합니다.");

            // 기본 정보로 돌아오면 막이 다시 깔린다.
            Find<Button>(info, "InfoSection0").onClick.Invoke();
            yield return null;
            Assert.AreEqual(scrimAlpha, scrim.color.a, 0.001f);
            Assert.AreEqual(1f, save.alpha, 0.001f, "기본 정보로 돌아오면 저장 단추가 다시 보여야 합니다.");
            Assert.IsTrue(save.interactable);

            // 창을 닫았다 열면 기본 정보부터 보이고, 고른 값은 남아 있다.
            ui.BackToMapList();
            yield return null;
            yield return OpenInfo(MapLibrary.DefaultId);
            Assert.AreEqual(MapInfoView.InfoSection, info.CurrentSection);
            Assert.AreEqual(1, sky.Value);
            Assert.AreEqual(2, floor.Value);
            Assert.AreEqual(120f, info.ShownSunYaw, 0.001f);
        }

        [UnityTest]
        public IEnumerator 줄일_수_없는_바닥을_고르면_까닭을_알리고_칸이_원래_크기로_돌아온다()
        {
            yield return LoadEnvironmentScene();
            autoSave.SetFloorSize(24, out _);
            Assert.AreEqual(PlaceResult.Ok, world.Add(GoldPart, new Vector3(10.5f, 0.5f, 0.5f)));
            yield return OpenInfo(MapLibrary.DefaultId);
            ui.MapInfo.ShowSection(MapInfoView.EnvironmentSection);
            ChoiceBar floor = Find<ChoiceBar>(ui.MapInfo, "FloorBar");
            Assert.AreEqual(1, floor.Value);

            floor.GetButton(0).onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(24, world.FloorSize);
            StringAssert.Contains("바깥에 블록 1개가 남아", notice.Message);
            Assert.AreEqual(1, floor.Value, "바꾸지 못했으면 칸이 원래 크기를 가리켜야 합니다.");
        }

        [UnityTest]
        public IEnumerator 열려_있지_않은_맵의_분위기는_보이기만_하고_바꿀_수_없다()
        {
            yield return LoadEnvironmentScene();
            MapDocument document = MapDocument.Create("밤의 성", MapSize.MinOf(32), MapSize.MaxOf(32));
            document.environment = new MapEnvironment { sky = "night", sunYaw = 45f, sunPitch = 70f };
            MapStorage.Save(document, MapLibrary.PathOf("map-night"));

            ui.OpenMapList();
            ui.OpenMapInfo("map-night");
            yield return Frames(2);
            MapInfoView info = ui.MapInfo;
            info.ShowSection(MapInfoView.EnvironmentSection);
            yield return null;

            // 그 맵의 값이 보인다.
            Assert.AreEqual(2, Find<ChoiceBar>(info, "SkyBar").Value);
            Assert.AreEqual(2, Find<ChoiceBar>(info, "FloorBar").Value);
            Assert.AreEqual(45f, info.ShownSunYaw, 0.001f);
            Assert.AreEqual(70f, info.ShownSunPitch, 0.001f);

            // 누를 수 없고, 눌러도 지금 맵과 그 맵이 바뀌지 않는다.
            CanvasGroup group = Find<CanvasGroup>(info, "EnvironmentPage");
            Assert.IsFalse(group.interactable);
            Assert.IsFalse(group.blocksRaycasts);
            Assert.Less(group.alpha, 0.9f);
            StringAssert.Contains("이 맵을 연 뒤에", Find<TMPro.TMP_Text>(info, "EnvironmentNote").text);

            Find<ChoiceBar>(info, "SkyBar").GetButton(1).onClick.Invoke();
            Find<Slider>(info, "SunYawSlider").value = 2f;
            Find<ChoiceBar>(info, "FloorBar").GetButton(0).onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(MapSky.DefaultId, autoSave.SkyId, "보던 맵이 아니라 지금 맵의 하늘이 바뀌었습니다.");
            Assert.AreEqual(MapSize.Default, world.FloorSize);
            Assert.IsTrue(MapStorage.TryLoad(MapLibrary.PathOf("map-night"), out MapDocument unchanged, out _));
            Assert.AreEqual("night", unchanged.environment.sky);
            Assert.AreEqual(45f, unchanged.environment.sunYaw, 0.001f);
        }

        [UnityTest]
        public IEnumerator 화면_그림을_찍는다()
        {
            string directory = RequireCaptureDirectory();
            yield return LoadEnvironmentScene();
            BeginCapture();

            // 같은 자리에서 하늘 넷을 찍는다. 그림자가 잘 보이게 조금 내려다본다.
            player.CaptureLook(true);
            player.SetLook(20f, 14f);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "env-day.png"));

            foreach (string id in new[] { "sunset", "night", "cloudy" })
            {
                autoSave.SetSky(id);
                yield return Frames(3);
                SaveCapture(Path.Combine(directory, $"env-{id}.png"));
            }

            // 맑은 낮으로 돌아오면 처음에 찍은 그림과 같아야 한다.
            autoSave.SetSky(MapSky.DefaultId);
            yield return Frames(3);
            string again = Path.Combine(directory, "env-day-again.png");
            SaveCapture(again);
            Assert.Less(MeanDifference(Path.Combine(directory, "env-day.png"), again), 0.004f, "맑은 낮으로 돌아왔는데 처음의 그림과 다릅니다.");
            File.Delete(again);

            // 해를 낮추고 방향을 돌리면 그림자가 길어지고 다른 쪽으로 눕는다.
            autoSave.SetSun(90f, 20f);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "env-sun-low.png"));
            autoSave.SetSun(MapSky.DefaultSunYaw, MapSky.DefaultSunPitch);

            // 넓힌 바닥: 3인칭으로 물러나 내려다본다.
            autoSave.SetFloorSize(32, out _);
            player.Rig.SetViewDistance(8f);
            player.SetLook(0f, 30f);
            yield return new WaitForSeconds(0.7f);
            SaveCapture(Path.Combine(directory, "env-floor-32.png"));
            player.Rig.SetViewDistance(0f);
            autoSave.SetFloorSize(16, out _);
            yield return new WaitForSeconds(0.6f);

            // 맵 정보 창의 둘째 갈래. 노을을 골라 창 옆으로 하늘이 보이게 한다.
            autoSave.SetSky("sunset");
            yield return OpenInfo(MapLibrary.DefaultId);
            ui.MapInfo.ShowSection(MapInfoView.EnvironmentSection);
            yield return Frames(3);
            SaveCapture(Path.Combine(directory, "map-info-environment.png"));
        }

        /// <summary>두 그림의 점마다의 색 차이의 평균(0~1). 크기가 다르면 1이다.</summary>
        private static float MeanDifference(string firstPath, string secondPath)
        {
            var first = new Texture2D(2, 2);
            var second = new Texture2D(2, 2);
            try
            {
                first.LoadImage(File.ReadAllBytes(firstPath));
                second.LoadImage(File.ReadAllBytes(secondPath));
                if (first.width != second.width || first.height != second.height) return 1f;

                Color32[] a = first.GetPixels32();
                Color32[] b = second.GetPixels32();
                double sum = 0.0;
                for (int i = 0; i < a.Length; i++)
                {
                    sum += Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b);
                }

                return (float)(sum / (a.Length * 3.0 * 255.0));
            }
            finally
            {
                Object.Destroy(first);
                Object.Destroy(second);
            }
        }

        /// <summary>바닥의 모눈 무늬가 되풀이되는 횟수.</summary>
        private float FloorTiling()
        {
            var block = new MaterialPropertyBlock();
            environment.Floor.GetPropertyBlock(block);
            return block.GetVector(BaseMapSt).x;
        }

        private static void AssertColor(Color expected, Color actual, string message = null)
        {
            bool same = Mathf.Abs(expected.r - actual.r) < ColorClose && Mathf.Abs(expected.g - actual.g) < ColorClose && Mathf.Abs(expected.b - actual.b) < ColorClose;
            Assert.IsTrue(same, $"{message ?? "색이 다릅니다."} 기대 {expected}, 실제 {actual}");
        }

        /// <summary>맵 목록 창을 열고 그 맵의 줄에 있는 정보를 눌러 맵 정보 창으로 간다.</summary>
        private IEnumerator OpenInfo(string id)
        {
            ui.OpenMapList();
            yield return Frames(2);
            ui.OpenMapInfo(id);
            yield return Frames(2);
            Assert.IsTrue(ui.IsMapInfoOpen, "맵 정보 창이 열리지 않았습니다.");
        }

        private IEnumerator LoadEnvironmentScene()
        {
            yield return LoadSandbox();

            world = Object.FindAnyObjectByType<BlockWorld>();
            autoSave = Object.FindAnyObjectByType<MapAutoSave>();
            environment = Object.FindAnyObjectByType<WorldEnvironment>();
            notice = Object.FindAnyObjectByType<NoticeBar>();
            Assert.IsNotNull(world, "Sandbox 씬에서 블록 세계를 찾을 수 없습니다.");
            Assert.IsNotNull(autoSave, "Sandbox 씬에서 자동 저장을 찾을 수 없습니다.");
            Assert.IsNotNull(environment, "Sandbox 씬에 분위기를 보이는 부품이 없습니다. 23일차 셋업을 실행하세요.");
            Assert.IsNotNull(notice, "게임 화면에 알림 띠가 없습니다.");
            yield return Frames(2);
        }
    }
}
