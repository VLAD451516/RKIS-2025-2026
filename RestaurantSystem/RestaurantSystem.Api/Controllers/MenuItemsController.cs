using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Models;
using RestaurantSystem.Api.Services;
using RestaurantSystem.Core.Entities;
using RestaurantSystem.Core.Interfaces;

namespace RestaurantSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;

    public MenuItemsController(IUnitOfWork unitOfWork, IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<MenuItem>> Create([FromForm] MenuItemDto dto, IFormFile? image)
    {
        var menuItem = new MenuItem
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            RestaurantId = dto.RestaurantId
        };

        if (image != null)
        {
            menuItem.ImagePath = await _fileService.SaveFileAsync(image, "menuitems");
        }

        await _unitOfWork.MenuItems.AddAsync(menuItem);
        await _unitOfWork.CompleteAsync();

        return CreatedAtAction(nameof(GetById), new { id = menuItem.Id }, menuItem);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MenuItem>> GetById(int id)
    {
        var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(id);
        if (menuItem == null) return NotFound();
        return Ok(menuItem);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromForm] MenuItemDto dto, IFormFile? image)
    {
        var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(id);
        if (menuItem == null) return NotFound();

        menuItem.Name = dto.Name;
        menuItem.Description = dto.Description;
        menuItem.Price = dto.Price;

        if (image != null)
        {
            if (!string.IsNullOrEmpty(menuItem.ImagePath))
                _fileService.DeleteFile(menuItem.ImagePath);
            menuItem.ImagePath = await _fileService.SaveFileAsync(image, "menuitems");
        }

        _unitOfWork.MenuItems.Update(menuItem);
        await _unitOfWork.CompleteAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(id);
        if (menuItem == null) return NotFound();

        if (!string.IsNullOrEmpty(menuItem.ImagePath))
            _fileService.DeleteFile(menuItem.ImagePath);

        _unitOfWork.MenuItems.Remove(menuItem);
        await _unitOfWork.CompleteAsync();

        return NoContent();
    }
}
