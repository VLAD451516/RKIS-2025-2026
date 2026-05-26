using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RestaurantSystem.API.Controllers;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;
using Xunit;

namespace RestaurantSystem.Tests.UnitTests
{
    public class RestaurantsControllerTests
    {
        private readonly Mock<IRepository<Restaurant>> _mockRepo;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly RestaurantsController _controller;

        public RestaurantsControllerTests()
        {
            _mockRepo = new Mock<IRepository<Restaurant>>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new RestaurantsController(_mockRepo.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenRestaurantDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Restaurant?)null);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsRestaurant_WhenRestaurantExists()
        {
            // Arrange
            var restaurant = new Restaurant { Id = 1, Name = "Test" };
            _mockRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(restaurant);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<RestaurantDto>>(result);
            var dto = Assert.IsType<RestaurantDto>(actionResult.Value);
            Assert.Equal(restaurant.Name, dto.Name);
        }
    }
}
