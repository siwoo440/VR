using TMPro;
using UnityEngine;

namespace AtelierVerse.Player
{
    /// <summary>
    /// 캐릭터 머리 위 이름표. 항상 보는 사람 쪽을 향한다.
    /// </summary>
    public class Nameplate : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private Transform viewer;

        public string DisplayName => label != null ? label.text : string.Empty;

        public void SetName(string displayName)
        {
            if (label != null) label.text = displayName;
        }

        private void LateUpdate()
        {
            if (viewer == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                viewer = main.transform;
            }

            Vector3 away = transform.position - viewer.position;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
