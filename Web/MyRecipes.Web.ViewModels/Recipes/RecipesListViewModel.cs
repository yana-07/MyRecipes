namespace MyRecipes.Web.ViewModels.Recipes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RecipesListViewModel : PagingViewModel
    {
        public IEnumerable<RecipeInListViewModel> Recipes { get; set; }
    }
}
