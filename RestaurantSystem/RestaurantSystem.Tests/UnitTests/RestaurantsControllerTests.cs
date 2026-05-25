using Moq;
using RestaurantSystem.Api.Controllers;
using RestaurantSystem.Core.Entities;
using RestaurantSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RestaurantSystem.Api.Services;

namespace RestaurantSystem.Tests.UnitTests;

public class RestaurantsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IFileService> _mockFileService;
    private readonly IMemoryCache _cache;
    private readonly RestaurantsController _controller;

    public RestaurantsControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockFileService = new Mock<IFileService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _controller = new RestaurantsController(_mockUnitOfWork.Object, _mockFileService.Object, _cache);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenRestaurantDoesNotExist()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.Restaurants.GetWithMenuItemsAsync(It.IsAny<int>()))
            .ReturnsAsync((Restaurant?)null);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenRestaurantExists()
    {
        // Arrange
        var restaurant = new Restaurant { Id = 1, Name = "Test" };
        _mockUnitOfWork.Setup(u => u.Restaurants.GetWithMenuItemsAsync(1))
            .ReturnsAsync(restaurant);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRestaurant = Assert.IsType<Restaurant>(okResult.Value);
        Assert.Equal(1, returnedRestaurant.Id);
    }
}
