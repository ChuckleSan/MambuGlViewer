using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class GLSummaryModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<GLSummary> GLSummaries { get; set; } = new List<GLSummary>();

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
                GLSummaries = Transactions
                    .GroupBy(t => new
                    {
                        t.glAccount.glCode,
                        t.glAccount.name,
                        t.glAccount.type
                    })
                    .Select(g => new GLSummary
                    {
                        GLCode = g.Key.glCode,
                        GLName = g.Key.name,
                        Type = g.Key.type,
                        TotalDebits = g.Where(t => t.type == "DEBIT").Sum(t => t.amount),
                        TotalCredits = g.Where(t => t.type == "CREDIT").Sum(t => t.amount),
                        Balance = g.Where(t => t.type == "CREDIT").Sum(t => t.amount) -
                                 g.Where(t => t.type == "DEBIT").Sum(t => t.amount)
                    })
                    .OrderBy(s => s.GLCode)
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

    public class GLSummary
    {
        public string GLCode { get; set; } = string.Empty;
        public string GLName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal Balance { get; set; }
    }
}