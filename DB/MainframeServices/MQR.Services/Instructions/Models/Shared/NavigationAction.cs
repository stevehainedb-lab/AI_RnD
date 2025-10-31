namespace MQR.Services.Instructions.Models.Shared;

/// <summary>
/// Navigation action that can be performed during screen navigation.
/// </summary>
public class NavigationAction
{
    /// <summary>Navigation key to press (e.g., "Enter", "PF3").</summary>
    public KeyCommand NavigationKey { get; init; }

    /// <summary>Timeout duration for the navigation operation.</summary>
    public TimeSpan? NavigationTimeout { get; init; }

    /// <summary>Duration to wait after navigation before proceeding.</summary>
    public TimeSpan? NavigationWait { get; init; }

    /// <summary>Number of screen refreshes to perform.</summary>
    public int? ScreenRefreshes { get; init; }

    public override string ToString() => NavigationKey.ToString();
    
}