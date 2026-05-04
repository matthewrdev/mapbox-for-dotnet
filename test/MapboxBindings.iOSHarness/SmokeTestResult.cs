namespace MapboxBindings.iOSHarness;

public enum SmokeTestStatus
{
    Passed,
    Warning,
    Failed
}

public sealed record SmokeTestResult(
    string Name,
    SmokeTestStatus Status,
    string Details);
