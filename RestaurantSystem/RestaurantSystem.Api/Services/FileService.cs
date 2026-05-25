namespace RestaurantSystem.Api.Services;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string subDirectory);
    void DeleteFile(string filePath);
}

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long _maxFileSize = 5 * 1024 * 1024; // 5MB

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (file.Length > _maxFileSize)
            throw new ArgumentException("File size exceeds 5MB");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            throw new ArgumentException("Invalid file type");

        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", subDirectory);
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine("uploads", subDirectory, fileName).Replace("\\", "/");
    }

    public void DeleteFile(string filePath)
    {
        var fullPath = Path.Combine(_environment.WebRootPath, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
