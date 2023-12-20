namespace PactNetAbstactMessages;

public abstract record KeyValuePair(string Key);
public record IntegerKeyValuePair(string Key, int IntegerValue) : KeyValuePair(Key);
public record DoubleKeyValuePair(string Key, double DoubleValue) : KeyValuePair(Key);

public record Message(KeyValuePair[] KeyValuePairs);
