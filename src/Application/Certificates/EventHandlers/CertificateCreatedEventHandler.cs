using Hippo.Application.Common.Models;
using Hippo.Core.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hippo.Application.Certificates.EventHandlers;

public class CertificateCreatedEventHandler : INotificationHandler<DomainEventNotification<CertificateCreatedEvent>>
{
    private readonly ILogger<CertificateCreatedEventHandler> _logger;

    public CertificateCreatedEventHandler(ILogger<CertificateCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<CertificateCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Hippo Domain Event: {DomainEvent}", domainEvent.GetType().Name);

        return Task.CompletedTask;
    }
}
