namespace PactNetAbstactMessages;

using System;
using System.Collections.Generic;

public interface IMatcherWrapper : IDictionary<string, object>
{
    Type ContentType { get; }
}

public sealed class MatcherWrapper<TContent> : Dictionary<string, object>, IMatcherWrapper
{
    public Type ContentType => typeof(TContent);
}
