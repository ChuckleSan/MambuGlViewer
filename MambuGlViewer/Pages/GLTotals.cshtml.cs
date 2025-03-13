using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class GLTotalsModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<GLAccountTotal> GLAccountTotals { get; set; } = new List<GLAccountTotal>();

        private readonly IWebHostEnvironment _environment;

        public GLTotalsModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            LoadTransactions();
            if (Transactions.Any())
            {
                GLAccountTotals = Transactions
                    .GroupBy(t => new { t.glAccount.glCode, t.glAccount.name, t.glAccount.type, t.glAccount.currency.currencyCode })
                    .Select(g => new GLAccountTotal
                    {
                        GLCode = g.Key.glCode,
                        GLName = g.Key.name,
                        Type = g.Key.type,
                        CurrencyCode = g.Key.currencyCode,
                        TotalDebits = g.Where(t => t.type == "DEBIT").Sum(t => t.amount),
                        TotalCredits = g.Where(t => t.type == "CREDIT").Sum(t => t.amount),
                        ValueAtEnd = g.Where(t => t.type == "CREDIT").Sum(t => t.amount) - g.Where(t => t.type == "DEBIT").Sum(t => t.amount)
                    })
                    .OrderBy(t => t.GLCode)
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

    public class GLAccountTotal
    {
        public string GLCode { get; set; }
        public string GLName { get; set; }
        public string Type { get; set; }
        public string CurrencyCode { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal ValueAtEnd { get; set; }
    }
}