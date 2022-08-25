namespace MyRecipes.Services
{
    using System.Threading.Tasks;

    public interface IGotvachBgScraperService
    {
        Task ImportRecipesAsync(int fromId, int toId);
    }
}
