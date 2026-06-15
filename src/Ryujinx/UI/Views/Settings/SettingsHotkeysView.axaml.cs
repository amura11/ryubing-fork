using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using System;
using System.Linq;
using System.Threading;
using Button = Ryujinx.Input.Button;
using Key = Ryujinx.Common.Configuration.Hid.Key;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsHotkeysView : RyujinxControl<SettingsViewModel>
    {
        private const string KeyboardPrefix = HotkeyNames.KeyboardPrefix;
        private const string GamepadPrefix = HotkeyNames.GamepadPrefix;

        private ToggleButton _currentKeyboardButton;
        private CancellationTokenSource _keyboardAssignmentSource;
        private ToggleButton _currentGamepadButton;
        private CancellationTokenSource _gamepadAssignmentSource;

        private readonly AvaloniaKeyboardDriver _avaloniaKeyboardDriver;

        public SettingsHotkeysView()
        {
            InitializeComponent();

            _avaloniaKeyboardDriver = new AvaloniaKeyboardDriver(this, KeyboardInputMode.Semantic);
            _avaloniaKeyboardDriver.KeyPressed += PhysicalKeyLabelHelper.ObserveKeyPress;

            foreach (ToggleButton button in this.GetLogicalDescendants().OfType<ToggleButton>())
            {
                if (button.Tag is not string tag)
                {
                    continue;
                }

                if (tag.StartsWith(KeyboardPrefix, StringComparison.Ordinal))
                {
                    button.IsCheckedChanged += KeyboardButton_IsCheckedChanged;
                    button.PointerPressed += KeyboardButton_RightClickUnbind;
                }
                else if (tag.StartsWith(GamepadPrefix, StringComparison.Ordinal))
                {
                    button.IsCheckedChanged += GamepadButton_IsCheckedChanged;
                    button.PointerPressed += GamepadButton_RightClickUnbind;
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_currentKeyboardButton != null && !_currentKeyboardButton.IsPointerOver)
            {
                _keyboardAssignmentSource?.Cancel();
            }

            if (_currentGamepadButton != null && !_currentGamepadButton.IsPointerOver)
            {
                _gamepadAssignmentSource?.Cancel();
            }
        }

        private void KeyboardButton_RightClickUnbind(object sender, PointerPressedEventArgs e)
        {
            if (sender is ToggleButton button && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (_currentKeyboardButton != null && _currentKeyboardButton == button)
                {
                    _keyboardAssignmentSource?.Cancel();
                    _currentKeyboardButton.IsChecked = false;
                    _currentKeyboardButton = null;

                    SetKeyboardHotkey((string)button.Tag, Key.Unbound);
                }
            }
        }

        private async void KeyboardButton_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
            {
                return;
            }

            if ((bool)button.IsChecked)
            {
                if (_currentKeyboardButton != null && _currentKeyboardButton != button)
                {
                    _keyboardAssignmentSource?.Cancel();
                    _currentKeyboardButton.IsChecked = false;
                    _currentKeyboardButton = null;
                }

                _currentKeyboardButton = button;
                _keyboardAssignmentSource = new CancellationTokenSource();

                this.Focus(NavigationMethod.Pointer);

                IKeyboard keyboard = (IKeyboard)_avaloniaKeyboardDriver.GetGamepad("0");
                KeyboardInputListener listener = new(keyboard);

                Button? result = await listener.ListenForButtonAsync(cancellationToken: _keyboardAssignmentSource.Token);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _currentKeyboardButton = null;
                    button.IsChecked = false;

                    if (result.HasValue)
                    {
                        SetKeyboardHotkey((string)button.Tag, result.Value.AsHidType<Key>());
                    }
                });
            }
            else
            {
                if (_currentKeyboardButton == button)
                {
                    _keyboardAssignmentSource?.Cancel();
                    _currentKeyboardButton = null;
                }
            }
        }

        private void GamepadButton_RightClickUnbind(object sender, PointerPressedEventArgs e)
        {
            if (sender is ToggleButton button && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (_currentGamepadButton != null && _currentGamepadButton == button)
                {
                    _gamepadAssignmentSource?.Cancel();
                    _currentGamepadButton.IsChecked = false;
                    _currentGamepadButton = null;

                    SetGamepadHotkey((string)button.Tag, GamepadCombination.Unbound);
                }
            }
        }

        private async void GamepadButton_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
            {
                return;
            }

            if ((bool)button.IsChecked)
            {
                if (_currentGamepadButton != null && _currentGamepadButton != button)
                {
                    _gamepadAssignmentSource?.Cancel();
                    _currentGamepadButton.IsChecked = false;
                    _currentGamepadButton = null;
                }

                _currentGamepadButton = button;
                _gamepadAssignmentSource = new CancellationTokenSource();

                IGamepadDriver driver = RyujinxApp.MainWindow.InputManager.GamepadDriver;
                GamepadInputListener listener = new(driver);

                Button[] result = await listener.ListenForComboAsync(cancellationToken: _gamepadAssignmentSource.Token);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _currentGamepadButton = null;
                    button.IsChecked = false;

                    if (result.Length == 0)
                    {
                        return;
                    }

                    GamepadInputId[] buttons = Array.ConvertAll(result, b => b.AsHidType<GamepadInputId>());
                    GamepadCombination combo = new() { Buttons = buttons };

                    SetGamepadHotkey((string)button.Tag, combo);
                });
            }
            else
            {
                if (_currentGamepadButton == button)
                {
                    _gamepadAssignmentSource?.Cancel();
                    _currentGamepadButton = null;
                }
            }
        }

        private void SetKeyboardHotkey(string hotkeyName, Key value)
        {
            if (DataContext is not SettingsViewModel viewModel)
            {
                return;
            }

            switch (hotkeyName)
            {
                case HotkeyNames.KeyboardToggleVSyncMode:
                    viewModel.KeyboardHotkey.ToggleVSyncMode = value;
                    break;
                case HotkeyNames.KeyboardScreenshot:
                    viewModel.KeyboardHotkey.Screenshot = value;
                    break;
                case HotkeyNames.KeyboardShowUI:
                    viewModel.KeyboardHotkey.ShowUI = value;
                    break;
                case HotkeyNames.KeyboardPause:
                    viewModel.KeyboardHotkey.Pause = value;
                    break;
                case HotkeyNames.KeyboardToggleMute:
                    viewModel.KeyboardHotkey.ToggleMute = value;
                    break;
                case HotkeyNames.KeyboardResScaleUp:
                    viewModel.KeyboardHotkey.ResScaleUp = value;
                    break;
                case HotkeyNames.KeyboardResScaleDown:
                    viewModel.KeyboardHotkey.ResScaleDown = value;
                    break;
                case HotkeyNames.KeyboardVolumeUp:
                    viewModel.KeyboardHotkey.VolumeUp = value;
                    break;
                case HotkeyNames.KeyboardVolumeDown:
                    viewModel.KeyboardHotkey.VolumeDown = value;
                    break;
                case HotkeyNames.KeyboardCustomVSyncIntervalIncrement:
                    viewModel.KeyboardHotkey.CustomVSyncIntervalIncrement = value;
                    break;
                case HotkeyNames.KeyboardCustomVSyncIntervalDecrement:
                    viewModel.KeyboardHotkey.CustomVSyncIntervalDecrement = value;
                    break;
                case HotkeyNames.KeyboardTurboMode:
                    viewModel.KeyboardHotkey.TurboMode = value;
                    break;
                case HotkeyNames.KeyboardStopEmulation:
                    viewModel.KeyboardHotkey.StopEmulation = value;
                    break;
            }
        }

        private void SetGamepadHotkey(string hotkeyName, GamepadCombination value)
        {
            if (DataContext is not SettingsViewModel viewModel)
            {
                return;
            }

            switch (hotkeyName)
            {
                case HotkeyNames.GamepadToggleVSyncMode:
                    viewModel.GamepadHotkey.ToggleVSyncMode = value;
                    break;
                case HotkeyNames.GamepadScreenshot:
                    viewModel.GamepadHotkey.Screenshot = value;
                    break;
                case HotkeyNames.GamepadShowUI:
                    viewModel.GamepadHotkey.ShowUI = value;
                    break;
                case HotkeyNames.GamepadPause:
                    viewModel.GamepadHotkey.Pause = value;
                    break;
                case HotkeyNames.GamepadToggleMute:
                    viewModel.GamepadHotkey.ToggleMute = value;
                    break;
                case HotkeyNames.GamepadResScaleUp:
                    viewModel.GamepadHotkey.ResScaleUp = value;
                    break;
                case HotkeyNames.GamepadResScaleDown:
                    viewModel.GamepadHotkey.ResScaleDown = value;
                    break;
                case HotkeyNames.GamepadVolumeUp:
                    viewModel.GamepadHotkey.VolumeUp = value;
                    break;
                case HotkeyNames.GamepadVolumeDown:
                    viewModel.GamepadHotkey.VolumeDown = value;
                    break;
                case HotkeyNames.GamepadCustomVSyncIntervalIncrement:
                    viewModel.GamepadHotkey.CustomVSyncIntervalIncrement = value;
                    break;
                case HotkeyNames.GamepadCustomVSyncIntervalDecrement:
                    viewModel.GamepadHotkey.CustomVSyncIntervalDecrement = value;
                    break;
                case HotkeyNames.GamepadTurboMode:
                    viewModel.GamepadHotkey.TurboMode = value;
                    break;
                case HotkeyNames.GamepadStopEmulation:
                    viewModel.GamepadHotkey.StopEmulation = value;
                    break;
            }
        }

        public void Dispose()
        {
            _keyboardAssignmentSource?.Cancel();
            _keyboardAssignmentSource = null;
            _currentKeyboardButton = null;

            _gamepadAssignmentSource?.Cancel();
            _gamepadAssignmentSource = null;
            _currentGamepadButton = null;

            _avaloniaKeyboardDriver.Dispose();
        }
    }

    public static class HotkeyNames
    {
        public const string KeyboardPrefix = "Keyboard";
        public const string GamepadPrefix = "Gamepad";

        public const string KeyboardToggleVSyncMode = KeyboardPrefix + "ToggleVSyncMode";
        public const string KeyboardScreenshot = KeyboardPrefix + "Screenshot";
        public const string KeyboardShowUI = KeyboardPrefix + "ShowUI";
        public const string KeyboardPause = KeyboardPrefix + "Pause";
        public const string KeyboardToggleMute = KeyboardPrefix + "ToggleMute";
        public const string KeyboardResScaleUp = KeyboardPrefix + "ResScaleUp";
        public const string KeyboardResScaleDown = KeyboardPrefix + "ResScaleDown";
        public const string KeyboardVolumeUp = KeyboardPrefix + "VolumeUp";
        public const string KeyboardVolumeDown = KeyboardPrefix + "VolumeDown";
        public const string KeyboardCustomVSyncIntervalIncrement = KeyboardPrefix + "CustomVSyncIntervalIncrement";
        public const string KeyboardCustomVSyncIntervalDecrement = KeyboardPrefix + "CustomVSyncIntervalDecrement";
        public const string KeyboardTurboMode = KeyboardPrefix + "TurboMode";
        public const string KeyboardStopEmulation = KeyboardPrefix + "StopEmulation";

        public const string GamepadToggleVSyncMode = GamepadPrefix + "ToggleVSyncMode";
        public const string GamepadScreenshot = GamepadPrefix + "Screenshot";
        public const string GamepadShowUI = GamepadPrefix + "ShowUI";
        public const string GamepadPause = GamepadPrefix + "Pause";
        public const string GamepadToggleMute = GamepadPrefix + "ToggleMute";
        public const string GamepadResScaleUp = GamepadPrefix + "ResScaleUp";
        public const string GamepadResScaleDown = GamepadPrefix + "ResScaleDown";
        public const string GamepadVolumeUp = GamepadPrefix + "VolumeUp";
        public const string GamepadVolumeDown = GamepadPrefix + "VolumeDown";
        public const string GamepadCustomVSyncIntervalIncrement = GamepadPrefix + "CustomVSyncIntervalIncrement";
        public const string GamepadCustomVSyncIntervalDecrement = GamepadPrefix + "CustomVSyncIntervalDecrement";
        public const string GamepadTurboMode = GamepadPrefix + "TurboMode";
        public const string GamepadStopEmulation = GamepadPrefix + "StopEmulation";
    }
}
