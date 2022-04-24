namespace EpgMgr
{
    /// <summary>
    /// Feedback info class. Helps communicate status to console. Might be upgraded to support UI later.
    /// </summary>
    public class FeedbackInfo : ICloneable
    {
        /// <summary>
        /// Current Status message
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Current item number, should be less than or equal to max items
        /// </summary>
        public int CurrentItem { get; set; }
        /// <summary>
        /// Max item numbers (used to generate percentage)
        /// </summary>
        public int MaxItems { get; set; }

        /// <summary>
        /// Calculated percentage based on current item and max items
        /// </summary>
        public decimal Percent => ((decimal)CurrentItem / (decimal)MaxItems) * 100;

        /// <summary>
        /// Create new feedback info instance
        /// </summary>
        public FeedbackInfo()
        {
            Status = string.Empty;
            CurrentItem = 0;
            MaxItems = 100;
        }

        /// <summary>
        /// Clone the feedback info instance. Used to provide some protection for multi thread
        /// </summary>
        /// <returns></returns>
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

    /// <summary>
    /// Event arguments for feedback event handlers
    /// </summary>
    public class FeedbackEventArgs : EventArgs
    {
        /// <summary>
        /// Reference to a copy of the info object
        /// </summary>
        public FeedbackInfo Info { get; set; }

        /// <summary>
        /// Create new feedback arguments instance
        /// </summary>
        /// <param name="info"></param>
        public FeedbackEventArgs(FeedbackInfo info)
        {
            Info = info;
        }
    }

    /// <summary>
    /// The user feedback manager class
    /// </summary>
    public class UserFeedbackManager : IDisposable
    {
        // Class to allow subscription to user feedback during operations
        private readonly ReaderWriterLockSlim feedbackLock;
        /// <summary>
        /// Event handler to handle new feedback. Triggered when feedback changes
        /// </summary>
        public event EventHandler<FeedbackEventArgs>? FeedbackChanged;

        /// <summary>
        /// The feedback information
        /// </summary>
        public FeedbackInfo Info { get; set; }

        /// <summary>
        /// Create new feedback instance, optional event handler argument
        /// </summary>
        /// <param name="feedback"></param>
        public UserFeedbackManager(EventHandler<FeedbackEventArgs>? feedback = null)
        {
            if (feedback != null)
                FeedbackChanged += feedback;
            feedbackLock = new ReaderWriterLockSlim();
            Info = new FeedbackInfo();
        }

        /// <summary>
        /// Update feedback and distribute to subscribers
        /// </summary>
        /// <param name="status"></param>
        /// <param name="currentItem"></param>
        /// <param name="maxItems"></param>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            feedbackLock.Dispose();
        }
    }
}
