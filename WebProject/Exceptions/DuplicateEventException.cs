using System.ComponentModel;

public class DuplicateEventException : BusinessException
{
    public DuplicateEventException(string title)
        : base($"Event with nanme '{title}' is existing"){}
}