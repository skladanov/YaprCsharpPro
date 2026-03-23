public class ValidationException : BusinessException
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("Failed data validation")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string errorMessage)
        : this(new Dictionary<string, string[]>
        {
            [field] = new[] { errorMessage }
        })
    { }
}