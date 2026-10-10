using UnityEngine;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 올라온 소리 요청(Sfx)을 실제로 낸다(19일차). 시작할 때 소리마다 클립을 만들어 두고, 소리를 내는 부품 몇 개를 돌려 가며 쓴다.
    /// 자리가 있는 소리는 그 자리로 옮겨 3차원으로 내고(멀면 작게, 옆이면 옆에서), 자리가 없는 소리는 어디서나 같게 낸다.
    /// 발소리와 블록 소리는 낼 때마다 높낮이를 조금씩 달리해 같은 소리가 되풀이되는 느낌을 줄인다.
    /// 전체 소리 크기는 개인 설정(GameSettings.SoundVolume)을 따른다.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private const int SourceCount = 10;

        // 자리가 있는 소리가 그대로 들리는 거리와 들리지 않게 되는 거리.
        private const float NearDistance = 2.5f;
        private const float FarDistance = 28f;

        private AudioClip[] clips;
        private AudioSource[] sources;
        private int next;

        /// <summary>만들어 둔 클립의 수.</summary>
        public int ClipCount => clips != null ? clips.Length : 0;

        /// <summary>소리를 내는 부품의 수. 이보다 많은 소리가 겹치면 가장 오래된 것이 끊긴다.</summary>
        public int VoiceCount => sources != null ? sources.Length : 0;

        /// <summary>지금까지 낸 소리의 수.</summary>
        public int PlayedCount { get; private set; }

        private void Awake()
        {
            clips = new AudioClip[Sfx.Count];
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = SfxSynth.CreateClip((SfxId)i);
            }

            sources = new AudioSource[SourceCount];
            for (int i = 0; i < sources.Length; i++)
            {
                var holder = new GameObject($"SfxVoice{i + 1}");
                holder.transform.SetParent(transform, false);

                AudioSource source = holder.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = NearDistance;
                source.maxDistance = FarDistance;
                sources[i] = source;
            }
        }

        private void OnEnable()
        {
            Sfx.Played += OnPlayed;
            GameSettings.Changed += ApplyVolume;
            ApplyVolume();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= ApplyVolume;
            Sfx.Played -= OnPlayed;
        }

        private void OnDestroy()
        {
            if (clips == null) return;

            foreach (AudioClip clip in clips)
            {
                if (clip != null) Destroy(clip);
            }
        }

        /// <summary>소리의 클립. 범위 밖이면 null이다.</summary>
        public AudioClip GetClip(SfxId id)
        {
            int index = (int)id;
            return clips != null && index >= 0 && index < clips.Length ? clips[index] : null;
        }

        private void ApplyVolume()
        {
            AudioListener.volume = GameSettings.SoundVolume;
        }

        private void OnPlayed(SfxId id, Vector3? position)
        {
            AudioClip clip = GetClip(id);
            if (clip == null || sources == null) return;

            AudioSource source = NextSource();
            source.transform.position = position ?? transform.position;
            source.spatialBlend = position.HasValue ? 1f : 0f;
            source.pitch = PitchOf(id);
            source.clip = clip;
            source.Play();
            PlayedCount++;
        }

        /// <summary>쉬고 있는 부품을 찾고, 다 쓰이고 있으면 차례대로 돌려 쓴다.</summary>
        private AudioSource NextSource()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                int index = (next + i) % sources.Length;
                if (sources[index].isPlaying) continue;

                next = (index + 1) % sources.Length;
                return sources[index];
            }

            AudioSource oldest = sources[next];
            next = (next + 1) % sources.Length;
            return oldest;
        }

        /// <summary>낼 때의 높낮이. 자주 되풀이되는 소리는 조금씩 달리한다.</summary>
        private static float PitchOf(SfxId id)
        {
            switch (id)
            {
                case SfxId.Step: return Random.Range(0.88f, 1.12f);
                case SfxId.Place:
                case SfxId.Remove:
                case SfxId.Paint:
                case SfxId.Move:
                case SfxId.Land: return Random.Range(0.95f, 1.05f);
                default: return 1f;
            }
        }
    }
}
