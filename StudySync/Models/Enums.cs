namespace StudySync.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }

    public enum BountyStatus
    {
        Open,
        InProgress,
        Completed,
        Cancelled
    }

    public enum SessionStatus
    {
        Upcoming,
        Live,
        Completed,
        Cancelled
    }

    public enum TransactionType
    {
        SwapDebit,
        SwapCredit,
        FocusReward,
        AdminAdjustment,
        SessionDebit,
        SessionCredit,
        SessionRefund
    }
}
