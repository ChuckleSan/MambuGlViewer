using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class EditDatesModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public DateTime EarliestDate { get; set; }
        public bool HasUpdated { get; set; } = false;

        private readonly IWebHostEnvironment _environment;

        public EditDatesModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            LoadTransactions();
            if (Transactions.Any())
            {
                EarliestDate = Transactions.Min(t => t.bookingDate);
            }
        }

        public IActionResult OnPost(DateTime baseDate)
        {
            LoadTransactions();
            if (!Transactions.Any())
            {
                TempData["Message"] = "No transactions to update.";
                return Page();
            }

            // Step 2: Find earliest date and calculate offset
            EarliestDate = Transactions.Min(t => t.bookingDate);
            TimeSpan offset = baseDate - EarliestDate;

            // Step 3: Update dates for each transaction
            foreach (var tx in Transactions)
            {
                tx.bookingDate = tx.bookingDate + offset;
                tx.creationDate = tx.creationDate + offset;
            }

            // Save updated transactions back to file
            SaveTransactions();

            HasUpdated = true;
            TempData["Message"] = $"Dates updated successfully. Offset applied: {offset.TotalDays} days.";
            return Page();
        }

        private void LoadTransactions()
        {
            string filePath = GetTransactionsFilePath();
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    Transactions = JsonSerializer.Deserialize<List<Transaction>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<Transaction>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading transactions: {ex.Message}");
                    Transactions = new List<Transaction>();
                }
            }
        }

        private void SaveTransactions()
        {
            string filePath = GetTransactionsFilePath();
            try
            {
                string json = JsonSerializer.Serialize(Transactions,
                    new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving transactions: {ex.Message}");
                TempData["Message"] = "Error saving updated dates: " + ex.Message;
            }
        }

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }
    }
}