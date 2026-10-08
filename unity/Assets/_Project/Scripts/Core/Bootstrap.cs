using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtelierVerse.Core
{
    /// <summary>
    /// Boot 씬의 진입점. 이후 로그인 확인과 저장 불러오기를 이곳에서 처리한 뒤 다음 씬으로 넘어간다.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string firstScene = "Sandbox";

        private void Start()
        {
            SceneManager.LoadScene(firstScene);
        }
    }
}
