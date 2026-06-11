using Ryujinx.Common;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using System;

namespace Ryujinx.Input.HLE
{
    /// <summary>
    /// Represents the currently active hotkey action. Only one hotkey can be active at a time;
    /// keyboard bindings take priority over gamepad bindings.
    /// </summary>
    public enum HotkeyState
    {
        None,
        ToggleVSyncMode,
        Screenshot,
        ShowUI,
        Pause,
        ToggleMute,
        ResScaleUp,
        ResScaleDown,
        VolumeUp,
        VolumeDown,
        CustomVSyncIntervalIncrement,
        CustomVSyncIntervalDecrement,
        TurboMode,
        StopEmulation,
    }

    /// <summary>
    /// Polls keyboard and gamepad input to detect hotkey activations.
    /// Reads directly from the underlying input drivers (independent of <see cref="NpadManager"/>),
    /// so hotkeys work regardless of whether game input is blocked.
    /// </summary>
    public class HotkeyManager
    {
        private readonly IKeyboard _keyboard;
        private readonly IGamepadDriver _gamepadDriver;

        private ReactiveObject<KeyboardHotkeys> _keyboardHotkeys;
        private ReactiveObject<GamepadHotkeys> _gamepadHotkeys;

        public HotkeyManager(IKeyboard keyboard, IGamepadDriver gamepadDriver)
        {
            _keyboard = keyboard;
            _gamepadDriver = gamepadDriver;
        }

        /// <summary>
        /// Binds this manager to live hotkey configuration. Values are read from the
        /// <see cref="ReactiveObject{T}"/> on each poll, so config changes take effect immediately.
        /// </summary>
        public void Initialize(ReactiveObject<KeyboardHotkeys> keyboardHotkeys, ReactiveObject<GamepadHotkeys> gamepadHotkeys)
        {
            _keyboardHotkeys = keyboardHotkeys ?? throw new ArgumentNullException(nameof(keyboardHotkeys));
            _gamepadHotkeys = gamepadHotkeys ?? throw new ArgumentNullException(nameof(gamepadHotkeys));
        }

        /// <summary>
        /// Polls all input sources and returns the currently active hotkey.
        /// Keyboard bindings are checked first; gamepad bindings are only checked if no keyboard hotkey is active.
        /// </summary>
        public HotkeyState GetCurrentHotkeyState()
        {
            if (_keyboardHotkeys == null || _gamepadHotkeys == null)
            {
                return HotkeyState.None;
            }

            HotkeyState state = GetKeyboardHotkeyState();

            if (state == HotkeyState.None)
            {
                state = GetGamepadHotkeyState();
            }

            return state;
        }

        /// <summary>
        /// Checks whether the turbo mode button is currently held on any input source.
        /// Only meaningful when <see cref="KeyboardHotkeys.TurboModeWhileHeld"/> is enabled.
        /// </summary>
        public bool IsTurboHeld()
        {
            if (_keyboardHotkeys == null || _gamepadHotkeys == null)
            {
                return false;
            }

            KeyboardHotkeys hotkeys = _keyboardHotkeys.Value;

            if (!hotkeys.TurboModeWhileHeld)
            {
                return false;
            }

            return _keyboard.IsPressed((Key)hotkeys.TurboMode)
                || IsGamepadHotkeyActive(_gamepadHotkeys.Value.TurboMode);
        }

