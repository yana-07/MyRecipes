namespace MyRecipes.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using AngleSharp;
    using MyRecipes.Data.Common.Repositories;
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Models;

    public class GotvachBgScraperService : IGotvachBgScraperService
    {
        private const string BaseUrl = "https://recepti.gotvach.bg/r-{0}";

        private readonly IDeletableEntityRepository<Category> categoriesRepostitory;
        private readonly IDeletableEntityRepository<Ingredient> ingredientsRepository;
        private readonly IDeletableEntityRepository<Recipe> recipesRepository;
        private readonly IRepository<RecipeIngredient> recipeIngredientsRepository;
        private readonly IRepository<Image> imagesRepository;
        private readonly IBrowsingContext context;

        public GotvachBgScraperService(
            IDeletableEntityRepository<Category> categoriesRepostitory,
            IDeletableEntityRepository<Ingredient> ingredientsRepository,
            IDeletableEntityRepository<Recipe> recipesRepository,
            IRepository<RecipeIngredient> recipeIngredientsRepository,
            IRepository<Image> imagesRepository)
        {
            this.categoriesRepostitory = categoriesRepostitory;
            this.ingredientsRepository = ingredientsRepository;
            this.recipesRepository = recipesRepository;
            this.recipeIngredientsRepository = recipeIngredientsRepository;
            this.imagesRepository = imagesRepository;

            var config = Configuration.Default.WithDefaultLoader();
            this.context = BrowsingContext.New(config);
        }

        public async Task ImportRecipesAsync(int fromId, int toId)
        {
            var concurrentBag = this.ScrapeRecipes(fromId, toId);
            Console.WriteLine($"Scraped recipes: {concurrentBag.Count}");

            int addedCount = 0;
            foreach (var recipeDto in concurrentBag)
            {
                var categoryId = await this.GetOrCreateCategoryAsync(recipeDto.CategoryName);

                if (recipeDto.CookingTime.Days >= 1)
                {
                    recipeDto.CookingTime = new TimeSpan(23, 59, 59);
                }

                if (recipeDto.PreparationTime.Days >= 1)
                {
                    recipeDto.CookingTime = new TimeSpan(23, 59, 59);
                }

                // var recipeExists = this.recipesRepository.AllAsNoTracking().Any(x => x.Name == recipeDto.RecipeName);
                // if (recipeExists)
                // {
                //    continue;
                // }
                var recipe = new Recipe
                {
                    Name = recipeDto.RecipeName,
                    Instructions = recipeDto.Instructions,
                    PreparationTime = recipeDto.PreparationTime,
                    CookingTime = recipeDto.CookingTime,
                    PortionsCount = recipeDto.PortionsCount,
                    OriginalUrl = recipeDto.OriginalUrl,
                    CategoryId = categoryId,
                };

                await this.recipesRepository.AddAsync(recipe);

                var ingredients = recipeDto.Ingredients
                    .Select(i => i.Split(" - ", 2))
                    .Where(i => i.Length >= 2)
                    .ToArray();

                foreach (var ingredient in ingredients)
                {
                    var ingredientId = await this.GetOrCreateIngredientAsync(ingredient[0].Trim());
                    var qty = ingredient[1].Trim();

                    var recipeIngredient = new RecipeIngredient
                    {
                        IngredientId = ingredientId,
                        Recipe = recipe,
                        Quantity = qty,
                    };

                    await this.recipeIngredientsRepository.AddAsync(recipeIngredient);
                }

                var image = new Image
                {
                    RemoteImageUrl = recipeDto.ImageUrl,
                    Recipe = recipe,
                };

                await this.imagesRepository.AddAsync(image);

                if (++addedCount % 1000 == 0)
                {
                    await this.recipesRepository.SaveChangesAsync();
                    Console.WriteLine($"Saved Count: {addedCount}");
                }
            }

            await this.recipesRepository.SaveChangesAsync();
            Console.WriteLine($"Count: {addedCount}");
        }

        private ConcurrentBag<RecipeDto> ScrapeRecipes(int fromId, int toId)
        {
            var concurrentBag = new ConcurrentBag<RecipeDto>();
            Parallel.For(fromId, toId, i =>
            {
                try
                {
                    var recipe = this.GetRecipe(i);
                    concurrentBag.Add(recipe);
                }
                catch
                {
                }
            });

            return concurrentBag;
        }

        private RecipeDto GetRecipe(int id)
        {
            var client = new HttpClient();

            var url = string.Format(BaseUrl, id);

            var response = client
                .GetAsync(url)
                .GetAwaiter().GetResult();

            var html = string.Empty;

            if (response.IsSuccessStatusCode)
            {
                html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine($"{id} Not Found");
                throw new InvalidOperationException();
            }

            var document = this.context
                .OpenAsync(request => request.Content(html))
                .GetAwaiter().GetResult();

            if (document.DocumentElement.OuterHtml.Contains("Тази страница не е намерена"))
            {
                Console.WriteLine($"{id} Not Found");
                throw new InvalidOperationException();
            }

            // var document = this.context.OpenAsync(url).GetAwaiter().GetResult(); -> не работи, затова си вземам html-a с HttpClient :(

            // if (document.StatusCode == HttpStatusCode.NotFound || document.DocumentElement.OuterHtml.Contains("Тази страница не е намерена"))
            // {
            //    throw new InvalidOperationException();
            // }
            var recipe = new RecipeDto();

            var recipeNameCategory = document
                .QuerySelectorAll("#recEntity > div.brdc")
                .Select(x => x.TextContent)
                .FirstOrDefault()?
                .Split('»', StringSplitOptions.RemoveEmptyEntries)
                .Reverse()
                .ToArray();

            recipe.CategoryName = recipeNameCategory[1].Trim();

            recipe.RecipeName = recipeNameCategory[0].Trim();

            var instructions = document
                .QuerySelectorAll(".text > p")
                .Select(x => x.TextContent)
                .ToList();

            recipe.Instructions = string.Join(Environment.NewLine, instructions);

            var time = document
                .QuerySelectorAll(".icb-prep");

            if (time.Length > 0)
            {
                var prepTime = time[0]
                    .TextContent
                    .Replace("Приготвяне", string.Empty)
                    .Replace(" мин.", string.Empty);

                recipe.PreparationTime = TimeSpan.FromMinutes(int.Parse(prepTime));
            }

            if (time.Length > 1)
            {
                var cookingTime = time[1]
                    .TextContent
                    .Replace("Готвене", string.Empty)
                    .Replace(" мин.", string.Empty);

                recipe.CookingTime = TimeSpan.FromMinutes(int.Parse(cookingTime));
            }

            var portionsCount = document
                .QuerySelector(".icb-fak")?
                .TextContent.Replace("Порции", string.Empty);

            recipe.PortionsCount = int.Parse(portionsCount);

            recipe.ImageUrl = "https://recepti.gotvach.bg" + document.QuerySelector("#gall img")?.GetAttribute("src");

            var ingredients = document
                .QuerySelectorAll(".products > ul > li")
                .Select(x => x.TextContent)
                .ToList();

            recipe.Ingredients.AddRange(ingredients);

            recipe.OriginalUrl = url;

            Console.WriteLine($"{id} Found");
            return recipe;
        }

        private async Task<int> GetOrCreateIngredientAsync(string name)
        {
            var ingredient = this.ingredientsRepository
                .AllAsNoTracking()
                .FirstOrDefault(x => x.Name == name);

            if (ingredient != null)
            {
                return ingredient.Id;
            }

            ingredient = new Ingredient
            {
                Name = name,
            };

            await this.ingredientsRepository.AddAsync(ingredient);
            await this.ingredientsRepository.SaveChangesAsync();

            return ingredient.Id;
        }

        private async Task<int> GetOrCreateCategoryAsync(string categoryName)
        {
            var category = this.categoriesRepostitory
                .AllAsNoTracking()
                .FirstOrDefault(x => x.Name == categoryName);

            if (category != null)
            {
                return category.Id;
            }

            category = new Category
            {
                Name = categoryName,
            };

            await this.categoriesRepostitory.AddAsync(category);
            await this.categoriesRepostitory.SaveChangesAsync();

            return category.Id;
        }
    }
}
