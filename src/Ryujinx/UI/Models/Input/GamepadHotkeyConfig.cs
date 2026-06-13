using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;

namespace Ryujinx.Ava.UI.Models.Input
{
    public partial class GamepadHotkeyConfig : BaseModel
    {
        [ObservableProperty]
        public partial GamepadCombination ToggleVSyncMode { get; set; }

        [ObservableProperty]
        public partial GamepadCombination Screenshot { get; set; }

        [ObservableProperty]
        public partial GamepadCombination ShowUI { get; set; }

        [ObservableProperty]
        public partial GamepadCombination Pause { get; set; }

        [ObservableProperty]
        public partial GamepadCombination ToggleMute { get; set; }

        [ObservableProperty]
        public partial GamepadCombination ResScaleUp { get; set; }

        [ObservableProperty]
        public partial GamepadCombination ResScaleDown { get; set; }

        [ObservableProperty]
        public partial GamepadCombination VolumeUp { get; set; }

        [ObservableProperty]
        public partial GamepadCombination VolumeDown { get; set; }

        [ObservableProperty]
        public partial GamepadCombination CustomVSyncIntervalIncrement { get; set; }

        [ObservableProperty]
        public partial GamepadCombination CustomVSyncIntervalDecrement { get; set; }

        [ObservableProperty]
        public partial GamepadCombination TurboMode { get; set; }

        [ObservableProperty]
        public partial GamepadCombination StopEmulation { get; set; }

        public GamepadHotkeyConfig(GamepadHotkeys config)
        {
            if (config == null)
            {
                return;
            }

            ToggleVSyncMode = config.ToggleVSyncMode;
            Screenshot = config.Screenshot;
            ShowUI = config.ShowUI;
            Pause = config.Pause;
            ToggleMute = config.ToggleMute;
            ResScaleUp = config.ResScaleUp;
            ResScaleDown = config.ResScaleDown;
            VolumeUp = config.VolumeUp;
            VolumeDown = config.VolumeDown;
            CustomVSyncIntervalIncrement = config.CustomVSyncIntervalIncrement;
            CustomVSyncIntervalDecrement = config.CustomVSyncIntervalDecrement;
            TurboMode = config.TurboMode;
            StopEmulation = config.StopEmulation;
        }

        public GamepadHotkeys GetConfig() =>
            new()
            {
                ToggleVSyncMode = ToggleVSyncMode ?? new(),
                Screenshot = Screenshot ?? new(),
                ShowUI = ShowUI ?? new(),
                Pause = Pause ?? new(),
                ToggleMute = ToggleMute ?? new(),
                ResScaleUp = ResScaleUp ?? new(),
                ResScaleDown = ResScaleDown ?? new(),
                VolumeUp = VolumeUp ?? new(),
                VolumeDown = VolumeDown ?? new(),
                CustomVSyncIntervalIncrement = CustomVSyncIntervalIncrement ?? new(),
                CustomVSyncIntervalDecrement = CustomVSyncIntervalDecrement ?? new(),
                TurboMode = TurboMode ?? new(),
                StopEmulation = StopEmulation ?? new(),
            };
    }
}
