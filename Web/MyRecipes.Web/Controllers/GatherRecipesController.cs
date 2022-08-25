namespace MyRecipes.Web.Controllers
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using MyRecipes.Services;

    // TODO: Move to the administration area
    public class GatherRecipesController : BaseController
    {
        private readonly IGotvachBgScraperService gotvachBgScraperService;

        public GatherRecipesController(IGotvachBgScraperService gotvachBgScraperService)
        {
            this.gotvachBgScraperService = gotvachBgScraperService;
        }

        public IActionResult Index()
        {
            return this.View();
        }

        public async Task<IActionResult> Add()
        {
            await this.gotvachBgScraperService.ImportRecipesAsync(0, 100);

            return this.View("Index");
        }
    }
}
