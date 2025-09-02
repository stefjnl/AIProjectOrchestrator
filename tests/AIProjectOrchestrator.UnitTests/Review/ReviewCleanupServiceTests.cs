using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Configuration;

namespace AIProjectOrchestrator.UnitTests.Review
{
    public class ReviewCleanupServiceTests
    {
        [Fact]
        public async Task ReviewCleanupService_StartsAndStopsWithoutError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ReviewCleanupService>>();
            var settings = new ReviewSettings
            {
                CleanupIntervalMinutes = 1
            };
            var mockSettings = new Mock<IOptions<ReviewSettings>>();
            mockSettings.Setup(s => s.Value).Returns(settings);
            
            var services = new ServiceCollection();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(new Mock<IServiceScopeFactory>().Object);
            
            var service = new ReviewCleanupService(
                mockLogger.Object,
                mockSettings.Object,
                mockServiceProvider.Object);

            // Act & Assert
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            
            // This should not throw an exception
            var task = service.StartAsync(cts.Token);
            
            // Give it a moment to start
            await Task.Delay(50);
            
            // Cancel and stop
            await service.StopAsync(CancellationToken.None);
        }
    }
}