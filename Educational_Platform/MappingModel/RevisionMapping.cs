using AutoMapper;
using Data_access_layer.model;
using Educational_Platform.ViewModel;

namespace Educational_Platform.MappingModel
{
    public class RevisionMapping : Profile
    {

        public RevisionMapping()
        {
            CreateMap<RevisionModelView, Revision>()
                .ReverseMap();
        }
    }
}
