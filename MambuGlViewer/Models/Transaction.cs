namespace MambuGLViewer.Models
{
    public class Transaction
    {
        public string accountKey { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string assignedBranchKey { get; set; } = string.Empty;
        public DateTime bookingDate { get; set; }
        public DateTime creationDate { get; set; }
        public string encodedKey { get; set; } = string.Empty;
        public int entryID { get; set; }
        public GLAccount glAccount { get; set; } = new GLAccount();
        public string productKey { get; set; } = string.Empty;
        public string productType { get; set; } = string.Empty;
        public string transactionId { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string userKey { get; set; } = string.Empty;
    }
}