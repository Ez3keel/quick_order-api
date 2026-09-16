namespace Delivery.Domain.Assignments.Exceptions;

public sealed class InvalidAssignmentStateTransitionException : Exception
{
    public DeliveryAssignmentStatus From { get; }
    public DeliveryAssignmentStatus To { get; }

    public InvalidAssignmentStateTransitionException(DeliveryAssignmentStatus from, DeliveryAssignmentStatus to)
        : base($"Cannot transition delivery assignment from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }
}
