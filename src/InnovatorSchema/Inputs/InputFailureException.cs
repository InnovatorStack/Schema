using System;

namespace InnovatorSchema;

internal sealed class InputFailureException : Exception
{
    public InputFailureException(IssueCode code, string message, IssuePath path)
        : base(message)
    {
        Code = code;
        Path = path;
    }

    public IssueCode Code { get; }

    public IssuePath Path { get; }
}
