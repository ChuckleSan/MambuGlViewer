using System.Text.Json;

using MambuGLViewer.Models; // Ensure this is included

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class GLTotalsModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<GLAccountTotal> GLAccountTotals { get; set; } = new List<GLAccountTotal>();

        private readonly IWebHostEnvironment _environment;
        private List<CustomerMapping> _customerMappings;

        public GLTotalsModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            LoadTransactions();
            LoadCustomerMappings();
            if (Transactions.Any())
            {
                GLAccountTotals = Transactions
                    .GroupBy(t => new { t.glAccount.glCode, t.glAccount.name, t.glAccount.type, t.glAccount.currency.currencyCode, t.accountKey }) // Include accountKey in grouping
                    .Select(g => new GLAccountTotal
                    {
                        GLCode = g.Key.glCode,
                        GLName = g.Key.name,
                        Type = g.Key.type,
                        CurrencyCode = g.Key.currencyCode,
                        AccountKey = g.Key.accountKey, // Add accountKey to the model
                        TotalDebits = g.Where(t => t.type == "DEBIT").Sum(t => t.amount),
                        TotalCredits = g.Where(t => t.type == "CREDIT").Sum(t => t.amount),
                        ValueAtEnd = g.Where(t => t.type == "CREDIT").Sum(t => t.amount) - g.Where(t => t.type == "DEBIT").Sum(t => t.amount)
                    })
                    .OrderBy(t => t.GLCode)
                    .ToList();
            }
        }

        public bool IsMainAccount(string accountKey)
        {
            if (_customerMappings == null || !_customerMappings.Any())
            {
                return false; // Default to sub-account if no mappings
            }
            return _customerMappings.Any(c => c.EurMainAccountKey == accountKey || c.UsdMainAccountKey == accountKey);
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

        private void LoadCustomerMappings()
        {
            string filePath = GetCustomerMappingsFilePath();
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    _customerMappings = JsonSerializer.Deserialize<List<CustomerMapping>>(json) ?? new List<CustomerMapping>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading customer mappings: {ex.Message}");
                    _customerMappings = new List<CustomerMapping>();
                }
            }
            else
            {
                _customerMappings = new List<CustomerMapping>();
            }
        }

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }

        private string GetCustomerMappingsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "customer_mappings.json");
        }
    }

    public class GLAccountTotal
    {
        public string GLCode { get; set; }
        public string GLName { get; set; }
        public string Type { get; set; }
        public string CurrencyCode { get; set; }
        public string AccountKey { get; set; } // Added to track accountKey
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal ValueAtEnd { get; set; }
    }
}