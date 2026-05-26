using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;

namespace RestaurantSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuItemsController : ControllerBase
    {
        private readonly IRepository<MenuItem> _menuItemRepo;

        public MenuItemsController(IRepository<MenuItem> menuItemRepo)
        {
            _menuItemRepo = menuItemRepo;
        }

        [HttpGet("restaurant/{restaurantId}")]
        public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetByRestaurantId(int restaurantId)
        {
            var items = await _menuItemRepo.GetQueryable()
                .Where(m => m.RestaurantId == restaurantId)
                .Select(m => new MenuItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ImagePath = m.ImagePath,
                    RestaurantId = m.RestaurantId
                })
                .ToListAsync();

            return items;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MenuItemDto>> GetById(int id)
        {
            var m = await _menuItemRepo.GetByIdAsync(id);
            if (m == null) return NotFound();

            return new MenuItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                ImagePath = m.ImagePath,
                RestaurantId = m.RestaurantId
            };
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<MenuItemDto>> Create(MenuItemDto dto)
        {
            var m = new MenuItem
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                ImagePath = dto.ImagePath,
                RestaurantId = dto.RestaurantId
            };

            await _menuItemRepo.AddAsync(m);
            await _menuItemRepo.SaveChangesAsync();

            dto.Id = m.Id;
            return CreatedAtAction(nameof(GetById), new { id = m.Id }, dto);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, MenuItemDto dto)
        {
            var m = await _menuItemRepo.GetByIdAsync(id);
            if (m == null) return NotFound();

            m.Name = dto.Name;
            m.Description = dto.Description;
            m.Price = dto.Price;
            m.ImagePath = dto.ImagePath;

            _menuItemRepo.Update(m);
            await _menuItemRepo.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _menuItemRepo.GetByIdAsync(id);
            if (m == null) return NotFound();

            _menuItemRepo.Remove(m);
            await _menuItemRepo.SaveChangesAsync();

            return NoContent();
        }
    }
}
