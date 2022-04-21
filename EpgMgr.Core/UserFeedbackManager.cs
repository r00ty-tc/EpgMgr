namespace EpgMgr
{
    public class FeedbackInfo : ICloneable
    {
        public string Status { get; set; }
        public int CurrentItem { get; set; }
        public int MaxItems { get; set; }

        public decimal Percent => ((decimal)CurrentItem / (decimal)MaxItems) * 100;

        public FeedbackInfo()
        {
            Status = string.Empty;
            CurrentItem = 0;
            MaxItems = 100;
        }

        public object Clone()
        {
            var newInfo = new FeedbackInfo
            {
                Status = Status,
                CurrentItem = CurrentItem,
                MaxItems = MaxItems
            };
            return newInfo;
        }
    }

    public class FeedbackEventArgs : EventArgs
    {
        public FeedbackInfo Info { get; set; }

        public FeedbackEventArgs(FeedbackInfo info)
        {
            Info = info;
        }
    }

    public class UserFeedbackManager : IDisposable
    {
        // Class to allow subscription to user feedback during operations
        private readonly ReaderWriterLockSlim feedbackLock;
        public event EventHandler<FeedbackEventArgs>? FeedbackChanged;
        public FeedbackInfo Info { get; set; }

        public UserFeedbackManager(EventHandler<FeedbackEventArgs>? feedback = null)
        {
            if (feedback != null)
                FeedbackChanged += feedback;
            feedbackLock = new ReaderWriterLockSlim();
            Info = new FeedbackInfo();
        }

        public void UpdateStatus(string? status = null, int? currentItem = null, int? maxItems = null)
        {
            // If someone triggers this, I feel sad for the world
            if (status == null && currentItem == null && maxItems == null)
                throw new Exception("Status update called with no values passed!");

            FeedbackInfo? newInfo = null;
            feedbackLock.EnterUpgradeableReadLock();
            try
            {
                // Check if any changes from current state
                if (status != null && !status.Equals(Info.Status) ||
                    currentItem.HasValue && !currentItem.Value.Equals(Info.CurrentItem) ||
                    maxItems.HasValue && !maxItems.Value.Equals(Info.MaxItems))
                {
                    feedbackLock.EnterWriteLock();
                    try
                    {
                        // Update changes
                        Info.Status = status ?? Info.Status;
                        Info.CurrentItem = currentItem ?? 0;
                        if (currentItem != null)
                            Info.MaxItems = maxItems ?? Info.MaxItems;
                        else
                            Info.MaxItems = maxItems ?? 0;

                        newInfo = (FeedbackInfo)Info.Clone();
                    }
                    finally
                    {
                        feedbackLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                feedbackLock.ExitUpgradeableReadLock();
            }

            // Update subscribers with a copy of status, if there was a change
            if (newInfo != null)
                FeedbackChanged?.Invoke(this, new FeedbackEventArgs(newInfo));
        }

        public void Dispose()
        {
            feedbackLock.Dispose();
        }
    }
}
