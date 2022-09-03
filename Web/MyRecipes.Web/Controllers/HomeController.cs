namespace MyRecipes.Web.Controllers
{
    using System.Diagnostics;

    using AutoMapper;
    using Microsoft.AspNetCore.Mvc;
    using MyRecipes.Services.Data;
    using MyRecipes.Web.ViewModels;
    using MyRecipes.Web.ViewModels.Home;

    public class HomeController : BaseController
    {
        private readonly IGetCountsService getCountService;
        private readonly IRecipesService recipesService;

        public HomeController(
            IGetCountsService getCountsService,
            IRecipesService recipesService)
        {
            this.getCountService = getCountsService;
            this.recipesService = recipesService;
        }

        public IActionResult Index()
        {
            var countsDto = this.getCountService.GetCounts();

            // var viewModel = this.mapper.Map<IndexViewModel>(countsDto); - не работи без регистрация
            // var viewModel = AutoMapperConfig.MapperInstance.Map<IndexViewModel>(countsDto); - не работи без регистрация
            var viewModel = new IndexViewModel
            {
                RandomRecipes = this.recipesService.GetRandom<IndexPageRecipeViewModel>(7),
                RecipesCount = countsDto.RecipesCount,
                CategoriesCount = countsDto.CategoriesCount,
                IngredientsCount = countsDto.IngredientsCount,
                ImagesCount = countsDto.ImagesCount,
            };
            return this.View(viewModel);
        }

        public IActionResult Privacy()
        {
            return this.View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return this.View(
                new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }
    }
}
