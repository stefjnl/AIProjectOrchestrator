using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using AIProjectOrchestrator.Web.Services;
using AIProjectOrchestrator.Domain.Entities;
using System.Linq;

namespace AIProjectOrchestrator.Web.Tests.Services;

public class APIClientTests
{
    [Fact]
    public async Task GetProjectsAsync_ReturnsProjects_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedProjects = new List<Project>
        {
            new() { Id = 1, Name = "Test Project", Description = "Test Description" }
        };

        var json = JsonSerializer.Serialize(expectedProjects);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        var loggerMock = new Mock<ILogger<APIClient>>();
        var apiClient = new APIClient(httpClient, loggerMock.Object);

        // Act
        var projects = await apiClient.GetProjectsAsync();

        // Assert
        Assert.Single(projects);
        Assert.Equal("Test Project", projects.First().Name);
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsEmptyList_WhenApiCallFails()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API call failed"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        var loggerMock = new Mock<ILogger<APIClient>>();
        var apiClient = new APIClient(httpClient, loggerMock.Object);

        // Act
        var projects = await apiClient.GetProjectsAsync();

        // Assert
        Assert.Empty(projects);
    }
}