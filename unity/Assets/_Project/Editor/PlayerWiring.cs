using AtelierVerse.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// PC 캐릭터의 뿌리에 몸(CharacterMotor), 카메라 리그(ViewRig), PC 조작(DesktopPlayerController)을 붙이고 서로 잇는다.
    /// 캐릭터 프리팹을 만드는 셋업(1·2·9일차)이 함께 쓰므로, 캐릭터의 구성이 바뀌면 이곳만 고친다.
    /// 이미 붙어 있으면 다시 잇기만 하므로 여러 번 불러도 결과가 같다.
    /// </summary>
    internal static class PlayerWiring
    {
        public const string PivotName = "CameraPivot";

        public static DesktopPlayerController Apply(GameObject root, InputActionAsset actions, Transform pivot, Camera camera, AvatarView avatar)
        {
            CharacterMotor motor = Ensure<CharacterMotor>(root);
            ViewRig rig = Ensure<ViewRig>(root);
            DesktopPlayerController controller = Ensure<DesktopPlayerController>(root);

            Set(motor, "avatar", avatar);
            Set(rig, "pivot", pivot);
            Set(rig, "viewCamera", camera);
            Set(rig, "avatar", avatar);
            Set(controller, "actions", actions);
            return controller;
        }

        private static T Ensure<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
