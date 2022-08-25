namespace MyRecipes.Services.Data.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Moq;
    using MyRecipes.Data.Common.Repositories;
    using MyRecipes.Data.Models;
    using Xunit;

    public class VotesServiceTests
    {
        [Fact]
        public async Task WhenUserVotesTwiceOnlyOneVoteShoudBeCounted()
        {
            var list = new List<Vote>();
            var mockRepo = new Mock<IRepository<Vote>>();
            mockRepo.Setup(x => x.All()).Returns(list.AsQueryable());
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Vote>())).Callback((Vote vote) => list.Add(vote));

            var repo = new FakeRepository();
            var service = new VotesService(repo);

            var serviceMocked = new VotesService(mockRepo.Object);

            await service.SetVoteAsync(1, "1", 1);
            await service.SetVoteAsync(1, "1", 5);
            await service.SetVoteAsync(1, "1", 5);
            await service.SetVoteAsync(1, "1", 5);
            await service.SetVoteAsync(1, "1", 5);

            await serviceMocked.SetVoteAsync(1, "2", 2);
            await serviceMocked.SetVoteAsync(1, "2", 3);

            Assert.Equal(1, repo.All().Count());
            Assert.Equal(5, repo.All().First().Value);

            Assert.Single(list);
            Assert.Equal(3, list.First().Value);
        }

        [Fact]
        public async Task WhenTwoUsersVoteForTheSameRecipeTheAverageVoteShouldBeCorrect()
        {
            var list = new List<Vote>();
            var mockRepo = new Mock<IRepository<Vote>>();
            mockRepo.Setup(x => x.All()).Returns(list.AsQueryable());
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Vote>())).Callback((Vote vote) => list.Add(vote));

            var service = new VotesService(mockRepo.Object);

            await service.SetVoteAsync(2, "Niki", 5);
            await service.SetVoteAsync(2, "Pesho", 1);
            await service.SetVoteAsync(2, "Niki", 2);

            mockRepo.Verify(x => x.AddAsync(It.IsAny<Vote>()), Times.Exactly(2));
            Assert.Equal(2, list.Count);
            Assert.Equal(1.5, service.GetAverageVote(2));
        }
    }

    public class FakeRepository : IRepository<Vote>
    {
        private List<Vote> list = new List<Vote>();

        public Task AddAsync(Vote entity)
        {
            this.list.Add(entity);
            return Task.CompletedTask;
        }

        public IQueryable<Vote> All()
        {
            return this.list.AsQueryable();
        }

        public IQueryable<Vote> AllAsNoTracking()
        {
            return this.list.AsQueryable();
        }

        public void Delete(Vote entity)
        {
        }

        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(0);
        }

        public void Update(Vote entity)
        {
        }
    }
}
