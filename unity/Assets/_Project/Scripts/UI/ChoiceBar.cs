using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AtelierVerse.UI
{
    /// <summary>
    /// 나란히 놓인 칸 가운데 하나를 고르는 줄(20일차). 고른 칸은 색으로 표시한다.
    /// 설정의 "창 / 전체 화면"과 "낮음 / 보통 / 높음", 설정의 갈래를 고르는 칸에 쓴다.
    /// </summary>
    public class ChoiceBar : MonoBehaviour
    {
        [SerializeField] private Button[] buttons;
        [SerializeField] private Image[] fills;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private Color selectedFill = Color.yellow;
        [SerializeField] private Color normalFill = Color.white;

        private bool hooked;

        /// <summary>사람이 다른 칸을 골랐다. 값은 고른 칸의 번호다.</summary>
        public event Action<int> Chosen;

        /// <summary>고른 칸의 번호. 아직 정하지 않았으면 -1이다.</summary>
        public int Value { get; private set; } = -1;

        public int Count => buttons != null ? buttons.Length : 0;

        private void Awake()
        {
            Hook();
        }

        /// <summary>고른 칸을 정한다. 알림은 올리지 않는다(값을 화면에 맞춰 보일 때 쓴다).</summary>
        public void SetValueWithoutNotify(int value)
        {
            // 꺼져 있는 창 안에 있어도 단추가 듣도록, 값을 처음 받을 때도 단추를 잇는다.
            Hook();
            Value = value;
            Paint();
        }

        public Button GetButton(int index)
        {
            return buttons != null && index >= 0 && index < buttons.Length ? buttons[index] : null;
        }

        public string GetLabel(int index)
        {
            return labels != null && index >= 0 && index < labels.Length && labels[index] != null ? labels[index].text : string.Empty;
        }

        private void Hook()
        {
            if (hooked || buttons == null) return;

            hooked = true;
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => Choose(index));
            }
        }

        private void Choose(int index)
        {
            if (index == Value) return;

            Value = index;
            Paint();
            Chosen?.Invoke(index);
        }

        private void Paint()
        {
            if (fills == null) return;

            for (int i = 0; i < fills.Length; i++)
            {
                if (fills[i] != null) fills[i].color = i == Value ? selectedFill : normalFill;
            }
        }
    }
}
