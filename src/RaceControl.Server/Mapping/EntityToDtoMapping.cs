using AutoMapper;
using RaceControl.Data.Dtos;
using RaceControl.Database.Entities;
using RaceControl.Server.Mapping.Converters;

namespace RaceControl.Server.Mapping;

public class EntityToDtoMapping : Profile
{
    public EntityToDtoMapping()
    {
        CreateMap<Championship, ChampionshipDto>();

        CreateMap<Event, EventDto>()
            .ForMember(dto => dto.StartDate, opt => opt.ConvertUsing(new DateTimeConverter(), src => src.StartTime))
            .ForMember(dto => dto.EndDate, opt => opt.ConvertUsing(new DateTimeConverter(), src => src.EndTime));

        CreateMap<Session, SessionDto>()
            .ForMember(dto => dto.StartTime, opt => opt.ConvertUsing(new DateTimeConverter(), s => s.StartTime));
    }
}