using System;

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

    /// <summary>
    /// Create a new vibration event indicating the strength of the vibration at a certain time.
    /// </summary>
    /// <param name="timestamp">When the event starts.</param>
    /// <param name="duration">How long the event lasts.</param>
    /// <param name="strength">How strong the toy should vibrate during the event.</param>
    public VibrationEvent(DateTime timestamp, int duration, float strength)
    {
        Timestamp = timestamp;
        Duration = duration;
        Strength = strength;
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
        DateTime eventEndTime = Timestamp + TimeSpan.FromMilliseconds(Duration);
        return DateTime.Now >= eventEndTime;
    }
}