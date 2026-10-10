using System;
using AtelierVerse.Core;
using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵의 분위기(4판). 하늘은 정해진 것 가운데 하나를 이름으로 적고, 해는 좌우 각도와 높이(도)로 적는다.
    /// 정하지 않은 맵은 맑은 낮이고 해는 3판까지의 씬과 같은 자리에 있다.
    /// </summary>
    [Serializable]
    public class MapEnvironment
    {
        public string sky = MapSky.DefaultId;
        public float sunYaw = MapSky.DefaultSunYaw;
        public float sunPitch = MapSky.DefaultSunPitch;

        public MapEnvironment Clone()
        {
            return new MapEnvironment { sky = sky, sunYaw = sunYaw, sunPitch = sunPitch };
        }
    }

    /// <summary>하늘 하나: 하늘의 색과 햇빛의 색·세기, 둘레의 빛. 맑은 낮은 둘레의 빛을 씬의 처음 값 그대로 쓴다.</summary>
    public struct SkyPreset
    {
        /// <summary>파일에 적는 이름.</summary>
        public string id;

        /// <summary>화면에 보이는 이름.</summary>
        public string label;

        public Color sky;
        public Color sunColor;
        public float sunIntensity;

        /// <summary>둘레의 빛(그늘진 면의 밝기)을 씬의 처음 값 그대로 둘지. false이면 ambient의 색으로 고르게 비춘다.</summary>
        public bool usesSceneAmbient;

        public Color ambient;
    }

    /// <summary>
    /// 고를 수 있는 하늘과 해의 값의 범위(23일차). 맑은 낮의 값은 22일차까지의 씬과 같아서, 분위기를 정하지 않은 맵은 전과 똑같이 보인다.
    /// </summary>
    public static class MapSky
    {
        public const string DefaultId = "day";

        // 22일차까지 씬의 해가 서 있던 방향(위에서 보아 북서쪽으로 30도, 높이 50도).
        public const float DefaultSunYaw = 330f;
        public const float DefaultSunPitch = 50f;

        public const float MinSunPitch = 10f;
        public const float MaxSunPitch = 90f;

        /// <summary>화면에서 해의 방향과 높이를 바꾸는 한 단계(도).</summary>
        public const float SunYawStep = 15f;
        public const float SunPitchStep = 5f;

        public static readonly SkyPreset[] Presets =
        {
            new SkyPreset
            {
                id = DefaultId,
                label = "맑은 낮",
                sky = Color.Lerp(AtelierPalette.Ivory, AtelierPalette.Blue, 0.2f),
                sunColor = new Color(1f, 0.96f, 0.88f),
                sunIntensity = 1.2f,
                usesSceneAmbient = true,
            },
            new SkyPreset
            {
                id = "sunset",
                label = "노을",
                sky = new Color(0.98f, 0.72f, 0.52f),
                sunColor = new Color(1f, 0.62f, 0.38f),
                sunIntensity = 1.05f,
                ambient = new Color(0.56f, 0.44f, 0.44f),
            },
            new SkyPreset
            {
                id = "night",
                label = "밤",
                sky = new Color(0.07f, 0.09f, 0.18f),
                sunColor = new Color(0.62f, 0.72f, 1f),
                sunIntensity = 0.4f,
                ambient = new Color(0.17f, 0.2f, 0.31f),
            },
            new SkyPreset
            {
                id = "cloudy",
                label = "흐림",
                sky = new Color(0.74f, 0.77f, 0.8f),
                sunColor = new Color(0.92f, 0.94f, 0.96f),
                sunIntensity = 0.7f,
                ambient = new Color(0.55f, 0.57f, 0.6f),
            },
        };

        /// <summary>하늘의 이름이 몇 번째인지. 모르는 이름이면 -1이다.</summary>
        public static int IndexOf(string id)
        {
            for (int i = 0; i < Presets.Length; i++)
            {
                if (Presets[i].id == id) return i;
            }

            return -1;
        }

        public static bool IsKnown(string id)
        {
            return IndexOf(id) >= 0;
        }

        /// <summary>이름의 하늘. 모르는 이름이면 맑은 낮이다.</summary>
        public static SkyPreset Find(string id)
        {
            int index = IndexOf(id);
            return Presets[index >= 0 ? index : 0];
        }

        /// <summary>화면에 보이는 하늘의 이름들. 고르는 칸을 만들 때 쓴다.</summary>
        public static string[] Labels()
        {
            var labels = new string[Presets.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = Presets[i].label;
            }

            return labels;
        }

        /// <summary>
        /// 읽은 분위기를 다듬는다. 없거나 모르는 하늘이면 맑은 낮, 숫자가 아닌 각도는 처음 값이 된다.
        /// 방향은 0 이상 360 미만으로, 높이는 MinSunPitch에서 MaxSunPitch 사이로 맞춘다.
        /// </summary>
        public static MapEnvironment Sanitize(MapEnvironment environment)
        {
            if (environment == null) return new MapEnvironment();

            return new MapEnvironment
            {
                sky = IsKnown(environment.sky) ? environment.sky : DefaultId,
                sunYaw = IsFinite(environment.sunYaw) ? Mathf.Repeat(environment.sunYaw, 360f) : DefaultSunYaw,
                sunPitch = IsFinite(environment.sunPitch) ? Mathf.Clamp(environment.sunPitch, MinSunPitch, MaxSunPitch) : DefaultSunPitch,
            };
        }

        /// <summary>해의 방향을 화면의 단계에 맞춘다(15도씩, 0 이상 360 미만).</summary>
        public static float SnapYaw(float yaw)
        {
            if (!IsFinite(yaw)) return DefaultSunYaw;
            return Mathf.Repeat(Mathf.Round(yaw / SunYawStep) * SunYawStep, 360f);
        }

        /// <summary>해의 높이를 화면의 단계에 맞춘다(5도씩, 범위 안으로).</summary>
        public static float SnapPitch(float pitch)
        {
            if (!IsFinite(pitch)) return DefaultSunPitch;
            return Mathf.Clamp(Mathf.Round(pitch / SunPitchStep) * SunPitchStep, MinSunPitch, MaxSunPitch);
        }

        /// <summary>해(한 방향으로 비추는 빛)가 서는 방향. 높이가 클수록 머리 위에서 비추어 그림자가 짧다.</summary>
        public static Quaternion SunRotation(float yaw, float pitch)
        {
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// 고를 수 있는 바닥의 크기(23일차). 바닥은 가운데(0, 0)를 중심으로 한 정사각형이고 높이는 모든 맵이 같다.
    /// 맵 파일의 bounds가 이 크기를 적는 자리이며, 읽을 때는 고를 수 있는 크기 가운데 가장 가까운 것으로 맞춘다.
    /// </summary>
    public static class MapSize
    {
        /// <summary>22일차까지 모든 맵의 크기였고, 새 맵의 처음 크기다.</summary>
        public const int Default = 16;

        /// <summary>블록을 쌓을 수 있는 높이.</summary>
        public const int Height = 12;

        public static readonly int[] Sizes = { 16, 24, 32 };

        public static readonly string[] Labels = { "작게 16", "보통 24", "크게 32" };

        public static int IndexOf(int size)
        {
            return Array.IndexOf(Sizes, size);
        }

        public static bool IsAllowed(int size)
        {
            return IndexOf(size) >= 0;
        }

        /// <summary>범위의 가로 길이에 가장 가까운, 고를 수 있는 크기. 범위가 없거나 바르지 않으면 처음 크기다.</summary>
        public static int FromBounds(MapBounds bounds)
        {
            if (bounds == null) return Default;

            float width = bounds.max.x - bounds.min.x;
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f) return Default;

            int best = Sizes[0];
            foreach (int size in Sizes)
            {
                if (Mathf.Abs(size - width) < Mathf.Abs(best - width)) best = size;
            }

            return best;
        }

        /// <summary>이 크기의 범위(상자)의 가장 작은 모서리.</summary>
        public static Vector3 MinOf(int size)
        {
            return new Vector3(-size * 0.5f, 0f, -size * 0.5f);
        }

        /// <summary>이 크기의 범위(상자)의 가장 큰 모서리.</summary>
        public static Vector3 MaxOf(int size)
        {
            return new Vector3(size * 0.5f, Height, size * 0.5f);
        }

        /// <summary>이 크기의 범위를 맵 파일에 적는 꼴로 만든다.</summary>
        public static MapBounds BoundsOf(int size)
        {
            return new MapBounds { min = MinOf(size), max = MaxOf(size) };
        }

        /// <summary>자리가 이 크기의 범위 안인지. 높이는 바닥부터 꼭대기까지를 본다.</summary>
        public static bool Contains(int size, Vector3 position)
        {
            Vector3 min = MinOf(size);
            Vector3 max = MaxOf(size);
            return position.x >= min.x && position.x <= max.x
                && position.y >= min.y && position.y <= max.y
                && position.z >= min.z && position.z <= max.z;
        }
    }
}
