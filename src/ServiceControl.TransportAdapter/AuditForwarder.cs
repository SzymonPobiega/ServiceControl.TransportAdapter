﻿namespace ServiceControl.TransportAdapter
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.Raw;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    class AuditForwarder<TEndpoint, TServiceControl>
        where TEndpoint : TransportDefinition, new()
        where TServiceControl : TransportDefinition, new()
    {
        public AuditForwarder(string adapterName, string frontendAuditQueue, string backendAuditQueue, string poisonMessageQueueName,
            Action<TransportExtensions<TEndpoint>> frontendTransportCustomization, Action<TransportExtensions<TServiceControl>> backendTransportCustomization)
        {
            frontEndConfig = RawEndpointConfiguration.Create(frontendAuditQueue, (context, _) => OnAuditMessage(context, backendAuditQueue), poisonMessageQueueName);
            frontEndConfig.CustomErrorHandlingPolicy(new RetryForeverPolicy());
            var transport = frontEndConfig.UseTransport<TEndpoint>();
            frontEndConfig.AutoCreateQueue();
            // customizations override defaults
            frontendTransportCustomization(transport);

            backEndConfig = RawEndpointConfiguration.CreateSendOnly($"{adapterName}.AuditForwarder");
            var backEndTransport = backEndConfig.UseTransport<TServiceControl>();
            backendTransportCustomization(backEndTransport);
        }

        Task OnAuditMessage(MessageContext context, string backendAuditQueue)
        {
            logger.Debug("Forwarding an audit message.");
            return Forward(context, backEnd, backendAuditQueue);
        }

        static Task Forward(MessageContext context, IDispatchMessages forwarder, string destination)
        {

            if (context.Headers.TryGetValue(Headers.ReplyToAddress, out var replyTo))
            {
                context.Headers[Headers.ReplyToAddress] = AddressSanitizer.MakeV5CompatibleAddress(replyTo);
                context.Headers[TransportAdapterHeaders.ReplyToAddress] = replyTo;
            }

            var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));
            return forwarder.Dispatch(new TransportOperations(operation), context.TransportTransaction, context.Extensions);
        }

        public async Task Start()
        {
            backEnd = await RawEndpoint.Start(backEndConfig).ConfigureAwait(false);
            frontEnd = await RawEndpoint.Start(frontEndConfig).ConfigureAwait(false);
        }

        public async Task Stop()
        {
            //null-checks for shutting down if start-up failed
            if (frontEnd != null)
            {
                await frontEnd.Stop().ConfigureAwait(false);
            }
            if (backEnd != null)
            {
                await backEnd.Stop().ConfigureAwait(false);
            }
        }

        RawEndpointConfiguration backEndConfig;
        RawEndpointConfiguration frontEndConfig;

        IReceivingRawEndpoint backEnd;
        IReceivingRawEndpoint frontEnd;
        static ILog logger = LogManager.GetLogger(typeof(AuditForwarder<,>));

        class RetryForeverPolicy : IErrorHandlingPolicy
        {
            public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IDispatchMessages dispatcher)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
        }
    }
}