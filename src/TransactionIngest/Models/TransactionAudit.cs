namespace TransactionIngest.Models;

public class TransactionAudit
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public Transaction? Transaction { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public string ChangedFields { get; set; } = string.Empty;

    public string OldValues { get; set; } = string.Empty;

    public string NewValues { get; set; } = string.Empty;
}