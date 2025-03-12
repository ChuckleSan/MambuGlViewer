namespace MambuGLViewer.Models
{
    public class GLAccount
    {
        public bool activated { get; set; }
        public bool allowManualJournalEntries { get; set; }
        public DateTime creationDate { get; set; }
        public Currency currency { get; set; } = new Currency();
        public string description { get; set; } = string.Empty;
        public string encodedKey { get; set; } = string.Empty;
        public string glCode { get; set; } = string.Empty;
        public DateTime lastModifiedDate { get; set; }
        public string name { get; set; } = string.Empty;
        public bool stripTrailingZeros { get; set; }
        public string type { get; set; } = string.Empty;
        public string usage { get; set; } = string.Empty;
    }
}