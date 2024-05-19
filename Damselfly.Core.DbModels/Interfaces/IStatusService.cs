using System;

namespace Damselfly.Core.ScopedServices.Interfaces;

/// <summary>
///     A generic Status service that works systemwide, client
///     and server
/// </summary>
public interface IStatusService
{
    void UpdateStatus(string newStatus, int? userId = null);
    event Action<string> OnStatusChanged;
}

public interface IUserStatusService
{
    void UpdateStatus(string newStatus);
    event Action<string> OnStatusChanged;
}
