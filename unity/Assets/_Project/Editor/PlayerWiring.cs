using AtelierVerse.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace AtelierVerse.EditorTools
{
    /// <summary>
    /// 캐릭터의 뿌리에 조작과 리그를 붙이고 서로 잇는다. 캐릭터 프리팹을 만드는 셋업이 함께 쓰므로, 캐릭터의 구성이 바뀌면 이곳만 고친다.
    /// Apply는 몸(CharacterMotor)·PC 카메라 리그(ViewRig)·PC 조작(DesktopPlayerController)을, ApplyXr는 VR 리그와 VR 조작, 조작 방식 고르기를,
    /// ApplyLocal은 이 기기의 캐릭터를 가리키는 공통 길(LocalPlayer)과 오른손 광선을 맡는다.
    /// 이미 붙어 있으면 다시 잇기만 하므로 여러 번 불러도 결과가 같다.
    /// </summary>
    internal static class PlayerWiring
    {
        public const string PivotName = "CameraPivot";
        public const string XrOriginName = "XrOrigin";
        public const string LeftHandName = "LeftHand";
        public const string RightHandName = "RightHand";
        public const string RightPointerName = "RightPointer";

        private const float PointerWidth = 0.003f;
        private const float PointerDotSize = 0.014f;

        private static readonly Vector3 HandSize = new Vector3(0.09f, 0.06f, 0.14f);

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

        /// <summary>
        /// VR 기기용 구성을 더한다: 추적 기준점과 두 손, VR 리그, VR 조작, 조작 방식 고르기.
        /// VR 쪽은 꺼 둔 채로 저장한다. 시작할 때 PlayerModeSwitch가 켤 쪽을 고른다.
        /// </summary>
        public static PlayerModeSwitch ApplyXr(GameObject root, InputActionAsset actions, Camera camera, AvatarView avatar, Material handMaterial)
        {
            Transform origin = EnsureChild(root.transform, XrOriginName);
            origin.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Transform leftHand = EnsureHand(origin, LeftHandName, handMaterial);
            Transform rightHand = EnsureHand(origin, RightHandName, handMaterial);

            XrRig rig = Ensure<XrRig>(root);
            XrPlayerController control = Ensure<XrPlayerController>(root);
            PlayerModeSwitch modeSwitch = Ensure<PlayerModeSwitch>(root);

            Set(rig, "actions", actions);
            Set(rig, "origin", origin);
            Set(rig, "viewCamera", camera);
            Set(rig, "leftHand", leftHand);
            Set(rig, "rightHand", rightHand);
            Set(rig, "avatar", avatar);
            Set(control, "actions", actions);
            Set(control, "rig", rig);
            Set(modeSwitch, "desktopControl", root.GetComponent<DesktopPlayerController>());
            Set(modeSwitch, "viewRig", root.GetComponent<ViewRig>());
            Set(modeSwitch, "xrControl", control);
            Set(modeSwitch, "xrRig", rig);

            rig.enabled = false;
            control.enabled = false;
            leftHand.gameObject.SetActive(false);
            rightHand.gameObject.SetActive(false);
            return modeSwitch;
        }

        /// <summary>
        /// 이 기기의 캐릭터를 가리키는 공통 길(LocalPlayer)과, 오른손이 가리키는 쪽으로 나가는 광선을 더한다.
        /// 광선은 가는 선과 끝의 작은 블록이며 꺼 둔 채로 저장한다. 메뉴가 열려 있을 때 XrRig가 켠다.
        /// VR 쪽이 없는 캐릭터에는 LocalPlayer만 붙인다.
        /// </summary>
        public static LocalPlayer ApplyLocal(GameObject root, Material pointerMaterial)
        {
            LocalPlayer local = Ensure<LocalPlayer>(root);

            XrRig rig = root.GetComponent<XrRig>();
            Transform origin = root.transform.Find(XrOriginName);
            if (rig == null || origin == null) return local;

            Transform pointer = EnsureChild(origin, RightPointerName);
            pointer.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            Transform ray = EnsureChild(pointer, "Ray");
            ray.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            LineRenderer line = ray.GetComponent<LineRenderer>();
            if (line == null) line = ray.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, new Vector3(0f, 0f, XrRig.DefaultPointerLength));
            line.widthMultiplier = PointerWidth;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = pointerMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            Transform dot = pointer.Find("Dot");
            if (dot == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Dot";
                cube.layer = pointer.gameObject.layer;
                Object.DestroyImmediate(cube.GetComponent<Collider>());
                dot = cube.transform;
                dot.SetParent(pointer, false);
            }

            dot.SetLocalPositionAndRotation(new Vector3(0f, 0f, XrRig.DefaultPointerLength), Quaternion.identity);
            dot.localScale = Vector3.one * PointerDotSize;
            var dotRenderer = dot.GetComponent<MeshRenderer>();
            dotRenderer.sharedMaterial = pointerMaterial;
            dotRenderer.shadowCastingMode = ShadowCastingMode.Off;
            dotRenderer.receiveShadows = false;

            Set(rig, "rightPointer", pointer);
            Set(rig, "pointerLine", line);
            Set(rig, "pointerDot", dot);

            pointer.gameObject.SetActive(false);
            return local;
        }

        private static T Ensure<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;

            var created = new GameObject(name);
            created.layer = parent.gameObject.layer;
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        /// <summary>블록 손 하나. 손의 트랜스폼이 컨트롤러의 자세를 따르고, 그 아래의 작은 블록이 보이는 모양이다. 충돌체는 두지 않는다.</summary>
        private static Transform EnsureHand(Transform origin, string name, Material material)
        {
            Transform hand = EnsureChild(origin, name);
            Transform mesh = hand.Find("Mesh");
            if (mesh == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Mesh";
                cube.layer = hand.gameObject.layer;
                Object.DestroyImmediate(cube.GetComponent<Collider>());
                mesh = cube.transform;
                mesh.SetParent(hand, false);
            }

            mesh.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            mesh.localScale = HandSize;

            var renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            return hand;
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
