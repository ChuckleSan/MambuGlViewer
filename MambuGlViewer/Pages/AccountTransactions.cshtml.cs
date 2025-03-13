using System.Text.Json;

using MambuGLViewer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MambuGlViewer.Pages
{
    public class AccountTransactionsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string AccountKey { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TransactionId { get; set; }

        public List<TransactionDetail> Transactions { get; set; } = new List<TransactionDetail>();

        private readonly IWebHostEnvironment _environment;

        public AccountTransactionsModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(AccountKey))
            {
                LoadTransactions();
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
                    var allTransactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();

                    // Filter transactions based on parameters
                    if (!string.IsNullOrEmpty(TransactionId))
                    {
                        // Show all transactions for the selected transactionId
                        Transactions = allTransactions
                            .Where(t => t.transactionId == TransactionId)
                            .Select(t => new TransactionDetail
                            {
                                EntryID = t.entryID,
                                TransactionID = t.transactionId,
                                GLCode = t.glAccount.glCode,
                                GLName = t.glAccount.name,
                                BookingDate = t.bookingDate,
                                AccountKey = t.accountKey,
                                SignedAmount = t.type == "DEBIT" ? -t.amount : t.amount
                            })
                            .OrderBy(t => t.EntryID)
                            .ToList();
                    }
                    else
                    {
                        // Initial view: transactions for the accountKey, excluding internal transfers for sub-accounts
                        var customerMappings = LoadCustomerMappingsRaw();
                        var customer = customerMappings.FirstOrDefault(c => c.EurMainAccountKey == AccountKey || c.UsdMainAccountKey == AccountKey);
                        var allMainAccountKeys = customerMappings
                            .SelectMany(c => new[] { c.EurMainAccountKey, c.UsdMainAccountKey })
                            .Where(k => !string.IsNullOrEmpty(k))
                            .ToHashSet();

                        var customerAccountKeys = new HashSet<string>();
                        if (customer != null)
                        {
                            customerAccountKeys.UnionWith(new[] { customer.EurMainAccountKey, customer.UsdMainAccountKey }.Where(k => !string.IsNullOrEmpty(k)));
                            var subAccountKeys = allTransactions
                                .Where(t => !allMainAccountKeys.Contains(t.accountKey) &&
                                            (t.glAccount.currency.currencyCode == "EUR" && customer.EurMainAccountKey != null ||
                                             t.glAccount.currency.currencyCode == "USD" && customer.UsdMainAccountKey != null))
                                .Select(t => t.accountKey)
                                .Distinct()
                                .ToHashSet();
                            customerAccountKeys.UnionWith(subAccountKeys);
                        }

                        var externalTransactions = allTransactions;
                        if (customer != null && !allMainAccountKeys.Contains(AccountKey))
                        {
                            externalTransactions = allTransactions
                                .GroupBy(t => t.transactionId)
                                .SelectMany(g =>
                                {
                                    var group = g.ToList();
                                    if (group.Count == 2 &&
                                        group.Any(t => t.type == "DEBIT") && group.Any(t => t.type == "CREDIT") &&
                                        group[0].amount == group[1].amount &&
                                        customerAccountKeys.Contains(group[0].accountKey) &&
                                        customerAccountKeys.Contains(group[1].accountKey))
                                    {
                                        return new List<Transaction>(); // Exclude internal transfer
                                    }
                                    return group;
                                })
                                .ToList();
                        }

                        Transactions = externalTransactions
                            .Where(t => t.accountKey == AccountKey)
                            .Select(t => new TransactionDetail
                            {
                                EntryID = t.entryID,
                                TransactionID = t.transactionId,
                                GLCode = t.glAccount.glCode,
                                GLName = t.glAccount.name,
                                BookingDate = t.bookingDate,
                                AccountKey = t.accountKey,
                                SignedAmount = t.type == "DEBIT" ? -t.amount : t.amount
                            })
                            .OrderBy(t => t.TransactionID)
                            .ThenBy(t => t.EntryID)
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading transactions: {ex.Message}");
                    Transactions = new List<TransactionDetail>();
                }
            }
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

        private string GetTransactionsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "transactions.json");
        }

        private string GetCustomerMappingsFilePath()
        {
            return Path.Combine(_environment.WebRootPath, "uploads", "customer_mappings.json");
        }
    }

    public class TransactionDetail
    {
        public int EntryID { get; set; }
        public string TransactionID { get; set; }
        public string GLCode { get; set; }
        public string GLName { get; set; }
        public DateTime BookingDate { get; set; }
        public string AccountKey { get; set; }
        public decimal SignedAmount { get; set; }
    }
}