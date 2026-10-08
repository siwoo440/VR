using AtelierVerse.Core;
using UnityEditor;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 에디터에서 재생할 때 VR 화면을 켤지 고르는 메뉴. 실행 파일의 -vr과 같은 일을 하며, 이 컴퓨터의 에디터 설정에만 저장된다.
    /// 메뉴: Atelier Verse/재생할 때 VR 켜기
    /// </summary>
    public static class XrEditorMenu
    {
        private const string MenuPath = "Atelier Verse/재생할 때 VR 켜기";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(XrSession.EditorStartKey, !EditorPrefs.GetBool(XrSession.EditorStartKey, false));
        }

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(XrSession.EditorStartKey, false));
            return true;
        }
    }
}
