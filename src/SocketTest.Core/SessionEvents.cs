namespace SocketTest.Core;

public enum SessionDirection
{
    Info,
    Sent,
    Received,
    Error
}

public sealed record SessionEvent(SessionDirection Direction, string Text, DateTimeOffset Timestamp)
{
    public static SessionEvent Info(string text) =>
        new(SessionDirection.Info, text, DateTimeOffset.Now);

    public static SessionEvent Sent(string text) =>
        new(SessionDirection.Sent, text, DateTimeOffset.Now);

    public static SessionEvent Received(string text) =>
        new(SessionDirection.Received, text, DateTimeOffset.Now);

    public static SessionEvent Error(string text) =>
        new(SessionDirection.Error, text, DateTimeOffset.Now);
}
