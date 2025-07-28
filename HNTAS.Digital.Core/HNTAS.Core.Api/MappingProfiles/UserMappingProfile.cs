using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Models.Users;

namespace HNTAS.Core.Api.MappingProfiles
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {

            CreateMap<OrgDetails, Organisation>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.OrgName))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.OrgId))
            .ForMember(dest => dest.CompaninesHouseNumber, opt => opt.MapFrom(src => src.CompaniesHouseNumber));


            CreateMap<User, UserResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(src => src.EmailId))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                StringFormatter.ToTitleCaseSingleWord(src.OrgDetails.FirstName) + " " + StringFormatter.ToTitleCaseSingleWord(src.OrgDetails.LastName)
            ))
            // Map the entire OrgDetails object to Organisation
            .ForMember(dest => dest.Organisation, opt => opt.MapFrom(src => src.OrgDetails))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                src.Roles.Select(role => role.GetDescription()).ToList()
            ))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                src.Status.ToString()
            ));


        }
    }
}
