using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class GLDetailsModel : PageModel
    {
        public string GLCode { get; set; } = string.Empty;
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        private readonly IWebHostEnvironment _environment;
        private List<CustomerMapping> _customerMappings = new List<CustomerMapping>();

        public GLDetailsModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet(string glCode)
        {
            GLCode = glCode;
            LoadTransactions();
            LoadCustomerMappings();
            if (Transactions.Any())
            {
                Transactions = Transactions
                    .Where(t => t.glAccount.glCode == glCode)
                    .OrderBy(t => t.bookingDate)
                    .ThenBy(t => t.entryID)
                    .ToList();
            }
        }

        public bool IsMainAccount(string accountKey)
        {
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

        private void LoadCustomerMappings()
        {
            string filePath = GetCustomerMappingsFilePath();
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    _customerMappings = JsonSerializer.Deserialize<List<CustomerMapping>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<CustomerMapping>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading customer mappings: {ex.Message}");
                    _customerMappings = new List<CustomerMapping>();
                }
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
}