namespace MyRecipes.Web.ViewModels.Home
{
    using System.Linq;

    using AutoMapper;
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Mapping;

    public class IndexPageRecipeViewModel : IMapFrom<Recipe>, IHaveCustomMappings
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string CategoryName { get; set; }

        public string ImageUrl { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<Recipe, IndexPageRecipeViewModel>()
                .ForMember(d => d.ImageUrl, opt =>
                    opt.MapFrom(s =>
                         s.Images.FirstOrDefault().RemoteImageUrl != null ?
                         s.Images.FirstOrDefault().RemoteImageUrl :
                         "/images/recipes/" + s.Images.FirstOrDefault().Id + "." + s.Images.FirstOrDefault().Extension));
        }
    }
}
