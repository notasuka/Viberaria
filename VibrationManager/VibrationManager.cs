using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using static Viberaria.ViberariaConfig;
using static Viberaria.bClient;

namespace Viberaria.VibrationManager;

public static class VibrationManager
{
    private static VibrationEvent _currentEvent = null;
    private static readonly Dictionary<VibrationPriority, LinkedList<VibrationEvent>> EventLists = new();
    private static float _currentStrength = 0f;
    private static readonly object CurrentStrengthLock = new();
    private static readonly object EventCheckerLock = new();

    static VibrationManager()
    {
        foreach (VibrationPriority priority in Enum.GetValues(typeof(VibrationPriority)))
        {
            EventLists[priority] = [];
        }
    }

    /// <summary>
    /// Create a new event to vibrate plugs for a certain duration.
    /// </summary>
    /// <param name="priority">The priority of the event.</param>
    /// <param name="timeOffset">The starting time offset from the current time (eg. starting in 1 second).</param>
    /// <param name="duration">The length of the vibration, in milliseconds.</param>
    /// <param name="strength">The strength of the vibration, from 0f to 1f.</param>
    /// <param name="addToFront">Whether the event should prioritize over other events of the same priority.</param>
    /// <param name="clearOthers">Whether the event should remove all other registered events of its priority.</param>
    public static void AddEvent(VibrationPriority priority, TimeSpan timeOffset, int duration, float strength, bool addToFront, bool clearOthers = false)
    {
        VibrationEvent vibrationEvent = new(DateTime.Now + timeOffset, duration, strength);
        if (clearOthers)
            EventLists[priority].Clear();
        if (addToFront)
            EventLists[priority].AddFirst(vibrationEvent);
        else
            EventLists[priority].AddLast(vibrationEvent);
        ProcessEvents();
    }

    /// <summary>
    /// Create a new event to vibrate plugs for a certain duration.
    /// </summary>
    /// <param name="priority">The priority of the event.</param>
    /// <param name="duration">The length of the vibration, in milliseconds.</param>
    /// <param name="strength">The strength of the vibration, from 0f to 1f.</param>
    /// <param name="addToFront">Whether the event should prioritize over other events of the same priority.</param>
    /// <param name="clearOthers">Whether the event should remove all other registered events of its priority.</param>
    public static void AddEvent(VibrationPriority priority, int duration, float strength, bool addToFront, bool clearOthers = false)
    {
        AddEvent(priority, timeOffset: TimeSpan.Zero, duration, strength, addToFront, clearOthers);
    }

    /// <summary>
    /// Clear all events in a priority's event list.
    /// </summary>
    /// <param name="priority">The priority of the event.</param>
    public static void ClearEvents(VibrationPriority priority)
    {
        EventLists[priority].Clear();
    }

    /// <summary>
    /// Remove all vibration events and stop all toys.
    /// </summary>
    public static void Halt()
    {
        foreach (var priority in Enum.GetValues(typeof(VibrationPriority))
                                     .Cast<VibrationPriority>()
                                     .OrderByDescending(priority => (int)priority))
        {
            EventLists[priority].Clear();
        }
        StopVibratingAllDevices();
    }

    /// <summary>
    /// Get the first vibration event in the given list that hasn't passed yet.
    /// </summary>
    /// <param name="eventList">The event list of a certain Vibration Priority</param>
    /// <returns>The first valid event in the list, or null if none found.</returns>
    private static VibrationEvent GetNextEvent(LinkedList<VibrationEvent> eventList)
    {
        // Todo: There might be a crash when locking the list if the intiface server stops while connected. Not
        //  sure what the exact context is...
        //  "NullReferenceException" at Viberaria.VibrationManager.VibrationManager.GetNextEvent(LinkedList`1 eventList)
        if (eventList == null)
        {
            // Funny number to find where in code the error came from.
            tChat.LogToPlayer("Viberaria: [193853] This will have caused a crash. " +
                              "Please report the funny number to the developer.", Color.Red);
            return null;
        }

        while (eventList.First != null)
        {
            VibrationEvent currentEvent = eventList.First.Value;
            if (currentEvent == null)
            {
                // Funny number to find where in code the error came from.
                tChat.LogToPlayer("Viberaria: [094385] This will have caused a crash. " +
                                  "Please report the funny number to the developer.", Color.Red);
                continue;
            }

            if (!currentEvent.HasPassed())
                return currentEvent;
            eventList.RemoveFirst();
        }

        return null;
    }

