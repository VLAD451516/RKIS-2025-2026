namespace RestaurantSystem.API.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file);
        void DeleteFile(string fileName);
        string GetFilePath(string fileName);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadsFolder;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
            _uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024) throw new Exception("File too large (max 5MB)");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) throw new Exception("Invalid file type");

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(_uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public void DeleteFile(string fileName)
        {
            var filePath = Path.Combine(_uploadsFolder, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public string GetFilePath(string fileName) => Path.Combine(_uploadsFolder, fileName);
    }
}
