namespace MyRecipes.Services.Data
{
    using System.Collections.Generic;
    using System.Linq;

    using MyRecipes.Data.Common.Repositories;
    using MyRecipes.Data.Models;

    public class CategoriesService : ICategoriesService
    {
        private readonly IDeletableEntityRepository<Category> categoriesRepository;

        public CategoriesService(IDeletableEntityRepository<Category> categoriesRepository)
        {
            this.categoriesRepository = categoriesRepository;
        }

        public IEnumerable<KeyValuePair<string, string>> GetAllAsKeyValuePairs()
        {
            return this.categoriesRepository
                .AllAsNoTracking() // няма да бъдат траквани и при SaveChanges() няма да бъдат нанесени промените (което в случая не ни е необходимо)
                .Select(x => new { x.Id, x.Name })
                .ToList()
                .OrderBy(x => x.Name)
                .Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name));

            // първата проекция е към анонимен тип, тъй като ef core ще гръмне, ако се опитаме да направим проекция към KeyValuePair,
            // тъй като няма да успее да я преведе до sql;
            // в такивa случаи правим проекция, за да спрем да работим с базата, след което локално си правим необходимата ни проекция;
            // така хем спестяваме трафик от базата, правейки проекция към анонимен тип и материализирайки завката едва след това, хем си
            // спестяваме грешки от ef core
        }
    }
}
