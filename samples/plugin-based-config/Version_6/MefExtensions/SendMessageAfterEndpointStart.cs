﻿using System.ComponentModel.Composition;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

#region MefSendMessageAfterEndpointStart

[Export(typeof(IRunAfterEndpointStart))]
public class SendMessageAfterEndpointStart : IRunAfterEndpointStart
{
    static ILog log = LogManager.GetLogger<SendMessageAfterEndpointStart>();
    public async Task Run(IEndpointInstance endpoint)
    {
        log.Info("Sending Message.");
        await endpoint.SendLocal(new MyMessage());
    }
}

#endregion