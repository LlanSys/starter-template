namespace ST.Application.Features.ControlPlane.Demo;
public interface IDemoFixture
{
 int Version { get; }
 Task ResetAsync(CancellationToken cancellationToken);
}