    /// <summary>
    /// Loop through all priorities and pick the first event of highest priority. Then vibrate toys with this event's strength.
    /// </summary>
    private static async void ProcessEvents()
    {
        bool eventFound = false;
        VibrationEvent soonestEvent = null;
        lock (EventCheckerLock)
        {
            foreach (var priority in Enum.GetValues(typeof(VibrationPriority))
                         .Cast<VibrationPriority>()
                         .OrderByDescending(priority => (int)priority))
            {
                VibrationEvent currentEvent = GetNextEvent(EventLists[priority]);
                if (currentEvent == null)
                {
                    continue;
                }

                // only vibrate if vibration strength/event changed
                if (_currentEvent == currentEvent)
                {
                    if (Instance.Debug.Enabled && Instance.Debug.ProcessEventMessages)
                    {
                        TimeSpan timeLeft = _currentEvent.EndTime - DateTime.Now;
                        double secs = Math.Truncate(timeLeft.TotalSeconds);
                        int nanos = (int)Math.Abs(timeLeft.TotalNanoseconds - secs * 1_000_000_000);
                        string nanosStr = nanos.ToString().PadLeft(9, '0'); // ensure leading zeros
                        tChat.LogToPlayer($"Iterating Events: Event ongoing ({secs}.{nanosStr} left).", Color.GreenYellow);
                    }
                    return;
                }

                if (soonestEvent == null ||
                    currentEvent.Timestamp < soonestEvent.Timestamp)
                {
                    soonestEvent = currentEvent;
                }

                if (soonestEvent.InFuture())
                {
                    continue;
                }

                if (Instance.Debug.Enabled && Instance.Debug.ProcessEventMessages)
                {
                    tChat.LogToPlayer("Iterating Events: Event found.", Color.GreenYellow);

                    double remainingTime = (currentEvent.EndTime - DateTime.Now).TotalMilliseconds;
                    int callbackT = (int)Math.Ceiling(remainingTime);
                    tChat.LogToPlayer($"dur={currentEvent.Duration}ms;rem={remainingTime}ms;callback={callbackT}ms", Color.YellowGreen);
                }

                eventFound = true;
                _currentEvent = currentEvent;
                break;
            }
        }

        if (eventFound)
        {
            VibrateAllDevices(_currentEvent);
            return;
        }

        if (Instance.Debug.Enabled && Instance.Debug.ProcessEventMessages &&
            !(soonestEvent != null && soonestEvent.InFuture()))
            // don't print if there is an event planned in the future. The next message will print that instead.
            tChat.LogToPlayer("Iterating Events: No event ongoing! :D", Color.GreenYellow);

        if (_currentEvent.HasPassed())
        {
            StopVibratingAllDevices();
        }


        if (soonestEvent != null && soonestEvent.InFuture())
        {
            TimeSpan delayTime = soonestEvent.Timestamp - DateTime.Now;
            if (Instance.Debug.Enabled && Instance.Debug.ProcessEventMessages)
                tChat.LogToPlayer($"Iterating Events: Soonest event in {delayTime.TotalMilliseconds} ms! Waiting...", Color.GreenYellow);
            await Task.Delay(delayTime);
            ProcessEvents();
        }
    }

    /// <summary>
    /// Vibrate all connected toys at a given strength for a given time, after which it calls ProcessEvents to get
    /// the next up vibration (eg. a lower priority event).
    /// </summary>
    /// <param name="vibrationEvent">The event containing vibration strength and duration.</param>
    private static async void VibrateAllDevices(VibrationEvent vibrationEvent)
    {
        // Take the ceiling, to ensure the vibration isn't shorter than the event duration.
        int callbackTime = (int)Math.Ceiling((vibrationEvent.EndTime - DateTime.Now).TotalMilliseconds);
        if (callbackTime < 0) callbackTime = 0; // ensure not to delay infinitely (Task.Delay(-1)).

        lock (CurrentStrengthLock)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_currentStrength != vibrationEvent.Strength)
            {
                // lower the amount of chat spam
                _currentStrength = vibrationEvent.Strength;
                if (Instance.Debug.Enabled && Instance.Debug.ToyStrengthMessages)
                {
                        tChat.LogToPlayer($"Vibrating at `{vibrationEvent.Strength}` for `{callbackTime}` msec", Color.Lime);
                }

                TryVibrateAllDevices(vibrationEvent.Strength);
            }
        }

        await Task.Delay(callbackTime);
        // Task.Delay isn't accurate enough and can cause the event to see itself as ongoing event. Thus,
        // mark it as finished.
        vibrationEvent.MarkAsFinished();

        if (Instance.Debug.Enabled && Instance.Debug.ProcessEventMessages)
            tChat.LogToPlayer($"  Event `{vibrationEvent.Strength},{callbackTime}` finished.", Color.GreenYellow);
        ProcessEvents();
    }

    /// <summary>
    /// A helper function to handle Intiface errors when vibrating toys.
    /// </summary>
    /// <param name="strength">The strength to vibrate toys at.</param>
    private static async void TryVibrateAllDevices(float strength)
    {
        try
        {
            foreach (var device in _client.Devices)
            {
                await device.VibrateAsync(strength * Instance.VibratorMaxIntensity);
            }
        }
        catch (Buttplug.Core.ButtplugException ex)
        {
            tChat.LogToPlayer($"Error trying to vibrate plug(s) with strength `{strength}`! \"{ex.Message}\"",
                Color.Red);
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't vibrate plug(s) on strength `{0}`:\n{1}",
                strength, ex.StackTrace);
        }
        catch (Exception ex)
        {
            // todo
            ModContent.GetInstance<Viberaria>().Logger.FatalFormat("UNHANDLED EXCEPTION while trying to vibrate plug(s) on strength `{0}`:\n{1}", strength, ex.StackTrace);
        }
    }

    /// <summary>
    /// A helper function to handle resetting the vibration manager and setting all toys to 0 strength
    /// </summary>
    private static void StopVibratingAllDevices()
    {
        lock (CurrentStrengthLock)
        {
            // lower the amount of chat spam
            if (_currentStrength != 0)
            {
                _currentStrength = 0;

                // Similar to VibrateAllDevices but without calling ProcessEvents afterward
                if (Instance.Debug.Enabled && Instance.Debug.ToyStrengthMessages)
                    tChat.LogToPlayer("Vibrating at `0`", Color.Lime);
            }

            TryVibrateAllDevices(0);
        }
    }
}