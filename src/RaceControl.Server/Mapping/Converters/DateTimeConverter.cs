using AutoMapper;

namespace RaceControl.Server.Mapping.Converters;

public class DateTimeConverter : IValueConverter<DateTime, string>
{
    public string Convert(DateTime sourceMember, ResolutionContext context)
        => sourceMember.ToString("u");
}