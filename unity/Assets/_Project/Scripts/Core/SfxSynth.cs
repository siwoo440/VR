using System;
using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 기본 소리를 코드로 만든다(19일차). 소리 파일을 내려받지 않고, 미끄러지는 음(사인파)과 거른 잡음을 섞어
    /// "톡", "딸깍", "쓱" 같은 짧은 소리를 만든다. 나무 블록을 놓는 것처럼 짧고 부드러운 쪽으로 맞췄다.
    /// 같은 소리는 늘 같게 만들어진다(잡음의 씨앗이 정해져 있다). 장면과 무관해 편집 모드 테스트로 검사한다.
    /// 소리를 진짜 녹음으로 바꿀 때는 이 파일 대신 소리 파일을 쓰게 SfxPlayer만 고치면 된다.
    /// </summary>
    public static class SfxSynth
    {
        /// <summary>1초에 담는 소리 값의 수.</summary>
        public const int SampleRate = 44100;

        // 처음에 소리가 올라오는 시간과 끝에서 사라지는 시간. 0에서 시작해 0으로 끝나야 "틱" 하는 잡음이 나지 않는다.
        private const float AttackSeconds = 0.003f;
        private const float ReleaseSeconds = 0.008f;

        /// <summary>소리 하나의 값들(-1~1, 한 줄기)을 만든다.</summary>
        public static float[] Build(SfxId id)
        {
            switch (id)
            {
                case SfxId.Step: return Step();
                case SfxId.Jump: return Finish(Tone(0.09f, 240f, 430f, 0.7f, 2.5f), 0.32f);
                case SfxId.Land: return Land();
                case SfxId.FlyOn: return Finish(Noise(0.22f, 500f, 3200f, 1f, 1.2f, 31), 0.3f);
                case SfxId.FlyOff: return Finish(Noise(0.2f, 3000f, 400f, 1f, 2.2f, 32), 0.3f);

                case SfxId.Place: return Place(540f, 380f);
                case SfxId.Remove: return Remove();
                case SfxId.Paint: return Paint();
                case SfxId.Move: return Place(440f, 300f);
                case SfxId.Rotate: return Finish(Tone(0.022f, 1250f, 1100f, 1f, 5f), 0.26f);
                case SfxId.Grab: return Finish(Tone(0.075f, 360f, 560f, 0.9f, 2.5f), 0.4f);
                case SfxId.Release: return Finish(Tone(0.08f, 540f, 340f, 0.9f, 2.5f), 0.4f);
                case SfxId.Snap: return Notes(0.036f, 0.016f, 5f, 0.34f, 900f, 1200f);
                case SfxId.Undo: return Notes(0.055f, 0.012f, 3f, 0.38f, 660f, 495f);
                case SfxId.Redo: return Notes(0.055f, 0.012f, 3f, 0.38f, 495f, 660f);

                case SfxId.Click: return Finish(Tone(0.03f, 820f, 760f, 1f, 4.5f), 0.3f);
                case SfxId.Select: return Finish(Tone(0.045f, 700f, 930f, 1f, 3.5f), 0.34f);
                case SfxId.Open: return Finish(Tone(0.13f, 330f, 620f, 0.8f, 2.2f, 0.2f), 0.34f);
                case SfxId.Close: return Finish(Tone(0.12f, 600f, 320f, 0.8f, 2.6f, 0.2f), 0.34f);
                case SfxId.Warn: return Notes(0.07f, 0.035f, 2.5f, 0.42f, 300f, 300f);
                case SfxId.Error: return Error();
                case SfxId.Shutter: return Shutter();
                default: return Finish(Tone(0.03f, 800f, 800f, 1f, 4f), 0.3f);
            }
        }

        /// <summary>소리 하나를 재생할 수 있는 클립으로 만든다. 다 쓰면 없애야 한다.</summary>
        public static AudioClip CreateClip(SfxId id)
        {
            float[] data = Build(id);
            AudioClip clip = AudioClip.Create($"Sfx_{id}", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>소리의 길이(초).</summary>
        public static float SecondsOf(float[] data)
        {
            return data == null ? 0f : data.Length / (float)SampleRate;
        }

        // ── 소리마다의 조합 ─────────────────────────────────────────────

        /// <summary>발소리: 낮게 거른 잡음의 짧은 "툭"에 낮은 울림을 조금 섞는다.</summary>
        private static float[] Step()
        {
            float[] data = New(0.07f);
            AddNoise(data, 0f, 0.06f, 1100f, 500f, 1f, 5f, 11);
            AddTone(data, 0f, 0.07f, 105f, 80f, 0.7f, 5f, 0f);
            return Finish(data, 0.3f);
        }

        /// <summary>내려앉는 소리: 발소리보다 낮고 길다.</summary>
        private static float[] Land()
        {
            float[] data = New(0.14f);
            AddTone(data, 0f, 0.14f, 120f, 62f, 1f, 4f, 0f);
            AddNoise(data, 0f, 0.07f, 900f, 300f, 0.7f, 6f, 12);
            return Finish(data, 0.48f);
        }

        /// <summary>블록을 놓는 "톡": 아래로 조금 미끄러지는 음에 한 옥타브 위의 울림과 아주 짧은 잡음을 얹는다.</summary>
        private static float[] Place(float startHz, float endHz)
        {
            float[] data = New(0.085f);
            AddTone(data, 0f, 0.085f, startHz, endHz, 1f, 5f, 0.35f);
            AddNoise(data, 0f, 0.012f, 4200f, 2000f, 0.5f, 3f, 21);
            return Finish(data, 0.6f);
        }

        /// <summary>블록을 지우는 "폭": 크게 내려가는 음에 바람 같은 잡음을 섞는다.</summary>
        private static float[] Remove()
        {
            float[] data = New(0.12f);
            AddTone(data, 0f, 0.12f, 430f, 170f, 1f, 3.5f, 0.15f);
            AddNoise(data, 0f, 0.09f, 2400f, 600f, 0.45f, 4f, 22);
            return Finish(data, 0.55f);
        }

        /// <summary>칠하는 "쓱": 점점 밝아지는 잡음에 옅은 음을 깐다.</summary>
        private static float[] Paint()
        {
            float[] data = New(0.15f);
            AddNoise(data, 0f, 0.15f, 700f, 3600f, 1f, 2.2f, 23);
            AddTone(data, 0.02f, 0.11f, 520f, 640f, 0.25f, 2f, 0f);
            return Finish(data, 0.42f);
        }

        /// <summary>오류: 경고보다 낮고 길며, 살짝 어긋난 두 음이 함께 울려 거칠게 들린다.</summary>
        private static float[] Error()
        {
            float[] data = New(0.2f);
            AddTone(data, 0f, 0.2f, 220f, 196f, 1f, 2f, 0.3f);
            AddTone(data, 0f, 0.2f, 233f, 207f, 0.8f, 2f, 0f);
            return Finish(data, 0.46f);
        }

        /// <summary>사진을 찍는 "찰칵": 짧은 잡음 둘.</summary>
        private static float[] Shutter()
        {
            float[] data = New(0.11f);
            AddNoise(data, 0f, 0.03f, 5200f, 2600f, 1f, 4f, 41);
            AddNoise(data, 0.055f, 0.045f, 3800f, 1400f, 0.8f, 4f, 42);
            return Finish(data, 0.4f);
        }

        // ── 만드는 도구 ───────────────────────────────────────────────

        private static float[] New(float seconds)
        {
            return new float[Mathf.Max(2, Mathf.CeilToInt(seconds * SampleRate))];
        }

        /// <summary>미끄러지는 음 하나.</summary>
        private static float[] Tone(float seconds, float startHz, float endHz, float gain, float decay, float harmonic = 0f)
        {
            float[] data = New(seconds);
            AddTone(data, 0f, seconds, startHz, endHz, gain, decay, harmonic);
            return data;
        }

        /// <summary>거른 잡음 하나.</summary>
        private static float[] Noise(float seconds, float cutoffStartHz, float cutoffEndHz, float gain, float decay, int seed)
        {
            float[] data = New(seconds);
            AddNoise(data, 0f, seconds, cutoffStartHz, cutoffEndHz, gain, decay, seed);
            return data;
        }

        /// <summary>짧은 음을 사이를 두고 이어 붙인다(두 음짜리 신호). peak는 가장 큰 소리 값이다.</summary>
        private static float[] Notes(float noteSeconds, float gapSeconds, float decay, float peak, params float[] pitches)
        {
            float[] data = New(pitches.Length * noteSeconds + (pitches.Length - 1) * gapSeconds);
            for (int i = 0; i < pitches.Length; i++)
            {
                AddTone(data, i * (noteSeconds + gapSeconds), noteSeconds, pitches[i], pitches[i], 1f, decay, 0.12f);
            }

            return Finish(data, peak);
        }

        /// <summary>
        /// startHz에서 endHz로 미끄러지는 사인파를 더한다. decay가 클수록 빨리 사라진다.
        /// harmonic은 한 옥타브 위의 음을 섞는 양이며, 섞으면 소리가 조금 단단해진다.
        /// </summary>
        private static void AddTone(float[] data, float offsetSeconds, float seconds, float startHz, float endHz, float gain, float decay, float harmonic)
        {
            int start = Mathf.RoundToInt(offsetSeconds * SampleRate);
            int count = Mathf.RoundToInt(seconds * SampleRate);
            double phase = 0.0;

            for (int i = 0; i < count && start + i < data.Length; i++)
            {
                float t = i / (float)count;
                phase += 2.0 * Math.PI * Mathf.Lerp(startHz, endHz, t) / SampleRate;
                float value = (float)Math.Sin(phase) + harmonic * (float)Math.Sin(phase * 2.0);
                data[start + i] += value * Envelope(i, count, decay) * gain;
            }
        }

        /// <summary>
        /// 낮은 소리만 남기는 거름을 거친 잡음을 더한다. 거름의 높이가 cutoffStartHz에서 cutoffEndHz로 옮겨 가며,
        /// 올라가면 "쓱" 하고 밝아지고 내려가면 "푹" 하고 가라앉는다. seed가 같으면 늘 같은 잡음이다.
        /// </summary>
        private static void AddNoise(float[] data, float offsetSeconds, float seconds, float cutoffStartHz, float cutoffEndHz, float gain, float decay, int seed)
        {
            int start = Mathf.RoundToInt(offsetSeconds * SampleRate);
            int count = Mathf.RoundToInt(seconds * SampleRate);
            var random = new System.Random(seed);
            float filtered = 0f;

            for (int i = 0; i < count && start + i < data.Length; i++)
            {
                float t = i / (float)count;
                float cutoff = Mathf.Lerp(cutoffStartHz, cutoffEndHz, t);
                float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                filtered += alpha * (white - filtered);
                data[start + i] += filtered * Envelope(i, count, decay) * gain;
            }
        }

        /// <summary>소리의 크기가 시간에 따라 변하는 모양: 아주 짧게 올라와서, 뒤로 갈수록 줄어들고, 맨 끝에서 0에 닿는다.</summary>
        private static float Envelope(int index, int count, float decay)
        {
            float attack = Mathf.Clamp01(index / (AttackSeconds * SampleRate));
            float body = Mathf.Exp(-decay * index / count);
            float tail = Mathf.Clamp01((count - 1 - index) / (ReleaseSeconds * SampleRate));
            return attack * body * tail;
        }

        /// <summary>가장 큰 값이 peak가 되게 크기를 맞추고, 처음과 끝을 0으로 둔다.</summary>
        private static float[] Finish(float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            }

            if (max > 0.00001f)
            {
                float scale = peak / max;
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= scale;
                }
            }

            data[0] = 0f;
            data[data.Length - 1] = 0f;
            return data;
        }
    }
}
