using RaceControl.Data.Dtos.LiveTimingDtos;

namespace RaceControl.Console.Events;

public class MessageEventArgs : EventArgs
{
    public string Topic { get; init; }

    public ILiveTimingDto Message { get; init; }
}