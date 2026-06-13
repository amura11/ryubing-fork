using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
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
using System.Threading;
using Button = Ryujinx.Input.Button;
using Key = Ryujinx.Common.Configuration.Hid.Key;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsHotkeysView : RyujinxControl<SettingsViewModel>
    {
        private ButtonKeyAssigner _currentAssigner;
        private ToggleButton _currentGamepadButton;
        private CancellationTokenSource _gamepadAssignmentSource;

        private readonly AvaloniaKeyboardDriver _avaloniaKeyboardDriver;

        public SettingsHotkeysView()
        {
            InitializeComponent();

            _avaloniaKeyboardDriver = new AvaloniaKeyboardDriver(this, KeyboardInputMode.Semantic);
            _avaloniaKeyboardDriver.KeyPressed += PhysicalKeyLabelHelper.ObserveKeyPress;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!_currentAssigner?.ToggledButton?.IsPointerOver ?? false)
            {
                _currentAssigner.Cancel();
            }
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;
            bool shouldRemoveBinding = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;

            if (shouldRemoveBinding)
            {
                SetKeyboardHotkey(_currentAssigner.ToggledButton.Name, Key.Unbound);
            }

            _currentAssigner?.Cancel(shouldUnbind);

            PointerPressed -= MouseClick;
        }

        private void KeyboardButton_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
            {
                return;
            }

            if ((bool)button.IsChecked)
            {
                if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
                {
                    return;
                }

                if (_currentAssigner == null)
                {
                    _currentAssigner = new ButtonKeyAssigner(button);

                    this.Focus(NavigationMethod.Pointer);

                    PointerPressed += MouseClick;

                    IKeyboard keyboard = (IKeyboard)_avaloniaKeyboardDriver.GetGamepad("0");
                    IButtonAssigner assigner = new KeyboardKeyAssigner(keyboard);

                    _currentAssigner.ButtonAssigned += (_, be) =>
                    {
                        if (be.ButtonValue.HasValue)
                        {
                            Button buttonValue = be.ButtonValue.Value;
                            Dispatcher.UIThread.Post(() => SetKeyboardHotkey(button.Name, buttonValue.AsHidType<Key>()));
                        }
                    };

                    _currentAssigner.GetInputAndAssign(assigner, keyboard);
                }
                else
                {
                    _currentAssigner.Cancel();
                    _currentAssigner = null;
                    button.IsChecked = false;
                }
            }
            else
            {
                _currentAssigner?.Cancel();
                _currentAssigner = null;
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
                    
                    SetGamepadHotkey(button.Name, GamepadCombination.Unbound);
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
                // Cancel any in-progress assignment on a different button
                if (_currentGamepadButton != null && _currentGamepadButton != button)
                {
                    _gamepadAssignmentSource?.Cancel();
                    _currentGamepadButton.IsChecked = false;
                    _currentGamepadButton = null;
                }

                _currentGamepadButton = button;
                _gamepadAssignmentSource = new CancellationTokenSource();

                IGamepadDriver driver = RyujinxApp.MainWindow.InputManager.GamepadDriver;
                GamepadComboListener listener = new(driver);

                Button[] result = await listener.ListenAsync(cancellationToken: _gamepadAssignmentSource.Token);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Null before setting IsChecked so the unchecked branch below is a no-op
                    _currentGamepadButton = null;
                    button.IsChecked = false;

                    if (result.Length == 0)
                    {
                        return;
                    }

                    GamepadInputId[] buttons = Array.ConvertAll(result, b => b.AsHidType<GamepadInputId>());
                    GamepadCombination combo = new() { Buttons = buttons };
                    
                    SetGamepadHotkey(button.Name, combo);
                });
            }
            else
            {
                // User unchecked the button manually — cancel any in-progress assignment
                if (_currentGamepadButton == button)
                {
                    _gamepadAssignmentSource?.Cancel();
                    _currentGamepadButton = null;
                }
            }
        }
        
        private void SetKeyboardHotkey(string buttonName, Key value)
        {
            if (DataContext is not SettingsViewModel viewModel)
            {
                return;
            }

            switch (buttonName)
            {
                case "ToggleVSyncMode":
                    viewModel.KeyboardHotkey.ToggleVSyncMode = value;
                    break;
                case "Screenshot":
                    viewModel.KeyboardHotkey.Screenshot = value;
                    break;
                case "ShowUI":
                    viewModel.KeyboardHotkey.ShowUI = value;
                    break;
                case "Pause":
                    viewModel.KeyboardHotkey.Pause = value;
                    break;
                case "ToggleMute":
                    viewModel.KeyboardHotkey.ToggleMute = value;
                    break;
                case "ResScaleUp":
                    viewModel.KeyboardHotkey.ResScaleUp = value;
                    break;
                case "ResScaleDown":
                    viewModel.KeyboardHotkey.ResScaleDown = value;
                    break;
                case "VolumeUp":
                    viewModel.KeyboardHotkey.VolumeUp = value;
                    break;
                case "VolumeDown":
                    viewModel.KeyboardHotkey.VolumeDown = value;
                    break;
                case "CustomVSyncIntervalIncrement":
                    viewModel.KeyboardHotkey.CustomVSyncIntervalIncrement = value;
                    break;
                case "CustomVSyncIntervalDecrement":
                    viewModel.KeyboardHotkey.CustomVSyncIntervalDecrement = value;
                    break;
                case "TurboMode":
                    viewModel.KeyboardHotkey.TurboMode = value;
                    break;
                case "StopEmulation":
                    viewModel.KeyboardHotkey.StopEmulation = value;
                    break;
            }
        }
        
        private void SetGamepadHotkey(string buttonName, GamepadCombination value)
        {
            if (DataContext is not SettingsViewModel viewModel)
            {
                return;
            }

            switch (buttonName)
            {
                case "GamepadToggleVSyncMode":
                    viewModel.GamepadHotkey.ToggleVSyncMode = value;
                    break;
                case "GamepadScreenshot":
                    viewModel.GamepadHotkey.Screenshot = value;
                    break;
                case "GamepadShowUI":
                    viewModel.GamepadHotkey.ShowUI = value;
                    break;
                case "GamepadPause":
                    viewModel.GamepadHotkey.Pause = value;
                    break;
                case "GamepadToggleMute":
                    viewModel.GamepadHotkey.ToggleMute = value;
                    break;
                case "GamepadResScaleUp":
                    viewModel.GamepadHotkey.ResScaleUp = value;
                    break;
                case "GamepadResScaleDown":
                    viewModel.GamepadHotkey.ResScaleDown = value;
                    break;
                case "GamepadVolumeUp":
                    viewModel.GamepadHotkey.VolumeUp = value;
                    break;
                case "GamepadVolumeDown":
                    viewModel.GamepadHotkey.VolumeDown = value;
                    break;
                case "GamepadCustomVSyncIntervalIncrement":
                    viewModel.GamepadHotkey.CustomVSyncIntervalIncrement = value;
                    break;
                case "GamepadCustomVSyncIntervalDecrement":
                    viewModel.GamepadHotkey.CustomVSyncIntervalDecrement = value;
                    break;
                case "GamepadTurboMode":
                    viewModel.GamepadHotkey.TurboMode = value;
                    break;
                case "GamepadStopEmulation":
                    viewModel.GamepadHotkey.StopEmulation = value;
                    break;
            }
        }

        public void Dispose()
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;

            _gamepadAssignmentSource?.Cancel();
            _gamepadAssignmentSource = null;
            _currentGamepadButton = null;

            _avaloniaKeyboardDriver.Dispose();
        }
    }
}
