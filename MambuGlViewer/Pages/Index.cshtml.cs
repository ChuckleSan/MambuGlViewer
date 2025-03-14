using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public IndexModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a file to upload.";
                return Page();
            }

            if (!file.FileName.EndsWith(".json"))
            {
                TempData["Message"] = "Only JSON files are supported.";
                return Page();
            }

            string filePath = GetTransactionsFilePath();
            try
            {
                string uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string json = System.IO.File.ReadAllText(filePath);
                var transactions = JsonSerializer.Deserialize<List<Transaction>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (transactions == null || !transactions.Any())
                {
                    TempData["Message"] = "Uploaded file is empty or invalid.";
                    return Page();
                }

                TempData["Message"] = "File uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error uploading file: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnGetDownload()
        {
            string filePath = GetTransactionsFilePath();
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Message"] = "No transactions file available to download.";
                return Page();
            }

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/json", "transactions.json");
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error downloading file: {ex.Message}";
                return Page();
            }
        }

        public string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }
    }
}