namespace Domain.Exceptions
{
    public class DuplicateLoginException : BusinessException
    {
        public DuplicateLoginException()
            : base("The user is already registered") { }

        public DuplicateLoginException(String login)
            : base($"The user with login {login} is already registered") { }
    }
}
