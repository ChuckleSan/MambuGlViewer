using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class SearchByAccountModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<Transaction> FilteredTransactions { get; set; } = new List<Transaction>();
        public string SearchAccountKey { get; set; } = string.Empty;
        public bool SearchPerformed { get; set; }

        private readonly IWebHostEnvironment _environment;

        public SearchByAccountModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            LoadTransactions();
        }

        public IActionResult OnPost(string accountKey)
        {
            LoadTransactions();
            if (Transactions.Any())
            {
                SearchAccountKey = accountKey;
                SearchPerformed = true;
                FilteredTransactions = Transactions
                    .Where(t => t.accountKey == accountKey)
                    .ToList();
            }

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
                    Transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading transactions: {ex.Message}");
                    Transactions = new List<Transaction>();
                }
            }
        }

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }
    }
}