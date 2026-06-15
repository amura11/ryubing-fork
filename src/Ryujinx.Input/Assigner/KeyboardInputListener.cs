using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// Listens for keyboard button input.
    /// </summary>
    public class KeyboardInputListener : IInputListener
    {
        private const int PollIntervalMs = 10;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        private readonly IKeyboard _keyboard;

        public KeyboardInputListener(IKeyboard keyboard)
        {
            _keyboard = keyboard;
        }

        public async Task<Button?> ListenForButtonAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

            // Use edge detection so a key already held when listening starts doesn't fire immediately.
            HashSet<Key> previouslyPressed = GetPressedKeys();

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);

                HashSet<Key> currentlyPressed = GetPressedKeys();

                // Check platform-specific key aliases before regular edge detection.
                // Some OS/keyboard combinations report one physical key as multiple logical keys.
                Key aliased = GetAliasedKey(currentlyPressed);

                if (aliased != Key.Unknown && !previouslyPressed.Contains(aliased))
                {
                    return new Button(aliased);
                }

                foreach (Key key in currentlyPressed)
                {
                    if (!previouslyPressed.Contains(key))
                    {
                        return new Button(key);
                    }
                }

                previouslyPressed = currentlyPressed;
            }

            return null;
        }

        public async Task<Button[]> ListenForComboAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
            HashSet<Key> collected = [];
            bool hasInput = false;

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);

                HashSet<Key> currentlyPressed = GetPressedKeys();

                if (!hasInput)
                {
                    // Wait phase: wait for any key to be pressed.
                    if (currentlyPressed.Count > 0)
                    {
                        hasInput = true;
                        collected.UnionWith(currentlyPressed);
                    }
                }
                else
                {
                    if (currentlyPressed.Count > 0)
                    {
                        // Collect phase: accumulate all keys held during the combo.
                        collected.UnionWith(currentlyPressed);
                    }
                    else
                    {
                        // All keys released: combo is complete.
                        return collected.Select(k => new Button(k)).ToArray();
                    }
                }
            }

            return [];
        }

        public void Dispose() { }

        private HashSet<Key> GetPressedKeys()
        {
            HashSet<Key> pressed = [];

            // Skip Key.Unknown (0), Key.Unbound, and Key.Count.
            for (Key key = Key.ShiftLeft; key < Key.Unbound; key++)
            {
                if (_keyboard.IsPressed(key))
                {
                    pressed.Add(key);
                }
            }

            return pressed;
        }

        private static Key GetAliasedKey(HashSet<Key> pressedKeys)
        {
            // On some layouts (e.g. AltGr on Windows), RightAlt is reported as Ctrl+Alt.
            // Prefer AltRight so the binding reflects the physical key pressed.
            if (pressedKeys.Contains(Key.ControlLeft) && pressedKeys.Contains(Key.AltRight))
            {
                return Key.AltRight;
            }

            // On some Copilot keyboards, the right-control key is reported as ShiftLeft+Win+F23.
            // Prefer ControlRight so the binding reflects the physical key pressed.
            if (pressedKeys.Contains(Key.ShiftLeft) &&
                pressedKeys.Contains(Key.F23) &&
                (pressedKeys.Contains(Key.WinLeft) || pressedKeys.Contains(Key.WinRight)))
            {
                return Key.ControlRight;
            }

            return Key.Unknown;
        }
    }
}
