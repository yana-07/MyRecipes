﻿namespace MyRecipes.Web.ViewModels.Recipes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AutoMapper;
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Mapping;

    public class SingleRecipeViewModel : IMapFrom<Recipe>, IHaveCustomMappings
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string CategoryName { get; set; }

        public DateTime CreatedOn { get; set; }

        public string AddedByUserUsername { get; set; }

        public string ImageUrl { get; set; }

        public string Instructions { get; set; }

        public TimeSpan PreparationTime { get; set; }

        public TimeSpan CookingTime { get; set; }

        public int PortionsCount { get; set; }

        public int CategoryRecipesCount { get; set; }

        public string OriginalUrl { get; set; }

        public double AverageVote { get; set; }

        public IEnumerable<IngredientsViewModel> Ingredients { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<Recipe, SingleRecipeViewModel>()
                .ForMember(vm => vm.AverageVote, opt =>
                    opt.MapFrom(dbm => dbm.Votes.Count == 0 ? 0 : dbm.Votes.Average(v => v.Value)))
                .ForMember(vm => vm.ImageUrl, opt =>
                {
                    opt.MapFrom(dbm =>
                       dbm.Images.FirstOrDefault().RemoteImageUrl != null ?
                       dbm.Images.FirstOrDefault().RemoteImageUrl :
                       "/images/recipes/" + dbm.Images.FirstOrDefault().Id + "." + dbm.Images.FirstOrDefault().Extension);
                });
        }
    }
}
