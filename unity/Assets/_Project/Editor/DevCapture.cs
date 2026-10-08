using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 개발 일지에 넣을 장면 그림을 에디터 창 없이 찍는다. 장면은 저장하지 않는다.
    /// 명령줄 예: Unity.exe -batchmode -quit -projectPath unity -executeMethod AtelierVerse.EditorTools.DevCapture.CaptureSandbox -captureOut Devlogs/Day01/sandbox.png
    /// </summary>
    public static class DevCapture
    {
        private const string SandboxScene = "Assets/_Project/Scenes/Sandbox.unity";
        private const string DefaultOutput = "Devlogs/capture.png";
        private const int Width = 1280;
        private const int Height = 720;

        public static void CaptureSandbox()
        {
            string output = ReadArgument("-captureOut") ?? DefaultOutput;
            EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);

            var holder = new GameObject("__CaptureCamera");
            var camera = holder.AddComponent<Camera>();
            holder.transform.position = new Vector3(10f, 8.5f, -12f);
            holder.transform.LookAt(new Vector3(0f, 0.6f, 0.5f));
            camera.fieldOfView = 42f;

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var picture = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                picture.Apply();

                string directory = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(output, picture.EncodeToPNG());
                Debug.Log($"[Atelier Verse] 장면 그림을 저장했습니다: {output}");
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(picture);
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        private static string ReadArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name) return arguments[i + 1];
            }

            return null;
        }
    }
}
