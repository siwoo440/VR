using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 맵의 시작 위치를 보여 주는 바닥의 표식(18일차). 둥근 판 위의 화살표가 시작할 때 바라보는 쪽을 가리킨다.
    /// 만드는 동안 어디서 시작하는지 알 수 있게 늘 보인다. 충돌체가 없어 블록 놓기와 걷기를 막지 않는다.
    /// 어디에 둘지는 캐릭터의 시작 위치를 다루는 쪽(PlayerSpawn)이 알려 준다.
    /// </summary>
    public class SpawnMarker : MonoBehaviour
    {
        /// <summary>표식이 놓인 자리(시작 위치의 발이 닿는 자리).</summary>
        public Vector3 Position => transform.position;

        /// <summary>화살표가 가리키는 좌우 각도.</summary>
        public float Yaw => transform.eulerAngles.y;

        /// <summary>표식을 시작 위치에 놓는다.</summary>
        public void Show(Vector3 position, float yaw)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
