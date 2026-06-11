using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using System;
using System.Collections.Generic;

namespace Ryujinx.Input.HLE
{
    /// <summary>
    /// Snapshots the state of all connected gamepads once per update cycle, then allows
    /// cheap repeated queries against that snapshot. Tracks previous-frame state per
    /// gamepad to support edge detection (button-down events) and clean disconnect handling.
    /// </summary>
    public class GamepadStateManager
    {
        private record TrackedGamepad(string Id)
        {
            public GamepadStateSnapshot? CurrentState { get; set; }
            public GamepadStateSnapshot? PreviousState { get; set; }
        }

        private readonly IGamepadDriver _gamepadDriver;
        private readonly List<TrackedGamepad> _gamepads = [];

        public GamepadStateManager(IGamepadDriver gamepadDriver)
        {
            _gamepadDriver = gamepadDriver;
        }

        /// <summary>
        /// Takes a fresh snapshot of every connected gamepad. Must be called once per
        /// frame before any query methods.
        /// </summary>
        public void Update()
        {
            foreach (TrackedGamepad record in _gamepads)
            {
                record.PreviousState = record.CurrentState;
                record.CurrentState = null;
            }

            ReadOnlySpan<string> gamepadIds = _gamepadDriver.GamepadsIds;

            foreach (string id in gamepadIds)
            {
                using IGamepad gamepad = _gamepadDriver.GetGamepad(id);

                if (gamepad != null)
                {
                    TrackedGamepad record = GetOrCreateRecord(id);
                    record.CurrentState = gamepad.GetStateSnapshot();
                }
            }

            _gamepads.RemoveAll(r => r.CurrentState == null && r.PreviousState == null);
        }

        /// <summary>
        /// Whether a button is currently held on any connected gamepad,
        /// or a specific gamepad if <paramref name="gamepadId"/> is provided.
        /// </summary>
        public bool IsButtonPressed(GamepadButtonInputId button, string gamepadId = null)
        {
            foreach (TrackedGamepad record in _gamepads)
            {
                if (gamepadId != null && record.Id != gamepadId)
                {
                    continue;
                }

                if (record.CurrentState?.IsPressed(button) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a button was just pressed this frame (not held from the previous frame)
        /// on any connected gamepad, or a specific gamepad if <paramref name="gamepadId"/> is provided.
        /// </summary>
        public bool IsButtonDown(GamepadButtonInputId button, string gamepadId = null)
        {
            foreach (TrackedGamepad record in _gamepads)
            {
                if (gamepadId != null && record.Id != gamepadId)
                {
                    continue;
                }

                bool pressedNow = record.CurrentState?.IsPressed(button) == true;
                bool pressedBefore = record.PreviousState?.IsPressed(button) == true;

                if (pressedNow && !pressedBefore)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether all buttons in the combination are currently held on the same gamepad.
        /// Returns false if the combo is null or unbound. Optionally filters to a specific
        /// gamepad if <paramref name="gamepadId"/> is provided.
        /// </summary>
        public bool IsComboActive(GamepadCombination combo, string gamepadId = null)
        {
            if (combo == null || combo.IsUnbound)
            {
                return false;
            }

            foreach (TrackedGamepad record in _gamepads)
            {
                if (gamepadId != null && record.Id != gamepadId)
                {
                    continue;
                }

                if (record.CurrentState != null && IsSnapshotComboPressed(record.CurrentState.Value, combo))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a combo was just activated this frame — all buttons are held now on a
        /// gamepad that did not have the full combo active in the previous frame. Optionally
        /// filters to a specific gamepad if <paramref name="gamepadId"/> is provided.
        /// </summary>
        public bool IsComboDown(GamepadCombination combo, string gamepadId = null)
        {
            if (combo == null || combo.IsUnbound)
            {
                return false;
            }

            foreach (TrackedGamepad record in _gamepads)
            {
                if (gamepadId != null && record.Id != gamepadId)
                {
                    continue;
                }

                bool activeNow = record.CurrentState != null && IsSnapshotComboPressed(record.CurrentState.Value, combo);
                bool activeBefore = record.PreviousState != null && IsSnapshotComboPressed(record.PreviousState.Value, combo);

                if (activeNow && !activeBefore)
                {
                    return true;
                }
            }

            return false;
        }

        private TrackedGamepad GetOrCreateRecord(string id)
        {
            foreach (TrackedGamepad record in _gamepads)
            {
                if (record.Id == id)
                {
                    return record;
                }
            }

            var newRecord = new TrackedGamepad(id);
            _gamepads.Add(newRecord);

            return newRecord;
        }

        private static bool IsSnapshotComboPressed(GamepadStateSnapshot snapshot, GamepadCombination combo)
        {
            foreach (GamepadInputId button in combo.Buttons)
            {
                if (!snapshot.IsPressed((GamepadButtonInputId)button))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
