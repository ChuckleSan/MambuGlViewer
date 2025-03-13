using System.Text.Json;

using MambuGLViewer.Models; // Ensure this is included

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class AllRecordsModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<Transaction> PagedTransactions { get; set; } = new List<Transaction>();
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int SkipCount { get; set; }

        private readonly IWebHostEnvironment _environment;
        private List<CustomerMapping> _customerMappings;

        public AllRecordsModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet(string action = null, int currentPage = 1, int? targetPage = null, int pageSize = 5)
        {
            LoadTransactions();
            LoadCustomerMappings();
            if (Transactions.Any())
            {
                TotalRecords = Transactions.Count;
                PageSize = pageSize > 0 ? pageSize : 5;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                // Determine the target page based on the action
                int nextPage;
                switch (action?.ToLower())
                {
                    case "next":
                        nextPage = currentPage + 1;
                        break;
                    case "prev":
                        nextPage = currentPage - 1;
                        break;
                    case "page":
                        nextPage = targetPage ?? currentPage;
                        break;
                    default:
                        nextPage = currentPage; // Initial load or no action
                        break;
                }

                // Clamp nextPage to valid range
                CurrentPage = nextPage < 1 ? 1 : nextPage > TotalPages ? TotalPages : nextPage;

                // Calculate skip based on the target page (now CurrentPage)
                SkipCount = (CurrentPage - 1) * PageSize;
                PagedTransactions = Transactions
                    .Skip(SkipCount)
                    .Take(PageSize)
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
}