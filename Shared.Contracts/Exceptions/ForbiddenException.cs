public class ForbiddenException : BusinessException
{
    public ForbiddenException()
        : base("Unauthorized operation") { }

    public ForbiddenException(Guid userId)
        : base($"Unauthorized booking operation for user with ID {userId}") { }
}