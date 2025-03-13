using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class CustomerSummaryModel : PageModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<CustomerSummary> CustomerSummaries { get; set; } = new List<CustomerSummary>();

        private readonly IWebHostEnvironment _environment;

        public CustomerSummaryModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            LoadTransactions();
            LoadCustomerMappings();
            CalculateSummaries();
        }

        public IActionResult OnPost(string customerEncodedKey, string eurMainAccountKey, string usdMainAccountKey)
        {
            if (!string.IsNullOrWhiteSpace(customerEncodedKey) &&
                (!string.IsNullOrWhiteSpace(eurMainAccountKey) || !string.IsNullOrWhiteSpace(usdMainAccountKey)))
            {
                var mappings = LoadCustomerMappingsRaw();
                var allMainAccounts = mappings.SelectMany(m => new[] { m.EurMainAccountKey, m.UsdMainAccountKey })
                                              .Where(k => !string.IsNullOrEmpty(k))
                                              .ToList();
                if ((eurMainAccountKey != null && allMainAccounts.Contains(eurMainAccountKey)) ||
                    (usdMainAccountKey != null && allMainAccounts.Contains(usdMainAccountKey)))
                {
                    ModelState.AddModelError("", "An account key is already assigned as a main account for another customer.");
                    LoadTransactions();
                    LoadCustomerMappings();
                    CalculateSummaries();
                    return Page();
                }

                mappings.Add(new CustomerMapping
                {
                    CustomerEncodedKey = customerEncodedKey,
                    BranchKey = "8a195893895f9a4301896850ec87503e",
                    EurMainAccountKey = eurMainAccountKey ?? "",
                    UsdMainAccountKey = usdMainAccountKey ?? ""
                });
                SaveCustomerMappings(mappings);
            }
            else
            {
                ModelState.AddModelError("", "Customer Encoded Key and at least one main account key are required.");
            }

            LoadTransactions();
            LoadCustomerMappings();
            CalculateSummaries();
            return Page();
        }

        private void CalculateSummaries()
        {
            if (Transactions.Any() && CustomerMappings.Any())
            {
                var allMainAccountKeys = CustomerMappings
                    .SelectMany(c => new[] { c.EurMainAccountKey, c.UsdMainAccountKey })
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToHashSet();

                // Identify internal transfers (same transactionId, different accountKeys within a customer's set)
                var transactionsById = Transactions
                    .GroupBy(t => t.transactionId)
                    .Where(g => g.Count() > 1) // Only groups with multiple entries could be transfers
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(t => new { t.accountKey, t.type, t.amount }).ToList()
                    );

                CustomerSummaries = CustomerMappings
                    .Select(c =>
                    {
                        var customerAccountKeys = new HashSet<string>(
                            new[] { c.EurMainAccountKey, c.UsdMainAccountKey }.Where(k => !string.IsNullOrEmpty(k))
                        );

                        // Add all sub-account keys for this customer from transactions
                        var subAccountKeys = Transactions
                            .Where(t => !allMainAccountKeys.Contains(t.accountKey) &&
                                        (t.glAccount.currency.currencyCode == "EUR" && c.EurMainAccountKey != null ||
                                         t.glAccount.currency.currencyCode == "USD" && c.UsdMainAccountKey != null))
                            .Select(t => t.accountKey)
                            .Distinct()
                            .ToHashSet();
                        customerAccountKeys.UnionWith(subAccountKeys);

                        // Filter out internal transfers for this customer's accounts
                        var externalTransactions = Transactions
                            .Where(t => customerAccountKeys.Contains(t.accountKey))
                            .GroupBy(t => t.transactionId)
                            .SelectMany(g =>
                            {
                                var group = g.ToList();
                                if (group.Count == 2 && // Assuming paired DEBIT/CREDIT for transfers
                                    group.Any(t => t.type == "DEBIT") && group.Any(t => t.type == "CREDIT") &&
                                    group[0].amount == group[1].amount && // Balanced transfer
                                    customerAccountKeys.Contains(group[0].accountKey) &&
                                    customerAccountKeys.Contains(group[1].accountKey))
                                {
                                    return new List<Transaction>(); // Exclude internal transfer
                                }
                                return group; // Keep external or unbalanced transactions
                            })
                            .ToList();

                        var transactionsByAccount = externalTransactions
                            .GroupBy(t => t.accountKey)
                            .Select(g => new
                            {
                                AccountKey = g.Key,
                                TotalDebits = g.Where(t => t.type == "DEBIT").Sum(t => t.amount),
                                TotalCredits = g.Where(t => t.type == "CREDIT").Sum(t => t.amount),
                                CurrencyCode = g.First().glAccount.currency.currencyCode
                            })
                            .ToList();

                        return new CustomerSummary
                        {
                            CustomerEncodedKey = c.CustomerEncodedKey,
                            BranchKey = c.BranchKey,
                            MainAccounts = new List<AccountSummary>
                            {
                                new AccountSummary
                                {
                                    AccountKey = c.EurMainAccountKey,
                                    CurrencyCode = "EUR",
                                    TotalDebits = transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.EurMainAccountKey)?.TotalDebits ?? 0,
                                    TotalCredits = transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.EurMainAccountKey)?.TotalCredits ?? 0,
                                    ValueAtEnd = (transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.EurMainAccountKey)?.TotalCredits ?? 0) -
                                                 (transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.EurMainAccountKey)?.TotalDebits ?? 0)
                                },
                                new AccountSummary
                                {
                                    AccountKey = c.UsdMainAccountKey,
                                    CurrencyCode = "USD",
                                    TotalDebits = transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.UsdMainAccountKey)?.TotalDebits ?? 0,
                                    TotalCredits = transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.UsdMainAccountKey)?.TotalCredits ?? 0,
                                    ValueAtEnd = (transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.UsdMainAccountKey)?.TotalCredits ?? 0) -
                                                 (transactionsByAccount.FirstOrDefault(t => t.AccountKey == c.UsdMainAccountKey)?.TotalDebits ?? 0)
                                }
                            }.Where(a => !string.IsNullOrEmpty(a.AccountKey)).ToList(),
                            CardAccounts = transactionsByAccount
                                .Where(t => !allMainAccountKeys.Contains(t.AccountKey) &&
                                            (t.CurrencyCode == "EUR" && c.EurMainAccountKey != null ||
                                             t.CurrencyCode == "USD" && c.UsdMainAccountKey != null))
                                .Select(t => new AccountSummary
                                {
                                    AccountKey = t.AccountKey,
                                    CurrencyCode = t.CurrencyCode,
                                    TotalDebits = t.TotalDebits,
                                    TotalCredits = t.TotalCredits,
                                    ValueAtEnd = t.TotalCredits - t.TotalDebits
                                })
                                .OrderBy(a => a.CurrencyCode)
                                .ToList()
                        };
                    })
                    .OrderBy(c => c.CustomerEncodedKey)
                    .ToList();
            }
        }

        private List<CustomerMapping> CustomerMappings { get; set; } = new List<CustomerMapping>();

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
            CustomerMappings = LoadCustomerMappingsRaw();
        }

        private List<CustomerMapping> LoadCustomerMappingsRaw()
        {
            string filePath = GetCustomerMappingsFilePath();
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<List<CustomerMapping>>(json) ?? new List<CustomerMapping>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading customer mappings: {ex.Message}");
                    return new List<CustomerMapping>();
                }
            }
            return new List<CustomerMapping>();
        }

        private void SaveCustomerMappings(List<CustomerMapping> mappings)
        {
            string filePath = GetCustomerMappingsFilePath();
            string uploadsDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }
            string json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
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

    public class CustomerSummary
    {
        public string CustomerEncodedKey { get; set; }
        public string BranchKey { get; set; }
        public List<AccountSummary> MainAccounts { get; set; } = new List<AccountSummary>();
        public List<AccountSummary> CardAccounts { get; set; } = new List<AccountSummary>();
    }

    public class AccountSummary
    {
        public string AccountKey { get; set; }
        public string CurrencyCode { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal ValueAtEnd { get; set; }
    }

    public class CustomerMapping
    {
        public string CustomerEncodedKey { get; set; }
        public string BranchKey { get; set; }
        public string EurMainAccountKey { get; set; }
        public string UsdMainAccountKey { get; set; }
    }
}