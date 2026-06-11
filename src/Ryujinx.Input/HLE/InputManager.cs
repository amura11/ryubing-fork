using System;

namespace Ryujinx.Input.HLE
{
    public class InputManager(IGamepadDriver keyboardDriver, IGamepadDriver gamepadDriver)
        : IDisposable
    {
        public IGamepadDriver KeyboardDriver { get; } = keyboardDriver;
        public IGamepadDriver GamepadDriver { get; } = gamepadDriver;
        public IGamepadDriver MouseDriver { get; private set; }

        /// <summary>
        /// Shared gamepad state snapshot manager. Polls all connected gamepads once per
        /// frame and provides cheap button/combo queries with edge detection.
        /// </summary>
        public GamepadStateManager GamepadState { get; } = new(gamepadDriver);

        public void SetMouseDriver(IGamepadDriver mouseDriver)
        {
            MouseDriver?.Dispose();

            MouseDriver = mouseDriver;
        }

        public NpadManager CreateNpadManager()
        {
            return new NpadManager(KeyboardDriver, GamepadDriver, MouseDriver);
        }

        public HotkeyManager CreateHotkeyManager()
        {
            IKeyboard keyboard = KeyboardDriver.GetGamepad("0") as IKeyboard;
            return new HotkeyManager(keyboard, GamepadDriver);
        }

        public TouchScreenManager CreateTouchScreenManager()
        {
            if (MouseDriver == null)
            {
                throw new InvalidOperationException("Mouse Driver has not been initialized.");
            }

            return new TouchScreenManager(MouseDriver.GetGamepad("0") as IMouse);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                KeyboardDriver?.Dispose();
                GamepadDriver?.Dispose();
                MouseDriver?.Dispose();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}
