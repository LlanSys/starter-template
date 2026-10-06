using MassTransit;
using MassTransit.Configuration;

namespace ST.Infrastructure.Logging.Specifications;

internal sealed class LoggingPipeSpecification<TConsumer> : IPipeSpecification<ConsumerConsumeContext<TConsumer>> where TConsumer : class
{
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer>> builder)
    {

    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}