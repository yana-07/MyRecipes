namespace MyRecipes.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using MyRecipes.Data.Common.Repositories;
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Mapping;
    using MyRecipes.Web.ViewModels.Recipes;

    public class RecipesService : IRecipesService
    {
        private readonly string[] allowedExtensions = new[] { "jpg", "png", "gif" };

        private readonly IDeletableEntityRepository<Recipe> recipesRepository;
        private readonly IDeletableEntityRepository<Ingredient> ingredientsRepository;

        public RecipesService(
            IDeletableEntityRepository<Recipe> recipesRepository,
            IDeletableEntityRepository<Ingredient> ingredientsRepository)
        {
            this.recipesRepository = recipesRepository;
            this.ingredientsRepository = ingredientsRepository;
        }

        // TODO: implement IMapTo<Recipe> for CreateRecipeInputModel with custom mappings for cooking and prep time
        public async Task CreateAsync(CreateRecipeInputModel input, string userId, string imagePath)
        {
            var recipe = new Recipe
            {
                CategoryId = input.CategoryId,
                Name = input.Name,
                CookingTime = TimeSpan.FromMinutes(input.CookingTime),
                PreparationTime = TimeSpan.FromMinutes(input.PreparationTime),
                Instructions = input.Instructions,
                PortionsCount = input.PortionsCount,
                AddedByUserId = userId,
            };

            foreach (var inputIngredient in input.Ingredients)
            {
                var ingredient = this.ingredientsRepository.All().FirstOrDefault(x => x.Name == inputIngredient.IngredientName);
                if (ingredient == null)
                {
                    ingredient = new Ingredient { Name = inputIngredient.IngredientName };
                }

                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Ingredient = ingredient,
                    Quantity = inputIngredient.Quantity,
                });
            }

            // /wwwroot/images/recipes/{id}.{ext}
            Directory.CreateDirectory($"{imagePath}/recipes/");
            foreach (var image in input.Images)
            {
                var extension = Path.GetExtension(image.FileName).TrimStart('.');
                if (!this.allowedExtensions.Any(x => extension.EndsWith(x)))
                {
                    throw new Exception($"Invalid image extension {extension}");
                }

                var dbImage = new Image
                {
                    AddedByUserId = userId,

                    // при SaveChanges() EF Core построява граф от обектите, които следва да влязат в базата, прави топологично сортиране, следователно първи влизат
                    // онези обекти, които не зависят от други, в случая рецептата ще влeзе първа, ще й се сетне id-то, така че след нея да може да бъде записан image
                    // и id-то на рецептата в него да се сетне автоматично
                    // Recipe = recipe, -> в случая излишно, тъй като добавяме снимката директно към рецептата
                    Extension = extension,
                };

                recipe.Images.Add(dbImage); // може и директно Recipe = recipe при конструиране на image обекта

                var physicalPath = $"{imagePath}/recipes/{dbImage.Id}.{extension}";

                // препоръчително е файловете физически да се въхраняват в wwwroot папката, тъй като веднъж съхранени в нея, те могат да бъдат линквани през html-a
                using (Stream fileStream = new FileStream(physicalPath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream); // директна връзка между двата стрийма: четем от юзъра и пишем във файловата система
                }
            }

            await this.recipesRepository.AddAsync(recipe);
            await this.recipesRepository.SaveChangesAsync(); // добавяйки рецептата, всички нейни снимки ще се запишат в базата, както и съставките и (EF Core)
        }

        public IEnumerable<T> GetAll<T>(int page, int itemsPerPage = 12)
        {
            // offset = (page - 1) * pageSize
            return this.recipesRepository.AllAsNoTracking()
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * itemsPerPage).Take(itemsPerPage)
                .To<T>()
                .ToList();
        }

        public T GetById<T>(int id)
        {
            var recipe = this.recipesRepository.AllAsNoTracking()
                .Where(x => x.Id == id)
                .To<T>()
                .FirstOrDefault();

            return recipe;
        }

        public int GetCount()
        {
            return this.recipesRepository.AllAsNoTracking().Count();
        }
    }
}
