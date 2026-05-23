namespace CanvasFlow.Api.Models.Enums
{
    /// <summary>
    /// Defines the possible status states for a user account.
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// Account is awaiting manual review/approval.
        /// </summary>
        Pending,
        
        /// <summary>
        /// Account is fully active and usable.
        /// </summary>
        Active,
        
        /// <summary>
        /// Account has been restricted by an administrator.
        /// </summary>
        Blocked
    }
}