using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 카메라가 보는 장면을 그림(PNG)으로 찍는다. 맵의 대표 그림을 만드는 데 쓴다(18일차).
    /// 보는 카메라는 건드리지 않고, 같은 자리에 임시 카메라를 두어 한 번만 그린다.
    /// 화면 요소(단추, 글자)처럼 그림에 들어가면 안 되는 층은 빼고 찍을 수 있다.
    /// </summary>
    public static class SceneSnapshot
    {
        public const int DefaultWidth = 480;
        public const int DefaultHeight = 270;

        /// <summary>PNG 파일의 맨 앞 여덟 바이트. 찍은 그림이 PNG인지 확인할 때 쓴다.</summary>
        public static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// source가 보는 장면을 width×height의 PNG로 찍는다. hiddenLayers에 든 층은 그리지 않는다.
        /// 찍지 못하면 null이다.
        /// </summary>
        public static byte[] CapturePng(Camera source, int width = DefaultWidth, int height = DefaultHeight, int hiddenLayers = 0)
        {
            if (source == null || width <= 0 || height <= 0) return null;

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var holder = new GameObject("SnapshotCamera");
            Texture2D picture = null;
            RenderTexture previous = RenderTexture.active;

            try
            {
                var camera = holder.AddComponent<Camera>();
                camera.CopyFrom(source);
                camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                camera.cullingMask = source.cullingMask & ~hiddenLayers;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.aspect = width / (float)height;
                camera.targetTexture = target;
                camera.Render();

                picture = new Texture2D(width, height, TextureFormat.RGB24, false);
                RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                picture.Apply();
                camera.targetTexture = null;
                return picture.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                if (picture != null) Object.Destroy(picture);
                Object.Destroy(holder);
                target.Release();
                Object.Destroy(target);
            }
        }

        /// <summary>바이트가 PNG의 머리로 시작하는지.</summary>
        public static bool IsPng(byte[] bytes)
        {
            if (bytes == null || bytes.Length < PngSignature.Length) return false;

            for (int i = 0; i < PngSignature.Length; i++)
            {
                if (bytes[i] != PngSignature[i]) return false;
            }

            return true;
        }
    }
}