        private HotkeyState GetKeyboardHotkeyState()
        {
            KeyboardHotkeys hotkeys = _keyboardHotkeys.Value;

            return hotkeys switch
            {
                _ when _keyboard.IsPressed((Key)hotkeys.ToggleVSyncMode) => HotkeyState.ToggleVSyncMode,
                _ when _keyboard.IsPressed((Key)hotkeys.Screenshot) => HotkeyState.Screenshot,
                _ when _keyboard.IsPressed((Key)hotkeys.ShowUI) => HotkeyState.ShowUI,
                _ when _keyboard.IsPressed((Key)hotkeys.Pause) => HotkeyState.Pause,
                _ when _keyboard.IsPressed((Key)hotkeys.ToggleMute) => HotkeyState.ToggleMute,
                _ when _keyboard.IsPressed((Key)hotkeys.ResScaleUp) => HotkeyState.ResScaleUp,
                _ when _keyboard.IsPressed((Key)hotkeys.ResScaleDown) => HotkeyState.ResScaleDown,
                _ when _keyboard.IsPressed((Key)hotkeys.VolumeUp) => HotkeyState.VolumeUp,
                _ when _keyboard.IsPressed((Key)hotkeys.VolumeDown) => HotkeyState.VolumeDown,
                _ when _keyboard.IsPressed((Key)hotkeys.CustomVSyncIntervalIncrement) => HotkeyState.CustomVSyncIntervalIncrement,
                _ when _keyboard.IsPressed((Key)hotkeys.CustomVSyncIntervalDecrement) => HotkeyState.CustomVSyncIntervalDecrement,
                _ when _keyboard.IsPressed((Key)hotkeys.TurboMode) => HotkeyState.TurboMode,
                _ when _keyboard.IsPressed((Key)hotkeys.StopEmulation) => HotkeyState.StopEmulation,
                _ => HotkeyState.None,
            };
        }

        private HotkeyState GetGamepadHotkeyState()
        {
            GamepadHotkeys hotkeys = _gamepadHotkeys.Value;

            return hotkeys switch
            {
                _ when IsGamepadHotkeyActive(hotkeys.ToggleVSyncMode) => HotkeyState.ToggleVSyncMode,
                _ when IsGamepadHotkeyActive(hotkeys.Screenshot) => HotkeyState.Screenshot,
                _ when IsGamepadHotkeyActive(hotkeys.ShowUI) => HotkeyState.ShowUI,
                _ when IsGamepadHotkeyActive(hotkeys.Pause) => HotkeyState.Pause,
                _ when IsGamepadHotkeyActive(hotkeys.ToggleMute) => HotkeyState.ToggleMute,
                _ when IsGamepadHotkeyActive(hotkeys.ResScaleUp) => HotkeyState.ResScaleUp,
                _ when IsGamepadHotkeyActive(hotkeys.ResScaleDown) => HotkeyState.ResScaleDown,
                _ when IsGamepadHotkeyActive(hotkeys.VolumeUp) => HotkeyState.VolumeUp,
                _ when IsGamepadHotkeyActive(hotkeys.VolumeDown) => HotkeyState.VolumeDown,
                _ when IsGamepadHotkeyActive(hotkeys.CustomVSyncIntervalIncrement) => HotkeyState.CustomVSyncIntervalIncrement,
                _ when IsGamepadHotkeyActive(hotkeys.CustomVSyncIntervalDecrement) => HotkeyState.CustomVSyncIntervalDecrement,
                _ when IsGamepadHotkeyActive(hotkeys.TurboMode) => HotkeyState.TurboMode,
                _ when IsGamepadHotkeyActive(hotkeys.StopEmulation) => HotkeyState.StopEmulation,
                _ => HotkeyState.None,
            };
        }

        /// <summary>
        /// Checks whether a gamepad hotkey combination is currently active on any connected gamepad.
        /// All buttons in the combination must be pressed simultaneously on the same gamepad.
        /// </summary>
        private bool IsGamepadHotkeyActive(GamepadHotkeyCombination combo)
        {
            if (combo == null || combo.IsUnbound)
            {
                return false;
            }

            ReadOnlySpan<string> gamepadIds = _gamepadDriver.GamepadsIds;

            foreach (string id in gamepadIds)
            {
                using IGamepad gamepad = _gamepadDriver.GetGamepad(id);

                if (gamepad == null)
                {
                    continue;
                }

                if (IsComboPressed(gamepad, combo))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsComboPressed(IGamepad gamepad, GamepadHotkeyCombination combo)
        {
            foreach (GamepadInputId button in combo.Buttons)
            {
                if (!gamepad.IsPressed((GamepadButtonInputId)button))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
