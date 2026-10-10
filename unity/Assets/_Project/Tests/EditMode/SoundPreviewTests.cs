using System;
using System.IO;
using AtelierVerse.Core;
using AtelierVerse.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 소리 미리 듣기 파일의 검사(19일차). 파일이 소리 파일(WAV)의 꼴을 갖추었는지, 소리가 순서대로 제 칸에 들어갔는지를 본다.
    /// </summary>
    public class SoundPreviewTests
    {
        private string directory;

        [SetUp]
        public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "AtelierVerseTests", "sound-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void 소리_값을_16비트_한_줄기_WAV로_바꾼다()
        {
            byte[] wav = SoundPreview.ToWav(new[] { 0f, 1f, -1f, 0.5f, 4f }, 44100);

            Assert.AreEqual(44 + 5 * 2, wav.Length);
            Assert.AreEqual("RIFF", Text(wav, 0));
            Assert.AreEqual(wav.Length - 8, BitConverter.ToInt32(wav, 4));
            Assert.AreEqual("WAVE", Text(wav, 8));
            Assert.AreEqual("fmt ", Text(wav, 12));
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 20), "압축하지 않은 소리(PCM)여야 합니다.");
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), "한 줄기(모노)여야 합니다.");
            Assert.AreEqual(44100, BitConverter.ToInt32(wav, 24));
            Assert.AreEqual(44100 * 2, BitConverter.ToInt32(wav, 28));
            Assert.AreEqual(2, BitConverter.ToInt16(wav, 32));
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34));
            Assert.AreEqual("data", Text(wav, 36));
            Assert.AreEqual(10, BitConverter.ToInt32(wav, 40));

            Assert.AreEqual(0, BitConverter.ToInt16(wav, 44));
            Assert.AreEqual(short.MaxValue, BitConverter.ToInt16(wav, 46));
            Assert.AreEqual(-short.MaxValue, BitConverter.ToInt16(wav, 48));
            Assert.AreEqual(short.MaxValue / 2, BitConverter.ToInt16(wav, 50), 1);
            Assert.AreEqual(short.MaxValue, BitConverter.ToInt16(wav, 52), "범위를 넘는 값은 잘라 냅니다.");
        }

        [Test]
        public void 모든_소리를_순서대로_제_칸에_넣어_파일로_쓴다()
        {
            string path = Path.Combine(directory, "preview.wav");
            SoundPreview.Export(path);

            byte[] wav = File.ReadAllBytes(path);
            int slot = Mathf.RoundToInt(SoundPreview.SlotSeconds * SfxSynth.SampleRate);
            Assert.AreEqual(44 + slot * Sfx.Count * 2, wav.Length);

            for (int i = 0; i < Sfx.Count; i++)
            {
                float[] sound = SfxSynth.Build((SfxId)i);
                Assert.Less(sound.Length, slot, $"{(SfxId)i}가 칸보다 길어 잘립니다.");

                // 칸의 앞부분은 그 소리이고, 소리가 끝난 뒤는 조용하다.
                int loudest = 0;
                for (int n = 1; n < sound.Length; n++)
                {
                    if (Mathf.Abs(sound[n]) > Mathf.Abs(sound[loudest])) loudest = n;
                }

                short expected = (short)Mathf.RoundToInt(sound[loudest] * short.MaxValue);
                Assert.AreEqual(expected, Sample(wav, i * slot + loudest), $"{(SfxId)i}가 제 칸에 없습니다.");
                Assert.AreEqual(0, Sample(wav, i * slot + sound.Length), $"{(SfxId)i} 뒤가 조용하지 않습니다.");
                Assert.AreEqual(0, Sample(wav, (i + 1) * slot - 1));
            }
        }

        private static short Sample(byte[] wav, int index)
        {
            return BitConverter.ToInt16(wav, 44 + index * 2);
        }

        private static string Text(byte[] wav, int offset)
        {
            return System.Text.Encoding.ASCII.GetString(wav, offset, 4);
        }
    }
}
