namespace Ryujinx.Common.Configuration.Hid
{
    /// <summary>
    /// Configuration for gamepad hotkey bindings. Each hotkey is a <see cref="GamepadCombination"/>
    /// allowing multi-button combos (e.g. Guide + DpadUp).
    /// </summary>
    public class GamepadHotkeys
    {
        public GamepadCombination ToggleVSyncMode { get; set; } = new();
        public GamepadCombination Screenshot { get; set; } = new();
        public GamepadCombination ShowUI { get; set; } = new();
        public GamepadCombination Pause { get; set; } = new();
        public GamepadCombination ToggleMute { get; set; } = new();
        public GamepadCombination ResScaleUp { get; set; } = new();
        public GamepadCombination ResScaleDown { get; set; } = new();
        public GamepadCombination VolumeUp { get; set; } = new();
        public GamepadCombination VolumeDown { get; set; } = new();
        public GamepadCombination CustomVSyncIntervalIncrement { get; set; } = new();
        public GamepadCombination CustomVSyncIntervalDecrement { get; set; } = new();
        public GamepadCombination TurboMode { get; set; } = new();
        public GamepadCombination StopEmulation { get; set; } = new();
    }
}
