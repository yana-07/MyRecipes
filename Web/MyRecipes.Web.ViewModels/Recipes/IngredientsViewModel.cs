namespace MyRecipes.Web.ViewModels.Recipes
{
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Mapping;

    public class IngredientsViewModel : IMapFrom<RecipeIngredient>
    {
        public string Quantity { get; set; }

        public string IngredientName { get; set; }
    }
}
