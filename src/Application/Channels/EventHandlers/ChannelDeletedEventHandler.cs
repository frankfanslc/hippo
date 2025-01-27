using Hippo.Application.Common.Exceptions;
using Hippo.Application.Common.Models;
using Hippo.Application.Jobs;
using Hippo.Core.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hippo.Application.Channels.EventHandlers;

public class ChannelDeletedEventHandler : INotificationHandler<DomainEventNotification<ChannelDeletedEvent>>
{
    private readonly ILogger<ChannelDeletedEventHandler> _logger;

    private readonly JobScheduler _jobScheduler = JobScheduler.Current;

    public ChannelDeletedEventHandler(ILogger<ChannelDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<ChannelDeletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Hippo Domain Event: {DomainEvent}", domainEvent.GetType().Name);

        try
        {
            foreach (Job job in _jobScheduler.GetRunningJobs())
            {
                if (job.Id == domainEvent.Channel.Id)
                {
                    job.Stop();
                }
            }
        }
        catch (JobFailedException e)
        {
            _logger.LogError(e.Message);
            throw;
        }

        return Task.CompletedTask;
    }
}
