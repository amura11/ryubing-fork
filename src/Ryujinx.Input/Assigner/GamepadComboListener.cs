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
    /// Listens for a multi-button gamepad combination by polling all connected gamepads.
    /// Locks to the first gamepad that has a button pressed, collects all buttons held
    /// during the hold phase, and returns the full set when all buttons are released.
    /// </summary>
    public class GamepadComboListener : IMultiButtonListener
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
        private const int PollIntervalMs = 10;

        private readonly IGamepadDriver _gamepadDriver;

        public GamepadComboListener(IGamepadDriver gamepadDriver)
        {
            _gamepadDriver = gamepadDriver;
        }

        public async Task<Button[]> ListenAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
            HashSet<GamepadButtonInputId> collectedButtons = [];
            string activeGamepadId = null;
            Stopwatch iterationTimer = new();

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollIntervalMs, cancellationToken).ConfigureAwait(false);

                iterationTimer.Restart();

                ReadOnlySpan<string> gamepadIds = _gamepadDriver.GamepadsIds;

                // If the active gamepad disconnects, bail
                if (activeGamepadId != null && !GamepadStillConnected(gamepadIds, activeGamepadId))
                {
                    return [];
                }

                foreach (string id in gamepadIds)
                {
                    if (activeGamepadId != null && id != activeGamepadId)
                    {
                        continue;
                    }

                    using IGamepad gamepad = _gamepadDriver.GetGamepad(id);

                    if (gamepad == null)
                    {
                        continue;
                    }

                    List<GamepadButtonInputId> currentlyPressed = GetPressedButtons(gamepad);

                    if (activeGamepadId == null)
                    {
                        // Wait phase: lock to the first gamepad that has any button pressed
                        if (currentlyPressed.Count > 0)
                        {
                            activeGamepadId = id;
                            collectedButtons.UnionWith(currentlyPressed);
                        }
                    }
                    else
                    {
                        if (currentlyPressed.Count > 0)
                        {
                            // Collect phase: accumulate all buttons held during the combo
                            collectedButtons.UnionWith(currentlyPressed);
                        }
                        else
                        {
                            // All buttons released: combo is complete
                            return collectedButtons.Select(b => new Button(b)).ToArray();
                        }
                    }
                }

                iterationTimer.Stop();

                if (iterationTimer.ElapsedMilliseconds > PollIntervalMs)
                {
                    Logger.Warning?.PrintMsg(LogClass.Hid, $"GamepadComboListener poll took {iterationTimer.ElapsedMilliseconds}ms (expected <{PollIntervalMs}ms)");
                }
            }

            return [];
        }

        private static List<GamepadButtonInputId> GetPressedButtons(IGamepad gamepad)
        {
            List<GamepadButtonInputId> pressed = [];
            GamepadStateSnapshot snapshot = gamepad.GetStateSnapshot();

            for (GamepadButtonInputId buttonId = GamepadButtonInputId.A; buttonId < GamepadButtonInputId.Count; buttonId++)
            {
                if (snapshot.IsPressed(buttonId))
                {
                    pressed.Add(buttonId);
                }
            }

            return pressed;
        }

        private static bool GamepadStillConnected(ReadOnlySpan<string> connectedIds, string targetId)
            => connectedIds.Contains(targetId);
    }
}
