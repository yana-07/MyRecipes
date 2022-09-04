namespace MyRecipes.Web.ViewModels.Recipes
{
    using AutoMapper;
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Mapping;

    public class EditRecipeInputModel : BaseRecipeInputModel, IMapFrom<Recipe>, IHaveCustomMappings
    {
        public int Id { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<Recipe, EditRecipeInputModel>()
                .ForMember(d => d.CookingTime, opt => opt.MapFrom(s => (int)s.CookingTime.TotalMinutes))
                .ForMember(d => d.PreparationTime, opt => opt.MapFrom(s => (int)s.PreparationTime.TotalMinutes));
        }
    }
}
