namespace Domain.Exceptions
{
    public class UnauthorizedException : BusinessException
    {
        public UnauthorizedException()
            : base("Unauthorized operation") { }

        public UnauthorizedException(Guid userId)
            : base($"Unauthorized booking operation for user with ID {userId}") { }
    }
}