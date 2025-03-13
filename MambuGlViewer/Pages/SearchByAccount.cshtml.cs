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
        [BindProperty(SupportsGet = true)]
        public string SearchAccountKey { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)]
        public string TransactionId { get; set; }
        public bool SearchPerformed { get; set; }

        private readonly IWebHostEnvironment _environment;
        private List<CustomerMapping> _customerMappings;

        public SearchByAccountModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet(string accountKey = null, string transactionId = null)
        {
            LoadTransactions();
            LoadCustomerMappings();

            if (!string.IsNullOrEmpty(accountKey))
            {
                SearchAccountKey = accountKey;
                SearchPerformed = true;

                if (!string.IsNullOrEmpty(transactionId))
                {
                    // Show all transactions for the selected transactionId
                    FilteredTransactions = Transactions
                        .Where(t => t.transactionId == transactionId)
                        .ToList();
                    TransactionId = transactionId; // Persist for Back link condition
                }
                else
                {
                    // Initial search by accountKey or return from detail
                    FilteredTransactions = Transactions
                        .Where(t => t.accountKey == accountKey)
                        .ToList();
                }
            }
        }

        public IActionResult OnPost(string accountKey)
        {
            LoadTransactions();
            LoadCustomerMappings();
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
}