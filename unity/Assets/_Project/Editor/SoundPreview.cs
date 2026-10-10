using System;
using System.IO;
using AtelierVerse.Core;
using UnityEditor;
using UnityEngine;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 코드로 만든 기본 소리를 하나의 소리 파일(WAV)로 이어 붙여 내보낸다. 게임을 켜지 않고도 소리를 차례로 들어 볼 수 있게 하기 위한 것이다.
    /// 소리는 SfxId의 순서대로, 소리마다 같은 길이의 칸에 하나씩 들어간다(몇 번째 소리인지로 어느 소리인지 안다).
    /// 메뉴: Atelier Verse/소리 미리 듣기 파일 만들기. 명령줄에서는 -soundOut 경로를 주고 ExportFromCommandLine을 부른다.
    /// </summary>
    public static class SoundPreview
    {
        /// <summary>소리 하나가 차지하는 칸의 길이(초). 소리가 끝난 뒤 다음 소리까지 조용하다.</summary>
        public const float SlotSeconds = 0.6f;

        private const string OutArgument = "-soundOut";
        private const string DefaultFileName = "sounds-preview.wav";

        [MenuItem("Atelier Verse/소리 미리 듣기 파일 만들기")]
        public static void ExportFromMenu()
        {
            string path = EditorUtility.SaveFilePanel("소리 미리 듣기 파일", string.Empty, DefaultFileName, "wav");
            if (string.IsNullOrEmpty(path)) return;

            Export(path);
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>명령줄에서 부른다. -soundOut 뒤의 경로에 파일을 쓴다.</summary>
        public static void ExportFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] != OutArgument) continue;

                Export(arguments[i + 1]);
                return;
            }

            throw new InvalidOperationException($"{OutArgument} 인자로 내보낼 파일의 경로를 주세요.");
        }

        /// <summary>모든 기본 소리를 SfxId의 순서대로 이어 붙여 16비트 한 줄기 WAV로 쓴다.</summary>
        public static void Export(string path)
        {
            int slot = Mathf.RoundToInt(SlotSeconds * SfxSynth.SampleRate);
            var samples = new float[slot * Sfx.Count];

            for (int i = 0; i < Sfx.Count; i++)
            {
                float[] sound = SfxSynth.Build((SfxId)i);
                Array.Copy(sound, 0, samples, i * slot, Mathf.Min(sound.Length, slot));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, ToWav(samples, SfxSynth.SampleRate));
            Debug.Log($"[Atelier Verse] 소리 {Sfx.Count}가지를 이어 붙여 저장했습니다({samples.Length / (float)SfxSynth.SampleRate:0.0}초): {path}");
        }

        /// <summary>소리 값(-1~1)을 16비트 한 줄기 WAV 파일의 바이트로 바꾼다.</summary>
        public static byte[] ToWav(float[] samples, int sampleRate)
        {
            const int headerSize = 44;
            const short channels = 1;
            const short bitsPerSample = 16;
            int dataSize = samples.Length * (bitsPerSample / 8);

            using (var stream = new MemoryStream(headerSize + dataSize))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(headerSize - 8 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * (bitsPerSample / 8));
                writer.Write((short)(channels * (bitsPerSample / 8)));
                writer.Write(bitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                foreach (float sample in samples)
                {
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
