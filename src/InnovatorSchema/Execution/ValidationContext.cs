using System;
using System.Collections.Generic;

namespace InnovatorSchema;

internal sealed class ValidationContext
{
    private readonly List<PathSegment> path = new();
    private readonly List<ValidationIssue> issues = new();
    private int operations;
    private bool limited;
    private IReadOnlyDictionary<IssueCode, string>? messages;

    public ValidationContext(ValidationOptions options)
    {
        options.CancellationToken.ThrowIfCancellationRequested();
        Options = options;
    }

    public ValidationOptions Options { get; }

    public int IssueCount => issues.Count;

    public bool IsStopped => limited ||
        (Options.Mode == ValidationMode.FirstIssue && issues.Count > 0);

    public bool Step()
    {
        if (IsStopped)
        {
            return false;
        }

        Options.CancellationToken.ThrowIfCancellationRequested();
        operations++;

        if (operations > Options.MaximumOperations)
        {
            Limit("Validation operation limit exceeded.");
            return false;
        }

        return true;
    }

    public PathScope Enter(PathSegment segment)
    {
        if (IsStopped)
        {
            return default;
        }

        int length = segment.Name.Length;

        foreach (PathSegment existing in path)
        {
            length += existing.Name.Length + 16;
        }

        if (segment.Name.Length > 1024 || path.Count >= Options.MaximumDepth || length > 4096)
        {
            Limit("Validation path limit exceeded.");
            return default;
        }

        path.Add(segment);
        return new PathScope(this);
    }

    public void Add(IssueCode code, string message, string? ruleId = null)
    {
        if (!IsStopped)
        {
            if (messages is not null && messages.TryGetValue(code, out string? replacement))
            {
                message = replacement;
            }

            Record(code, message, new IssuePath(path), ruleId);
        }
    }

    public MessageScope EnterMessages(IReadOnlyDictionary<IssueCode, string>? overrides)
    {
        var scope = new MessageScope(this, messages);
        messages = overrides;
        return scope;
    }

    public void AddInput(ValidationIssue issue)
    {
        if (!IsStopped)
        {
            Record(issue.Code, issue.Message, issue.Path, issue.RuleId,
                issue.LineNumber, issue.ColumnNumber);
        }
    }

    public void Limit(string message)
    {
        if (limited)
        {
            return;
        }

        limited = true;
        Record(IssueCode.LimitExceeded, message, new IssuePath(path));
    }

    public IReadOnlyList<ValidationIssue> Freeze()
    {
        return issues.Count == 0 ? Array.Empty<ValidationIssue>() :
            Array.AsReadOnly(issues.ToArray());
    }

    private void Record(
        IssueCode code, string message, IssuePath location, string? ruleId = null,
        int? lineNumber = null, int? columnNumber = null)
    {
        if (message.Length > 1024)
        {
            message = string.Concat(message.AsSpan(0, 1012), " [truncated]");
        }

        if (issues.Count >= Options.MaximumIssues)
        {
            limited = true;
            issues[issues.Count - 1] = new ValidationIssue(
                IssueCode.LimitExceeded,
                "Issue limit exceeded; remaining issues omitted.", location);
            return;
        }

        issues.Add(new ValidationIssue(code, message, location, ruleId, lineNumber, columnNumber));
    }

    internal readonly struct PathScope : IDisposable
    {
        private readonly ValidationContext? context;

        public PathScope(ValidationContext context)
        {
            this.context = context;
        }

        public bool IsEntered => context is not null;

        public void Dispose()
        {
            if (context is not null)
            {
                context.path.RemoveAt(context.path.Count - 1);
            }
        }
    }

    internal readonly struct MessageScope : IDisposable
    {
        private readonly ValidationContext context;
        private readonly IReadOnlyDictionary<IssueCode, string>? previous;

        public MessageScope(ValidationContext context,
            IReadOnlyDictionary<IssueCode, string>? previous)
        {
            this.context = context;
            this.previous = previous;
        }

        public void Dispose()
        {
            context.messages = previous;
        }
    }
}
