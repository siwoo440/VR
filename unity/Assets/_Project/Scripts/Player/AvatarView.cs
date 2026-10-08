using UnityEngine;
using UnityEngine.Rendering;

namespace AtelierVerse.Player
{
    /// <summary>
    /// 블록 캐릭터의 겉모습. 걸을 때 팔다리를 흔들고, 1인칭일 때는 몸을 그림자만 남기고 숨긴다.
    /// </summary>
    public class AvatarView : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private GameObject nameplate;
        [SerializeField] private float swingAngle = 32f;
        [SerializeField] private float swingPerMeter = 2.6f;
        [SerializeField] private float blendSpeed = 6f;

        private bool viewApplied;
        private float phase;
        private float weight;

        /// <summary>몸을 숨긴 1인칭 상태인지.</summary>
        public bool IsFirstPerson { get; private set; }

        public void SetFirstPerson(bool firstPerson)
        {
            if (viewApplied && IsFirstPerson == firstPerson) return;

            viewApplied = true;
            IsFirstPerson = firstPerson;

            ShadowCastingMode mode = firstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (Renderer body in bodyRenderers)
            {
                if (body != null) body.shadowCastingMode = mode;
            }

            if (nameplate != null) nameplate.SetActive(!firstPerson);
        }

        /// <summary>걷는 속도에 맞춰 팔다리를 앞뒤로 흔든다. 멈추면 천천히 제자리로 돌아온다.</summary>
        public void Animate(float planarSpeed, float deltaTime)
        {
            weight = Mathf.MoveTowards(weight, planarSpeed > 0.1f ? 1f : 0f, blendSpeed * deltaTime);
            phase += planarSpeed * swingPerMeter * deltaTime;

            float angle = Mathf.Sin(phase) * swingAngle * weight;
            SetPitch(leftArm, angle);
            SetPitch(rightArm, -angle);
            SetPitch(leftLeg, -angle);
            SetPitch(rightLeg, angle);
        }

        private static void SetPitch(Transform limb, float angle)
        {
            if (limb != null) limb.localEulerAngles = new Vector3(angle, 0f, 0f);
        }
    }
}
