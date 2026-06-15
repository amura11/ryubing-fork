using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// Listens for gamepad button input, either from a specific gamepad or from the first
    /// gamepad that produces any input (auto-detect mode).
    /// </summary>
    public class GamepadInputListener : IInputListener
    {
        private const int PollIntervalMs = 10;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        private readonly IGamepadDriver _driver;

        // Null means auto-detect: lock to the first gamepad that sends any input.
        private readonly string _pinnedGamepadId;

        public GamepadInputListener(IGamepadDriver driver, string gamepadId = null)
        {
            _driver = driver;
            _pinnedGamepadId = gamepadId;
        }

        public Task<Button?> ListenForButtonAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

            // Route to the appropriate implementation based on whether a specific gamepad was requested.
            return _pinnedGamepadId != null
                ? ListenForButtonOnPinnedGamepadAsync(deadline, cancellationToken)
                : ListenForButtonOnAnyGamepadAsync(deadline, cancellationToken);
        }

        public async Task<Button[]> ListenForComboAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
            HashSet<GamepadButtonInputId> collected = [];
            Stopwatch timer = new();

            IGamepad lockedGamepad;

            if (_pinnedGamepadId != null)
            {
                lockedGamepad = _driver.GetGamepad(_pinnedGamepadId);

                if (lockedGamepad == null)
                {
                    return [];
                }
            }
            else
            {
                // Wait phase: scan all gamepads until one sends input, then lock to it.
                lockedGamepad = await WaitForAnyGamepadInputAsync(deadline, cancellationToken, collected, timer);

                if (lockedGamepad == null)
                {
                    return [];
                }
            }

            try
            {
                // Collect/release phase: hold the locked gamepad to avoid per-iteration GetGamepad overhead.
                while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(PollIntervalMs).ConfigureAwait(false);
                    timer.Restart();

                    if (!lockedGamepad.IsConnected)
                    {
                        return [];
                    }

                    List<GamepadButtonInputId> currentPressed = GetPressedButtons(lockedGamepad.GetStateSnapshot());

                    if (currentPressed.Count > 0)
                    {
                        // Collect phase: accumulate all buttons held during the combo.
                        collected.UnionWith(currentPressed);
                    }
                    else
                    {
                        // All buttons released: combo is complete.
                        return collected.Select(b => new Button(b)).ToArray();
                    }

                    LogIfSlow(timer, nameof(ListenForComboAsync));
                }
            }
            finally
            {
                lockedGamepad.Dispose();
            }

            return [];
        }

        public void Dispose() { }

        // Polls a single known gamepad with edge detection so a button already held at listen-start doesn't fire.
        private async Task<Button?> ListenForButtonOnPinnedGamepadAsync(DateTime deadline, CancellationToken cancellationToken)
        {
            using IGamepad gamepad = _driver.GetGamepad(_pinnedGamepadId);

            if (gamepad == null)
            {
                return null;
            }

            Stopwatch timer = new();
            GamepadStateSnapshot previous = gamepad.GetStateSnapshot();

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);
                timer.Restart();

                if (!gamepad.IsConnected)
                {
                    break;
                }

                GamepadStateSnapshot current = gamepad.GetStateSnapshot();

                for (GamepadButtonInputId button = GamepadButtonInputId.A; button < GamepadButtonInputId.Count; button++)
                {
                    if (current.IsPressed(button) && !previous.IsPressed(button))
                    {
                        return new Button(button);
                    }
                }

                previous = current;
                LogIfSlow(timer, nameof(ListenForButtonAsync));
            }

            return null;
        }

        // Scans all connected gamepads each iteration and returns the first pressed button found.
        private async Task<Button?> ListenForButtonOnAnyGamepadAsync(DateTime deadline, CancellationToken cancellationToken)
        {
            Stopwatch timer = new();

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);
                timer.Restart();

                foreach (string id in _driver.GamepadsIds)
                {
                    using IGamepad gamepad = _driver.GetGamepad(id);

                    if (gamepad == null)
                    {
                        continue;
                    }

                    Button? pressed = GetFirstPressedButton(gamepad.GetStateSnapshot());

                    if (pressed.HasValue)
                    {
                        return pressed;
                    }
                }

                LogIfSlow(timer, nameof(ListenForButtonAsync));
            }

            return null;
        }

        /// <summary>
        /// Scans all connected gamepads until one has a button pressed. Returns that gamepad
        /// (caller owns and must dispose it) and seeds <paramref name="collected"/> with the
        /// initial buttons so the collect phase doesn't miss them.
        /// </summary>
        private async Task<IGamepad> WaitForAnyGamepadInputAsync(
            DateTime deadline,
            CancellationToken cancellationToken,
            HashSet<GamepadButtonInputId> collected,
            Stopwatch timer)
        {
            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);
                timer.Restart();

                foreach (string id in _driver.GamepadsIds)
                {
                    IGamepad gamepad = _driver.GetGamepad(id);

                    if (gamepad == null)
                    {
                        continue;
                    }

                    List<GamepadButtonInputId> pressed = GetPressedButtons(gamepad.GetStateSnapshot());

                    if (pressed.Count > 0)
                    {
                        collected.UnionWith(pressed);
                        return gamepad; // Caller takes ownership.
                    }

                    gamepad.Dispose();
                }

                LogIfSlow(timer, nameof(ListenForComboAsync));
            }

            return null;
        }

        private static Button? GetFirstPressedButton(GamepadStateSnapshot snapshot)
        {
            for (GamepadButtonInputId button = GamepadButtonInputId.A; button < GamepadButtonInputId.Count; button++)
            {
                if (snapshot.IsPressed(button))
                {
                    return new Button(button);
                }
            }

            return null;
        }

        private static List<GamepadButtonInputId> GetPressedButtons(GamepadStateSnapshot snapshot)
        {
            List<GamepadButtonInputId> pressed = [];

            for (GamepadButtonInputId button = GamepadButtonInputId.A; button < GamepadButtonInputId.Count; button++)
            {
                if (snapshot.IsPressed(button))
                {
                    pressed.Add(button);
                }
            }

            return pressed;
        }

        private static void LogIfSlow(Stopwatch timer, string methodName)
        {
            timer.Stop();

            if (timer.ElapsedMilliseconds > PollIntervalMs)
            {
                Logger.Warning?.PrintMsg(LogClass.Hid, $"GamepadInputListener.{methodName} poll took {timer.ElapsedMilliseconds}ms (expected <{PollIntervalMs}ms)");
            }
        }
    }
}
