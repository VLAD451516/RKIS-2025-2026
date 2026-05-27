using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.API.Services;

namespace RestaurantSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<string>> Upload(IFormFile file)
        {
            try
            {
                var fileName = await _fileService.SaveFileAsync(file);
                return Ok(fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{fileName}")]
        public IActionResult Get(string fileName)
        {
            var filePath = _fileService.GetFilePath(fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound("Файл не найден");

            var extension = Path.GetExtension(fileName).ToLower();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(filePath, contentType);
        }
    }
}
