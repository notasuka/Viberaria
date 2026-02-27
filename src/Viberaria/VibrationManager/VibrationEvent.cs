using System;
using Microsoft.Xna.Framework;
using Viberaria.tModAdapters;

namespace Viberaria.VibrationManager;

public class VibrationEvent
{
    /// <summary>
    /// The start time of the event
    /// </summary>
    public DateTime Timestamp { get; }
    /// <summary>
    /// The length of an event in milliseconds
    /// </summary>
    public int Duration { get; }
    /// <summary>
    /// The strength of vibrations during the event
    /// </summary>
    public float Strength { get; }

    public DateTime EndTime;
    /// <summary>
    ///  Whether the event was finished last time <see cref="InFuture()"/> was checked.
    /// </summary>
    /// <remarks>This is used by VibrationManager because checking <see cref="InFuture()"/> multiple times can result in the
    /// output changing as time continues.</remarks>
    public bool WasFinishedDuringLastCheck;
    private bool _hasFinished;


    /// <summary>
    /// Create a new vibration event indicating the strength of the vibration at a certain time.
    /// </summary>
    /// <param name="timestamp">When the event starts.</param>
    /// <param name="duration">How long the event lasts.</param>
    /// <param name="strength">How strong the toy should vibrate during the event.</param>
    public VibrationEvent(DateTime timestamp, int duration, float strength)
    {
        if (strength is < 0 or > 1)
        {
            if (Config.ViberariaConfig.Instance.Debug.Enabled)
                tChat.LogToPlayer($"Tried to vibrate at a strength outside of 0.0-1.0 ({strength})! Clamping.", Color.Red);
            strength = Math.Clamp(strength, 0, 1);
        }

        Timestamp = timestamp;
        Duration = duration;
        Strength = strength;
        // calculate end time in constructor to reduce unnecessary computations
        EndTime = Timestamp + TimeSpan.FromMilliseconds(Duration);
    }

    /// <summary>
    /// Create a new vibration event indicating the strength of the vibration, starting at the current time.
    /// </summary>
    /// <param name="duration">How long the event lasts.</param>
    /// <param name="strength">How strong the toy should vibrate during the event.</param>
    public VibrationEvent(int duration, float strength) : this(DateTime.Now, duration, strength) { }

    /// <summary>
    /// Whether an event started and finished in the past.
    /// </summary>
    /// <returns>True if event has passed.</returns>
    /// <remarks>The time of the end of the event is <c>timestamp + duration</c>. An event has passed when the
    /// current time is past this end time.</remarks>
    public bool HasPassed()
    {
        return _hasFinished || DateTime.Now >= EndTime;
    }

    /// <summary>
    /// Check whether the event is in the future and save that value to <see cref="WasFinishedDuringLastCheck"/>.
    /// </summary>
    /// <returns>Whether the event is in the future.</returns>
    public bool InFuture()
    {
        return WasFinishedDuringLastCheck = Timestamp > DateTime.Now;
    }

    /// <summary>
    /// A helper function to indicate that this vibration event has been executed.
    /// </summary>
    public void MarkAsFinished()
    {
        _hasFinished = true;
    }
}