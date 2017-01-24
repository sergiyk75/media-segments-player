namespace MediaPlaybackLib
{
    /// <summary>
    /// this are the external public levels
    /// we might have an internal level of timelinescale. e.g. Hour can be internally represented by a QuaterHour or HalfHours as the space allows
    /// </summary>
    public enum TimescaleLevel
    {
        None,
        Second,
        Minute,
        Hour,
        Day,
        Month,
        Year,
        Millisecond
    }
}