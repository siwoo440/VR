using System.Collections.Generic;
using AtelierVerse.Core;
using AtelierVerse.World;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 맵의 분위기(하늘, 해)와 바닥 크기의 규칙 검사(23일차). 고를 수 있는 값, 읽은 값을 다듬는 방법,
    /// 그리고 정하지 않은 맵이 22일차까지의 씬과 같은 값을 갖는지를 본다. 실제로 어떻게 보이는지는 플레이 모드 테스트와 그림으로 본다.
    /// </summary>
    public class MapEnvironmentTests
    {
        private const float Close = 0.0001f;

        [Test]
        public void 하늘은_네_가지이고_첫째가_맑은_낮이다()
        {
            Assert.AreEqual(4, MapSky.Presets.Length);
            Assert.AreEqual(MapSky.DefaultId, MapSky.Presets[0].id, "정하지 않은 맵의 하늘이 고르는 칸의 첫째여야 합니다.");
            Assert.AreEqual(MapSky.Presets.Length, MapSky.Labels().Length);

            var ids = new HashSet<string>();
            foreach (SkyPreset preset in MapSky.Presets)
            {
                Assert.IsFalse(string.IsNullOrEmpty(preset.id));
                Assert.IsFalse(string.IsNullOrEmpty(preset.label));
                Assert.IsTrue(ids.Add(preset.id), $"하늘의 이름 {preset.id}이(가) 둘입니다.");
                Assert.AreEqual(preset.id, MapSky.Find(preset.id).id);
                Assert.IsTrue(MapSky.IsKnown(preset.id));
            }

            Assert.AreEqual(-1, MapSky.IndexOf("rainbow"));
            Assert.IsFalse(MapSky.IsKnown(null));
            Assert.AreEqual(MapSky.DefaultId, MapSky.Find("rainbow").id, "모르는 하늘은 맑은 낮으로 봅니다.");
        }

        [Test]
        public void 맑은_낮은_22일차까지의_씬과_같은_값이다()
        {
            SkyPreset day = MapSky.Find(MapSky.DefaultId);

            // 캐릭터의 카메라에 적혀 있던 바탕색과 씬의 해에 적혀 있던 값.
            AssertColor(Color.Lerp(AtelierPalette.Ivory, AtelierPalette.Blue, 0.2f), day.sky);
            AssertColor(new Color(1f, 0.96f, 0.88f), day.sunColor);
            Assert.AreEqual(1.2f, day.sunIntensity, Close);
            Assert.IsTrue(day.usesSceneAmbient, "맑은 낮은 둘레의 빛을 씬의 처음 값 그대로 써야 전과 같게 보입니다.");

            Quaternion before = Quaternion.Euler(50f, -30f, 0f);
            Assert.Less(Quaternion.Angle(before, MapSky.SunRotation(MapSky.DefaultSunYaw, MapSky.DefaultSunPitch)), 0.01f, "해가 서 있던 방향이 달라졌습니다.");
        }

        [Test]
        public void 다른_하늘은_둘레의_빛을_따로_갖고_밤이_가장_어둡다()
        {
            SkyPreset day = MapSky.Find("day");
            SkyPreset sunset = MapSky.Find("sunset");
            SkyPreset night = MapSky.Find("night");
            SkyPreset cloudy = MapSky.Find("cloudy");

            foreach (SkyPreset preset in new[] { sunset, night, cloudy })
            {
                Assert.IsFalse(preset.usesSceneAmbient);
                Assert.Greater(preset.ambient.grayscale, 0.1f, $"{preset.label}의 그늘이 새까맣게 보입니다.");
                Assert.Greater(preset.sunIntensity, 0f);
            }

            Assert.Less(night.sunIntensity, cloudy.sunIntensity);
            Assert.Less(cloudy.sunIntensity, day.sunIntensity);
            Assert.Less(night.sky.grayscale, cloudy.sky.grayscale);
            Assert.Less(night.ambient.grayscale, cloudy.ambient.grayscale);
            Assert.Greater(sunset.sunColor.r, sunset.sunColor.b, "노을의 햇빛은 붉은 쪽이어야 합니다.");
        }

        [Test]
        public void 읽은_분위기는_모르는_하늘과_바르지_않은_각도를_다듬는다()
        {
            MapEnvironment empty = MapSky.Sanitize(null);
            Assert.AreEqual(MapSky.DefaultId, empty.sky);
            Assert.AreEqual(MapSky.DefaultSunYaw, empty.sunYaw, Close);
            Assert.AreEqual(MapSky.DefaultSunPitch, empty.sunPitch, Close);

            MapEnvironment odd = MapSky.Sanitize(new MapEnvironment { sky = "rainbow", sunYaw = -30f, sunPitch = 400f });
            Assert.AreEqual(MapSky.DefaultId, odd.sky);
            Assert.AreEqual(330f, odd.sunYaw, Close, "방향은 0 이상 360 미만으로 맞춥니다.");
            Assert.AreEqual(MapSky.MaxSunPitch, odd.sunPitch, Close);

            MapEnvironment low = MapSky.Sanitize(new MapEnvironment { sky = "night", sunYaw = 725f, sunPitch = 0f });
            Assert.AreEqual("night", low.sky);
            Assert.AreEqual(5f, low.sunYaw, Close);
            Assert.AreEqual(MapSky.MinSunPitch, low.sunPitch, Close, "해가 땅 아래로 내려가지 않게 합니다.");

            MapEnvironment broken = MapSky.Sanitize(new MapEnvironment { sky = "sunset", sunYaw = float.NaN, sunPitch = float.PositiveInfinity });
            Assert.AreEqual("sunset", broken.sky);
            Assert.AreEqual(MapSky.DefaultSunYaw, broken.sunYaw, Close);
            Assert.AreEqual(MapSky.DefaultSunPitch, broken.sunPitch, Close);

            // 다듬은 것은 새 것이다. 원래 것을 고치지 않는다.
            var original = new MapEnvironment { sky = "rainbow" };
            MapSky.Sanitize(original);
            Assert.AreEqual("rainbow", original.sky);
        }

        [Test]
        public void 해의_방향과_높이는_화면의_단계에_맞춘다()
        {
            Assert.AreEqual(345f, MapSky.SnapYaw(338f), Close);
            Assert.AreEqual(0f, MapSky.SnapYaw(353f), Close, "한 바퀴를 넘으면 다시 0부터입니다.");
            Assert.AreEqual(330f, MapSky.SnapYaw(-30f), Close);
            Assert.AreEqual(MapSky.DefaultSunYaw, MapSky.SnapYaw(float.NaN), Close);

            Assert.AreEqual(50f, MapSky.SnapPitch(52.4f), Close);
            Assert.AreEqual(55f, MapSky.SnapPitch(53f), Close);
            Assert.AreEqual(MapSky.MinSunPitch, MapSky.SnapPitch(3f), Close);
            Assert.AreEqual(MapSky.MaxSunPitch, MapSky.SnapPitch(140f), Close);

            // 처음 값은 이미 단계 위에 있다. 화면에 올렸다가 내려도 값이 바뀌지 않는다.
            Assert.AreEqual(MapSky.DefaultSunYaw, MapSky.SnapYaw(MapSky.DefaultSunYaw), Close);
            Assert.AreEqual(MapSky.DefaultSunPitch, MapSky.SnapPitch(MapSky.DefaultSunPitch), Close);
        }

        [Test]
        public void 해가_높을수록_위에서_비춘다()
        {
            Vector3 low = MapSky.SunRotation(0f, MapSky.MinSunPitch) * Vector3.forward;
            Vector3 high = MapSky.SunRotation(0f, MapSky.MaxSunPitch) * Vector3.forward;

            Assert.Less(low.y, 0f, "햇빛은 아래로 내려와야 합니다.");
            Assert.Less(high.y, low.y, "높이가 클수록 더 곧게 내려와 그림자가 짧아집니다.");
            Assert.AreEqual(-1f, high.y, 0.001f);

            // 방향을 돌리면 빛이 가는 쪽이 따라 돈다.
            Vector3 east = MapSky.SunRotation(90f, 45f) * Vector3.forward;
            Assert.Greater(east.x, 0.5f);
        }

        [Test]
        public void 바닥은_세_가지_크기에서_고르고_처음_크기는_16이다()
        {
            CollectionAssert.AreEqual(new[] { 16, 24, 32 }, MapSize.Sizes);
            Assert.AreEqual(16, MapSize.Default);
            Assert.AreEqual(MapSize.Sizes.Length, MapSize.Labels.Length);
            Assert.AreEqual(0, MapSize.IndexOf(MapSize.Default));

            Assert.IsTrue(MapSize.IsAllowed(24));
            Assert.IsFalse(MapSize.IsAllowed(20));
            Assert.IsFalse(MapSize.IsAllowed(0));

            MapBounds bounds = MapSize.BoundsOf(24);
            Assert.AreEqual(new Vector3(-12f, 0f, -12f), bounds.min);
            Assert.AreEqual(new Vector3(12f, MapSize.Height, 12f), bounds.max);

            // 22일차까지의 범위와 같다.
            Assert.AreEqual(new Vector3(-8f, 0f, -8f), MapSize.MinOf(MapSize.Default));
            Assert.AreEqual(new Vector3(8f, 12f, 8f), MapSize.MaxOf(MapSize.Default));
        }

        [Test]
        public void 파일의_범위는_고를_수_있는_크기_가운데_가장_가까운_것으로_읽는다()
        {
            foreach (int size in MapSize.Sizes)
            {
                Assert.AreEqual(size, MapSize.FromBounds(MapSize.BoundsOf(size)));
            }

            Assert.AreEqual(16, MapSize.FromBounds(Box(19.9f)));
            Assert.AreEqual(24, MapSize.FromBounds(Box(20.1f)));
            Assert.AreEqual(32, MapSize.FromBounds(Box(30f)));
            Assert.AreEqual(32, MapSize.FromBounds(Box(500f)), "고를 수 있는 것보다 큰 범위는 가장 큰 크기가 됩니다.");
            Assert.AreEqual(16, MapSize.FromBounds(Box(5f)), "1판에서 올린 작은 범위도 처음 크기가 됩니다.");

            Assert.AreEqual(MapSize.Default, MapSize.FromBounds(null));
            Assert.AreEqual(MapSize.Default, MapSize.FromBounds(new MapBounds()));
            Assert.AreEqual(MapSize.Default, MapSize.FromBounds(Box(float.NaN)));
            Assert.AreEqual(MapSize.Default, MapSize.FromBounds(Box(-4f)));
        }

        [Test]
        public void 자리가_그_크기의_바닥_안인지_안다()
        {
            Assert.IsTrue(MapSize.Contains(16, new Vector3(7.9f, 0f, -7.9f)));
            Assert.IsFalse(MapSize.Contains(16, new Vector3(10f, 0f, 0f)));
            Assert.IsTrue(MapSize.Contains(24, new Vector3(10f, 0f, 0f)));
            Assert.IsFalse(MapSize.Contains(32, new Vector3(0f, MapSize.Height + 1f, 0f)));
            Assert.IsFalse(MapSize.Contains(32, new Vector3(0f, -0.5f, 0f)));
        }

        private static MapBounds Box(float width)
        {
            return new MapBounds { min = new Vector3(-width * 0.5f, 0f, -width * 0.5f), max = new Vector3(width * 0.5f, 12f, width * 0.5f) };
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, Close);
            Assert.AreEqual(expected.g, actual.g, Close);
            Assert.AreEqual(expected.b, actual.b, Close);
        }
    }
}
