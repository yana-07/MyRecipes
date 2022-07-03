namespace MyRecipes.Services.Data
{
    using MyRecipes.Services.Data.ModelsDTOs;
    using MyRecipes.Web.ViewModels.Home;

    public interface IGetCountsService
    {
        // 1. Use View Model - IndexViewModel GetCounts();
        // 2. Create DTO -> controller makes it to a view model - CountsDto GetCounts();
        // 3. Tuples - (int RecipesCount, int CategoriesCount, int IngredientsCount, int ImagesCount) GetCounts();
        CountsDto GetCounts();
    }
}
