using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class GLTotalsModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<GLTotal> GLTotals { get; set; } = new List<GLTotal>();

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
                // Identify paired transactions to exclude
                var pairedTransactions = Transactions
                    .GroupBy(t => new { t.transactionId, t.glAccount.glCode, t.amount })
                    .Where(g => g.Count() == 2 && g.Any(t => t.type == "DEBIT") && g.Any(t => t.type == "CREDIT"))
                    .SelectMany(g => g)
                    .ToList();

                // Filter out paired transactions
                var filteredTransactions = Transactions
                    .Where(t => !pairedTransactions.Contains(t))
                    .ToList();

                GLTotals = filteredTransactions
                    .GroupBy(t => new
                    {
                        t.glAccount.glCode,
                        t.glAccount.name,
                        t.glAccount.type
                    })
                    .Select(g => new GLTotal
                    {
                        GLCode = g.Key.glCode,
                        GLName = g.Key.name,
                        GLType = g.Key.type,
                        TotalCredits = g.Where(t => t.type == "CREDIT").Sum(t => t.amount),
                        TotalDebits = g.Where(t => t.type == "DEBIT").Sum(t => t.amount),
                        ValueAtEnd = g.Where(t => t.type == "CREDIT").Sum(t => t.amount) -
                                     g.Where(t => t.type == "DEBIT").Sum(t => t.amount)
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

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }
    }

    public class GLTotal
    {
        public string GLCode { get; set; } = string.Empty;
        public string GLName { get; set; } = string.Empty;
        public string GLType { get; set; } = string.Empty;
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal ValueAtEnd { get; set; }
    }
}