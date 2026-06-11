using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Common.Configuration.Hid
{
    /// <summary>
    /// Represents a combination of gamepad buttons that must all be pressed simultaneously.
    /// </summary>
    public class GamepadCombination
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
}
