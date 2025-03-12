using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class EditDatesModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        [BindProperty]
        public string NewBookingDate { get; set; }
        [BindProperty]
        public string NewCreationDate { get; set; }
        public bool IsUpdated { get; set; }

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
                // Set default values to first transaction's dates
                NewBookingDate = Transactions.First().bookingDate.ToString("yyyy-MM-dd");
                NewCreationDate = Transactions.First().creationDate.ToString("yyyy-MM-dd");
            }
        }

        public IActionResult OnPost()
        {
            LoadTransactions();
            if (Transactions.Any())
            {
                bool updated = false;

                // Try to parse new booking date (date only)
                if (!string.IsNullOrWhiteSpace(NewBookingDate) && DateTime.TryParse(NewBookingDate, out DateTime newBookingDateOnly))
                {
                    foreach (var transaction in Transactions)
                    {
                        // Keep the original time, apply the new date
                        DateTime originalTime = transaction.bookingDate;
                        transaction.bookingDate = new DateTime(
                            newBookingDateOnly.Year,
                            newBookingDateOnly.Month,
                            newBookingDateOnly.Day,
                            originalTime.Hour,
                            originalTime.Minute,
                            originalTime.Second,
                            DateTimeKind.Utc);
                    }
                    updated = true;
                }

                // Try to parse new creation date (date only)
                if (!string.IsNullOrWhiteSpace(NewCreationDate) && DateTime.TryParse(NewCreationDate, out DateTime newCreationDateOnly))
                {
                    foreach (var transaction in Transactions)
                    {
                        // Keep the original time, apply the new date
                        DateTime originalTime = transaction.creationDate;
                        transaction.creationDate = new DateTime(
                            newCreationDateOnly.Year,
                            newCreationDateOnly.Month,
                            newCreationDateOnly.Day,
                            originalTime.Hour,
                            originalTime.Minute,
                            originalTime.Second,
                            DateTimeKind.Utc);
                    }
                    updated = true;
                }

                if (updated)
                {
                    SaveTransactions();
                    IsUpdated = true;
                }
                else
                {
                    ModelState.AddModelError("", "No valid dates provided to update.");
                }
            }
            return Page();
        }

        public IActionResult OnGetDownload()
        {
            LoadTransactions();
            if (Transactions.Any())
            {
                string json = JsonSerializer.Serialize(Transactions, new JsonSerializerOptions { WriteIndented = true });
                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
                return File(fileBytes, "application/json", "transactions_modified.json");
            }
            return RedirectToPage();
        }

        private void LoadTransactions()
        {
            string filePath = GetTransactionsFilePath();
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    Transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
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
            string json = JsonSerializer.Serialize(Transactions, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }
    }
}