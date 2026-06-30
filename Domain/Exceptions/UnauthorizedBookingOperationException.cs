namespace Domain.Exceptions
{
    public class UnauthorizedBookingOperationException : BusinessException
    {
        public UnauthorizedBookingOperationException()
            : base("Unauthorized operation") { }

        public UnauthorizedBookingOperationException(Guid userId)
            : base($"Unauthorized booking operation for user with ID {userId}") { }
    }
}