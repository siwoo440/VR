using UnityEngine.InputSystem;

namespace AtelierVerse.Core
{
    /// <summary>
    /// 빌드를 자동으로 확인하는 실행(-quitAfter) 동안 사람의 키보드·마우스·게임패드 입력을 받지 않게 한다(20일차).
    /// 확인하는 창은 다른 창 위에 뜨므로, 그때 다른 일을 하던 사람의 누름이 게임에 들어와 실제 맵에 블록이 놓인 일이 있었다.
    /// 머리와 손의 추적 기기는 막지 않는다(VR로 확인할 때 화면이 나와야 한다).
    /// </summary>
    public static class CheckRunInput
    {
        /// <summary>지금 사람의 입력을 막고 있는지.</summary>
        public static bool IsBlocking { get; private set; }

        /// <summary>지금 있는 기기와 나중에 꽂히는 기기를 모두 듣지 않게 한다.</summary>
        public static void Block()
        {
            if (IsBlocking) return;

            IsBlocking = true;
            foreach (InputDevice device in InputSystem.devices)
            {
                Disable(device);
            }

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        /// <summary>다시 듣게 한다.</summary>
        public static void Unblock()
        {
            if (!IsBlocking) return;

            IsBlocking = false;
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (IsUserDevice(device) && !device.enabled) InputSystem.EnableDevice(device);
            }
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected) Disable(device);
        }

        private static bool IsUserDevice(InputDevice device)
        {
            return device is Keyboard || device is Pointer || device is Gamepad || device is Joystick;
        }

        private static void Disable(InputDevice device)
        {
            if (IsUserDevice(device) && device.enabled) InputSystem.DisableDevice(device);
        }
    }
}
