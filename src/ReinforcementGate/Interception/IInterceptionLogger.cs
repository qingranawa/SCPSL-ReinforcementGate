using System;

namespace ReinforcementGate.Interception;

/// <summary>Records interception warnings and failures without controlling wave decisions.</summary>
public interface IInterceptionLogger
{
    /// <summary>Records a non-fatal interception warning.</summary>
    void Warn(string message);

    /// <summary>Records a non-fatal interception failure.</summary>
    void Error(string message, Exception exception);
}
