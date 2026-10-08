using TMPro;
using UnityEngine;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 같은 방에 있는 사람들의 목록. 여러 사람이 함께 들어오는 기능이 생기기 전까지는 자기 자신만 보여 준다.
    /// </summary>
    public class PeopleListView : MonoBehaviour
    {
        [SerializeField] private TMP_Text header;
        [SerializeField] private TMP_Text localName;
        [SerializeField] private string headerFormat = "사람들 {0}/{1}";

        public string HeaderText => header != null ? header.text : string.Empty;

        public void Show(string localDisplayName, int count, int capacity)
        {
            if (header != null) header.text = string.Format(headerFormat, count, capacity);
            if (localName != null) localName.text = localDisplayName;
        }
    }
}
