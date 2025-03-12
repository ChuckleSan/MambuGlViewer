using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Text.Json;

using MambuGLViewer.Models;

namespace MambuGLViewer.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsUploaded { get; set; }
        public int RecordCount { get; set; }

        private readonly IWebHostEnvironment _environment;

        public IndexModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            // Nothing to load on GET
        }

        public IActionResult OnPost(IFormFile jsonFile)
        {
            if (jsonFile != null && jsonFile.Length > 0)
            {
                try
                {
                    using var stream = new StreamReader(jsonFile.OpenReadStream());
                    string jsonContent = stream.ReadToEnd();
                    var transactions = JsonSerializer.Deserialize<List<Transaction>>(jsonContent);

                    if (transactions != null && transactions.Any())
                    {
                        SaveTransactions(transactions);
                        IsUploaded = true;
                        RecordCount = transactions.Count;
                    }
                    else
                    {
                        ModelState.AddModelError("", "No valid transactions found in the file.");
                    }
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("", $"Invalid JSON format: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error processing file: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("", "Please upload a valid JSON file.");
            }

            return Page();
        }

        private void SaveTransactions(List<Transaction> transactions)
        {
            string filePath = GetTransactionsFilePath();
            string uploadsDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            string json = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }
    }
}

