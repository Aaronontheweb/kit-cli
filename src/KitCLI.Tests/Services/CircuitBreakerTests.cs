using FluentAssertions;
using KitCLI.Services;

namespace KitCLI.Tests.Services;

public class CircuitBreakerTests
{
    [Fact]
    public async Task Should_Allow_Successful_Operations()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 3);
        var successCount = 0;

        // Act
        for (int i = 0; i < 5; i++)
        {
            await circuitBreaker.ExecuteAsync("test", async () =>
            {
                successCount++;
                await Task.Delay(1);
                return successCount;
            });
        }

        // Assert
        successCount.Should().Be(5);
        circuitBreaker.GetStatus("test").Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public async Task Should_Open_Circuit_After_Threshold_Failures()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 3, TimeSpan.FromMilliseconds(100));
        var failureCount = 0;

        // Act
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<int>("test", async () =>
                {
                    failureCount++;
                    await Task.Delay(1);
                    throw new Exception("Test failure");
                });
            }
            catch
            {
                // Expected
            }
        }

        // Assert
        failureCount.Should().Be(3);
        circuitBreaker.GetStatus("test").Should().Be(CircuitStatus.Open);

        // Attempting another call should fail immediately
        var action = async () => await circuitBreaker.ExecuteAsync("test", async () =>
        {
            await Task.Delay(1);
            return 1;
        });

        await action.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task Should_Reset_After_Timeout()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 2, TimeSpan.FromMilliseconds(50));

        // Act - cause failures to open circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<int>("test", async () =>
                {
                    await Task.Delay(1);
                    throw new Exception("Test failure");
                });
            }
            catch
            {
                // Expected
            }
        }

        // Circuit should be open
        circuitBreaker.GetStatus("test").Should().Be(CircuitStatus.Open);

        // Wait for reset timeout
        await Task.Delay(100);

        // Circuit should allow retry (half-open)
        var result = await circuitBreaker.ExecuteAsync("test", async () =>
        {
            await Task.Delay(1);
            return "success";
        });

        // Assert
        result.Should().Be("success");
        circuitBreaker.GetStatus("test").Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public async Task Should_Track_Multiple_Circuits_Independently()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 2);

        // Act - fail circuit1
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<int>("circuit1", async () =>
                {
                    await Task.Delay(1);
                    throw new Exception("Test");
                });
            }
            catch
            {
                // Expected
            }
        }

        // Assert
        circuitBreaker.GetStatus("circuit1").Should().Be(CircuitStatus.Open);
        circuitBreaker.GetStatus("circuit2").Should().Be(CircuitStatus.Closed);
    }

    [Fact]
    public async Task Should_Reset_Circuit_Manually()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 2);

        // Act - open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<int>("test", async () =>
                {
                    await Task.Delay(1);
                    throw new Exception("Test");
                });
            }
            catch
            {
                // Expected
            }
        }

        circuitBreaker.GetStatus("test").Should().Be(CircuitStatus.Open);

        // Reset manually
        circuitBreaker.Reset("test");

        // Should work again
        var result = await circuitBreaker.ExecuteAsync("test", async () =>
        {
            await Task.Delay(1);
            return "success";
        });

        // Assert
        result.Should().Be("success");
        circuitBreaker.GetStatus("test").Should().Be(CircuitStatus.Closed);
    }
}

