using AutoMapper;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Database.Entities;

namespace RaceControl.Server.MappingProfiles;

public class DtoToEntityMapping : Profile
{
    public DtoToEntityMapping()
    {
        CreateMap<RaceControlMessageLiveTimingDto, RaceControlMessage>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Utc.ToUniversalTime()))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => RaceControlMessage.ParseCategory(src.Category)))
            .ForMember(dest => dest.Sector, opt => opt.MapFrom(src => RaceControlMessage.ParseScope(src.Scope)))
            .ForMember(dest => dest.DriverNumber, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.RacingNumber) ? int.Parse(src.RacingNumber) : (int?)null));
    }
}