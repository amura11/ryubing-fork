using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Common.Configuration.Hid
{
    /// <summary>
    /// Represents a combination of gamepad buttons that must all be pressed simultaneously to trigger a hotkey.
    /// </summary>
    public class GamepadHotkeyCombination
    {
        /// <summary>
        /// The set of buttons that make up this combination. All must be held at once to activate.
        /// </summary>
        public GamepadInputId[] Buttons { get; set; } = [];

        /// <summary>
        /// Whether this combination has no buttons assigned.
        /// </summary>
        public bool IsUnbound => Buttons == null || Buttons.Length == 0;
    }

    /// <summary>
    /// Configuration for gamepad hotkey bindings. Each hotkey is a <see cref="GamepadHotkeyCombination"/>
    /// allowing multi-button combos (e.g. Guide + DpadUp).
    /// </summary>
    public class GamepadHotkeys
    {
        public GamepadHotkeyCombination ToggleVSyncMode { get; set; } = new();
        public GamepadHotkeyCombination Screenshot { get; set; } = new();
        public GamepadHotkeyCombination ShowUI { get; set; } = new();
        public GamepadHotkeyCombination Pause { get; set; } = new();
        public GamepadHotkeyCombination ToggleMute { get; set; } = new();
        public GamepadHotkeyCombination ResScaleUp { get; set; } = new();
        public GamepadHotkeyCombination ResScaleDown { get; set; } = new();
        public GamepadHotkeyCombination VolumeUp { get; set; } = new();
        public GamepadHotkeyCombination VolumeDown { get; set; } = new();
        public GamepadHotkeyCombination CustomVSyncIntervalIncrement { get; set; } = new();
        public GamepadHotkeyCombination CustomVSyncIntervalDecrement { get; set; } = new();
        public GamepadHotkeyCombination TurboMode { get; set; } = new();
    }
}
