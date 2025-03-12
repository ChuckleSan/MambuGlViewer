using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class GLSummaryModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<GLAccountSummary> GLAccountSummaries { get; set; } = new List<GLAccountSummary>();

        private readonly IWebHostEnvironment _environment;

        public GLSummaryModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            LoadTransactions();
            if (Transactions.Any())
            {
                GLAccountSummaries = Transactions
                    .GroupBy(t => new
                    {
                        t.glAccount.currency.currencyCode, // Group by currency first
                        t.accountKey,                      // Then by accountKey
                        t.glAccount.glCode,
                        t.glAccount.name
                    })
                    .Select(g => new GLAccountSummary
                    {
                        CurrencyCode = g.Key.currencyCode,
                        AccountKey = g.Key.accountKey,
                        GLCode = g.Key.glCode,
                        GLName = g.Key.name,
                        TotalDebits = g.Where(t => t.type == "DEBIT").Sum(t => t.amount),
                        TotalCredits = g.Where(t => t.type == "CREDIT").Sum(t => t.amount)
                    })
                    .OrderBy(s => s.CurrencyCode)  // Sort by currency first
                    .ThenBy(s => s.AccountKey)     // Then by accountKey
                    .ThenBy(s => s.GLCode)
                    .ToList();
            }
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

    public class GLAccountSummary
    {
        public string CurrencyCode { get; set; }
        public string AccountKey { get; set; }
        public string GLCode { get; set; }
        public string GLName { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
    }
}