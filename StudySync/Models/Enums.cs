namespace StudySync.Models
{
    public enum SessionStatus
    {
        Open,
        Completed,
        Cancelled
    }

    public enum TransactionType
    {
        SessionJoin,
        SessionEarned,
        SessionRefund,
        AdminAdjustment
    }
}
