using ST.Infrastructure.Logging.Specifications;
using ST.Infrastructure.Messaging;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging;

public class ConsumerLoggingObserver : IConsumerConfigurationObserver
{
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator) where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator, nameof(configurator));

        // Use ConsumerMessage instead of Message, and configure the filter for all message types
        configurator.AddPipeSpecification(new LoggingPipeSpecification<TConsumer>());
    }

    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator, nameof(configurator));
        // Add consume filter for each specific message type
        configurator.AddPipeSpecification(new LoggingConsumeFilterSpecification<TConsumer, TMessage>());
    }
}
