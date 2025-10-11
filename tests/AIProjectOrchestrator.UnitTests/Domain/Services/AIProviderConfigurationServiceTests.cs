using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Exceptions;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Services
{
    public class AIProviderConfigurationServiceTests
    {
        [Fact]
        public void GetProviderSettings_ValidProviderName_ReturnsExpectedConfiguration()
        {
            // Arrange
            var credentials = new AIProviderCredentials
            {
                Claude = new ClaudeCredentials { ApiKey = "test-key" },
                LMStudio = new LMStudioCredentials { BaseUrl = "http://test-url" },
                OpenRouter = new OpenRouterCredentials { ApiKey = "test-key" },
                NanoGpt = new NanoGptCredentials { ApiKey = "test-key" },
                AlibabaCloud = new AlibabaCloudCredentials { ApiKey = "test-key" }
            };
            
            var options = Options.Create(credentials);
            var service = new AIProviderConfigurationService(options);

            // Act
            var claudeSettings = service.GetProviderSettings<ClaudeCredentials>(ProviderNames.Claude);
            var lmstudioSettings = service.GetProviderSettings<LMStudioCredentials>(ProviderNames.LMStudio);
            var openRouterSettings = service.GetProviderSettings<OpenRouterCredentials>(ProviderNames.OpenRouter);
            var nanoGptSettings = service.GetProviderSettings<NanoGptCredentials>(ProviderNames.NanoGpt);
            var alibabaCloudSettings = service.GetProviderSettings<AlibabaCloudCredentials>(ProviderNames.AlibabaCloud);

            // Assert
            claudeSettings.Should().NotBeNull();
            claudeSettings.ApiKey.Should().Be("test-key");
            
            lmstudioSettings.Should().NotBeNull();
            lmstudioSettings.BaseUrl.Should().Be("http://test-url");
            
            openRouterSettings.Should().NotBeNull();
            openRouterSettings.ApiKey.Should().Be("test-key");
            
            nanoGptSettings.Should().NotBeNull();
            nanoGptSettings.ApiKey.Should().Be("test-key");
            
            alibabaCloudSettings.Should().NotBeNull();
            alibabaCloudSettings.ApiKey.Should().Be("test-key");
        }

        [Fact]
        public void GetProviderSettings_UnknownProviderName_ThrowsArgumentException()
        {
            // Arrange
            var credentials = new AIProviderCredentials();
            var options = Options.Create(credentials);
            var service = new AIProviderConfigurationService(options);

            // Act & Assert
            var action = () => service.GetProviderSettings<ClaudeCredentials>("UnknownProvider");
            action.Should().Throw<ArgumentException>()
                .WithMessage("Invalid provider name: UnknownProvider");
        }

        [Fact]
        public void GetProviderSettings_InvalidCast_ThrowsInvalidCastException()
        {
            // Arrange
            var credentials = new AIProviderCredentials
            {
                Claude = new ClaudeCredentials { ApiKey = "test-key" }
            };
            var options = Options.Create(credentials);
            var service = new AIProviderConfigurationService(options);

            // Act & Assert
            var action = () => service.GetProviderSettings<LMStudioCredentials>(ProviderNames.Claude);
            action.Should().Throw<InvalidCastException>()
                .WithMessage("Cannot convert ClaudeSettings to LMStudioCredentials");
        }

        [Fact]
        public void GetProviderSettingsNonGeneric_ValidProviderName_ReturnsExpectedConfiguration()
        {
            // Arrange
            var credentials = new AIProviderCredentials
            {
                Claude = new ClaudeCredentials { ApiKey = "test-key" },
                LMStudio = new LMStudioCredentials { BaseUrl = "http://test-url" }
            };
            var options = Options.Create(credentials);
            var service = new AIProviderConfigurationService(options);

            // Act
            var claudeSettings = service.GetProviderSettings(ProviderNames.Claude);
            var lmstudioSettings = service.GetProviderSettings(ProviderNames.LMStudio);

            // Assert
            claudeSettings.Should().NotBeNull().And.BeOfType<ClaudeCredentials>();
            ((ClaudeCredentials)claudeSettings).ApiKey.Should().Be("test-key");
            
            lmstudioSettings.Should().NotBeNull().And.BeOfType<LMStudioCredentials>();
            ((LMStudioCredentials)lmstudioSettings).BaseUrl.Should().Be("http://test-url");
        }

        [Fact]
        public void GetProviderSettingsNonGeneric_UnknownProviderName_ThrowsArgumentException()
        {
            // Arrange
            var credentials = new AIProviderCredentials();
            var options = Options.Create(credentials);
            var service = new AIProviderConfigurationService(options);

            // Act & Assert
            var action = () => service.GetProviderSettings("UnknownProvider");
            action.Should().Throw<ArgumentException>()
                .WithMessage("Invalid provider name: UnknownProvider");
        }

        [Fact]
        public void GetProviderNames_ReturnsAllExpectedProviderNames()
        {
            // Arrange
            var credentials = new AIProviderCredentials();
            var options = Options.Create(credentials);
            var service = new AIProviderConfigurationService(options);

            // Act
            var providerNames = service.GetProviderNames();

            // Assert
            providerNames.Should().Contain(ProviderNames.Claude);
            providerNames.Should().Contain(ProviderNames.LMStudio);
            providerNames.Should().Contain(ProviderNames.OpenRouter);
            providerNames.Should().Contain(ProviderNames.NanoGpt);
            providerNames.Should().Contain(ProviderNames.AlibabaCloud);
            providerNames.Should().HaveCount(5);
        }
    }
}