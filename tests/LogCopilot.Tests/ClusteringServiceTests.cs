using LogCopilot.Infrastructure.Clustering;
using Xunit;

namespace LogCopilot.Tests;

public class ClusteringServiceTests
{
    [Fact]
    public void ComputeExceptionSignature_ProducesSameHashForSameException()
    {
        var type = "System.NullReferenceException";
        var stackTrace = "   at MyApp.Service.DoWork()\n   at MyApp.Controller.Action()";

        var hash1 = ClusteringService.ComputeExceptionSignature(type, stackTrace);
        var hash2 = ClusteringService.ComputeExceptionSignature(type, stackTrace);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeExceptionSignature_ProducesDifferentHashForDifferentException()
    {
        var type1 = "System.NullReferenceException";
        var type2 = "System.ArgumentException";
        var stackTrace = "   at MyApp.Service.DoWork()";

        var hash1 = ClusteringService.ComputeExceptionSignature(type1, stackTrace);
        var hash2 = ClusteringService.ComputeExceptionSignature(type2, stackTrace);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ProducesConsistentHash()
    {
        var input = "test-input-string";
        
        var hash1 = ClusteringService.ComputeHash(input);
        var hash2 = ClusteringService.ComputeHash(input);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }
}
