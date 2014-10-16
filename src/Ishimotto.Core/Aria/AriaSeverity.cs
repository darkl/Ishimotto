namespace Ishimotto.Core.Aria
{
    /// <summary>
    /// The options to use as Aria log levels
    /// </summary>
    public enum AriaSeverity
    {
        None, // Not an aria valid value, represents a value when the AriaLogPath is String.Empty
        Debug,
        Info,
        Notice,
        Warn,
        Error,
    }
}