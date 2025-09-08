using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.API.Controllers;

namespace AIProjectOrchestrator.UnitTests.PromptTemplateTests
{
    public class PromptTemplateControllerTests
    {
        private readonly Mock<IPromptTemplateRepository> _mockRepository;
        private readonly Mock<ILogger<PromptTemplatesController>> _mockLogger;
        private readonly PromptTemplateService _service;
        private readonly PromptTemplatesController _controller;

        public PromptTemplateControllerTests()
        {
            _mockRepository = new Mock<IPromptTemplateRepository>();
            _service = new PromptTemplateService(_mockRepository.Object);
            _mockLogger = new Mock<ILogger<PromptTemplatesController>>();
            _controller = new PromptTemplatesController(_service, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateTemplateAsync_ShouldCreateNewTemplateWithCorrectProperties()
        {
            // Arrange
            var template = new Domain.Entities.PromptTemplate
            {
                Id = Guid.Empty,
                Title = "Test Title",
                Content = "Test Content"
            };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.PromptTemplate>(), It.IsAny<CancellationToken>()))
                .Returns((Domain.Entities.PromptTemplate t, CancellationToken ct) =>
                {
                    t.Id = Guid.NewGuid();
                    t.CreatedAt = DateTime.UtcNow;
                    t.UpdatedAt = DateTime.UtcNow;
                    return Task.FromResult(t);
                });

            // Act
            var result = await _service.CreateTemplateAsync(template);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Test Title", result.Title);
            Assert.Equal("Test Content", result.Content);
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
            Assert.NotNull(result.UpdatedAt);
            _mockRepository.Verify(r => r.AddAsync(It.Is<Domain.Entities.PromptTemplate>(t => t.Title == "Test Title" && t.Content == "Test Content"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateTemplateAsync_ShouldUpdateExistingTemplateWithNewProperties()
        {
            // Arrange
            var template = new Domain.Entities.PromptTemplate
            {
                Id = Guid.NewGuid(),
                Title = "Old Title",
                Content = "Old Content",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.PromptTemplate>(), It.IsAny<CancellationToken>()))
                .Returns((Domain.Entities.PromptTemplate t, CancellationToken ct) =>
                {
                    t.UpdatedAt = DateTime.UtcNow;
                    return Task.FromResult(t);
                });

            var updatedTitle = "New Title";
            var updatedContent = "New Content";
            template.Title = updatedTitle;
            template.Content = updatedContent;

            // Act
            var result = await _service.UpdateTemplateAsync(template);

            // Assert
            Assert.Equal(updatedTitle, result.Title);
            Assert.Equal(updatedContent, result.Content);
            Assert.NotNull(result.UpdatedAt);
            Assert.True(result.UpdatedAt > result.CreatedAt);
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<Domain.Entities.PromptTemplate>(t => t.Title == updatedTitle && t.Content == updatedContent), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllTemplatesAsync_ShouldReturnAllTemplates()
        {
            // Arrange
            var expectedTemplates = new List<Domain.Entities.PromptTemplate>
            {
                new Domain.Entities.PromptTemplate { Id = Guid.NewGuid(), Title = "Template 1", Content = "Content 1" },
                new Domain.Entities.PromptTemplate { Id = Guid.NewGuid(), Title = "Template 2", Content = "Content 2" }
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedTemplates);

            // Act
            var result = await _service.GetAllTemplatesAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, t => t.Title == "Template 1");
            Assert.Contains(result, t => t.Title == "Template 2");
            _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetTemplateByIdAsync_ShouldReturnTemplate_WhenExists()
        {
            // Arrange
            var expectedTemplate = new Domain.Entities.PromptTemplate { Id = Guid.NewGuid(), Title = "Test Template", Content = "Test Content" };
            var id = expectedTemplate.Id;

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(expectedTemplate);

            // Act
            var result = await _service.GetTemplateByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("Test Template", result.Title);
            _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetTemplateByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Domain.Entities.PromptTemplate)null!);

            // Act
            var result = await _service.GetTemplateByIdAsync(id);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteTemplateAsync_ShouldDeleteTemplate_WhenExists()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockRepository.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteTemplateAsync(id);

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateController_CreateNewTemplate_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);
            var template = new Domain.Entities.PromptTemplate
            {
                Id = Guid.Empty,
                Title = "Test Title",
                Content = "Test Content"
            };
            var createdTemplate = new Domain.Entities.PromptTemplate
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Content = "Test Content",
                CreatedAt = DateTime.UtcNow
            };

            mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<Domain.Entities.PromptTemplate>())).ReturnsAsync(createdTemplate);

            // Act
            var result = await controller.CreateOrUpdate(template);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdAtResult);
            Assert.Equal(201, createdAtResult.StatusCode);
            var value = Assert.IsType<Domain.Entities.PromptTemplate>(createdAtResult.Value);
            Assert.Equal(createdTemplate.Id, value.Id);
            mockService.Verify(s => s.CreateTemplateAsync(It.Is<Domain.Entities.PromptTemplate>(t => t.Title == "Test Title")), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateController_UpdateExistingTemplate_ShouldReturnOk()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);
            var template = new Domain.Entities.PromptTemplate
            {
                Id = Guid.NewGuid(),
                Title = "Updated Title",
                Content = "Updated Content"
            };
            var updatedTemplate = new Domain.Entities.PromptTemplate
            {
                Id = template.Id,
                Title = "Updated Title",
                Content = "Updated Content",
                UpdatedAt = DateTime.UtcNow
            };

            mockService.Setup(s => s.UpdateTemplateAsync(It.IsAny<Domain.Entities.PromptTemplate>())).ReturnsAsync(updatedTemplate);

            // Act
            var result = await controller.CreateOrUpdate(template);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            var value = Assert.IsType<Domain.Entities.PromptTemplate>(okResult.Value);
            Assert.Equal(template.Id, value.Id);
            Assert.Equal("Updated Title", value.Title);
            mockService.Verify(s => s.UpdateTemplateAsync(It.Is<Domain.Entities.PromptTemplate>(t => t.Title == "Updated Title")), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateController_NullTemplate_ShouldReturnBadRequest()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);

            // Act
            var result = await controller.CreateOrUpdate(null as Domain.Entities.PromptTemplate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("Prompt template cannot be null", badRequestResult.Value);
        }

        [Fact]
        public async Task GetAllController_ShouldReturnAllTemplates()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);
            var expectedTemplates = new List<Domain.Entities.PromptTemplate>
            {
                new Domain.Entities.PromptTemplate { Id = Guid.NewGuid(), Title = "Template 1", Content = "Content 1" },
                new Domain.Entities.PromptTemplate { Id = Guid.NewGuid(), Title = "Template 2", Content = "Content 2" }
            };

            mockService.Setup(s => s.GetAllTemplatesAsync()).ReturnsAsync(expectedTemplates);

            // Act
            var result = await controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            var value = Assert.IsType<List<Domain.Entities.PromptTemplate>>(okResult.Value);
            Assert.Equal(2, value.Count);
            mockService.Verify(s => s.GetAllTemplatesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdController_ShouldReturnTemplate_WhenExists()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);
            var id = Guid.NewGuid();
            var expectedTemplate = new Domain.Entities.PromptTemplate { Id = id, Title = "Test Template", Content = "Test Content" };

            mockService.Setup(s => s.GetTemplateByIdAsync(id)).ReturnsAsync(expectedTemplate);

            // Act
            var result = await controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            var value = Assert.IsType<Domain.Entities.PromptTemplate>(okResult.Value);
            Assert.Equal(id, value.Id);
            mockService.Verify(s => s.GetTemplateByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetByIdController_ShouldReturnNotFound_WhenNotExists()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);
            var id = Guid.NewGuid();

            mockService.Setup(s => s.GetTemplateByIdAsync(id)).ReturnsAsync((Domain.Entities.PromptTemplate)null);

            // Act
            var result = await controller.GetById(id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.NotNull(notFoundResult);
            Assert.Equal(404, notFoundResult.StatusCode);
            mockService.Verify(s => s.GetTemplateByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteController_ShouldReturnNoContent_WhenTemplateExists()
        {
            // Arrange
            var mockService = new Mock<IPromptTemplateService>();
            var controller = new PromptTemplatesController(mockService.Object, _mockLogger.Object);
            var id = Guid.NewGuid();

            mockService.Setup(s => s.DeleteTemplateAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await controller.Delete(id) as NoContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(204, result.StatusCode);
            mockService.Verify(s => s.DeleteTemplateAsync(id), Times.Once);
        }

        // Integration Tests with In-Memory Database
        [Fact]
        public async Task PromptTemplateIntegrationTest_CreateAndRetrieveTemplate()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var repository = new PromptTemplateRepository(context);
            var service = new PromptTemplateService(repository);
            var controller = new PromptTemplatesController(service, new Mock<ILogger<PromptTemplatesController>>().Object);

            var template = new Domain.Entities.PromptTemplate
            {
                Id = Guid.Empty,
                Title = "Integration Test Title",
                Content = "Integration Test Content"
            };

            // Act - Create
            var createResult = await controller.CreateOrUpdate(template);
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdTemplate = createdAtResult.Value as Domain.Entities.PromptTemplate;

            // Assert - Create
            Assert.NotNull(createdTemplate);
            Assert.NotEqual(Guid.Empty, createdTemplate.Id);
            Assert.Equal("Integration Test Title", createdTemplate.Title);
            Assert.Equal("Integration Test Content", createdTemplate.Content);

            // Act - Get By Id
            var getByIdResult = await controller.GetById(createdTemplate.Id);
            var okResult = Assert.IsType<OkObjectResult>(getByIdResult.Result);
            var retrievedTemplate = okResult.Value as Domain.Entities.PromptTemplate;

            // Assert - Get By Id
            Assert.NotNull(retrievedTemplate);
            Assert.Equal("Integration Test Title", retrievedTemplate.Title);
            Assert.Equal("Integration Test Content", retrievedTemplate.Content);
        }

        [Fact]
        public async Task PromptTemplateIntegrationTest_UpdateAndDeleteTemplate()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var repository = new PromptTemplateRepository(context);
            var service = new PromptTemplateService(repository);
            var controller = new PromptTemplatesController(service, new Mock<ILogger<PromptTemplatesController>>().Object);

            var template = new Domain.Entities.PromptTemplate
            {
                Id = Guid.Empty,
                Title = "Original Title",
                Content = "Original Content"
            };

            // Act - Create
            var createResult = await controller.CreateOrUpdate(template);
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdTemplate = createdAtResult.Value as Domain.Entities.PromptTemplate;

            // Assert - Create
            Assert.NotNull(createdTemplate);
            Assert.NotEqual(Guid.Empty, createdTemplate.Id);
            Assert.Equal("Original Title", createdTemplate.Title);

            // Act - Update
            createdTemplate.Title = "Updated Title";
            var updateResult = await controller.CreateOrUpdate(createdTemplate);
            var okResult = Assert.IsType<OkObjectResult>(updateResult.Result);
            var updatedTemplate = okResult.Value as Domain.Entities.PromptTemplate;

            // Assert - Update
            Assert.Equal("Updated Title", updatedTemplate.Title);
            Assert.NotNull(updatedTemplate.UpdatedAt);

            // Act - Delete
            var deleteResult = await controller.Delete(createdTemplate.Id) as NoContentResult;

            // Assert - Delete
            Assert.NotNull(deleteResult);
            Assert.Equal(204, deleteResult.StatusCode);

            // Act - Verify Delete
            var getAfterDeleteResult = await controller.GetById(createdTemplate.Id);

            // Assert - Verify Delete
            Assert.NotNull(getAfterDeleteResult);
            var notFoundResult = Assert.IsType<NotFoundResult>(getAfterDeleteResult.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task PromptTemplateIntegrationTest_FullCRUDOperations()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var repository = new PromptTemplateRepository(context);
            var service = new PromptTemplateService(repository);
            var controller = new PromptTemplatesController(service, new Mock<ILogger<PromptTemplatesController>>().Object);

            // Create multiple templates
            var template1 = new Domain.Entities.PromptTemplate { Id = Guid.Empty, Title = "Template 1", Content = "Content 1" };
            var template2 = new Domain.Entities.PromptTemplate { Id = Guid.Empty, Title = "Template 2", Content = "Content 2" };

            // Act - Create Template 1
            var create1Result = await controller.CreateOrUpdate(template1);
            var createdAtResult1 = Assert.IsType<CreatedAtActionResult>(create1Result.Result);
            var createdTemplate1 = createdAtResult1.Value as Domain.Entities.PromptTemplate;

            // Act - Create Template 2
            var create2Result = await controller.CreateOrUpdate(template2);
            var createdAtResult2 = Assert.IsType<CreatedAtActionResult>(create2Result.Result);
            var createdTemplate2 = createdAtResult2.Value as Domain.Entities.PromptTemplate;

            // Assert - Both created
            Assert.NotNull(createdTemplate1);
            Assert.NotNull(createdTemplate2);
            Assert.NotEqual(Guid.Empty, createdTemplate1.Id);
            Assert.NotEqual(Guid.Empty, createdTemplate2.Id);
            Assert.Equal("Template 1", createdTemplate1.Title);
            Assert.Equal("Template 2", createdTemplate2.Title);

            // Act - Get All
            var getAllResult = await controller.GetAll();
            var okResult = Assert.IsType<OkObjectResult>(getAllResult.Result);
            var allTemplates = okResult.Value as List<Domain.Entities.PromptTemplate>;

            // Assert - Get All
            Assert.NotNull(allTemplates);
            Assert.Equal(2, allTemplates.Count);
            Assert.Contains(allTemplates, t => t.Title == "Template 1");
            Assert.Contains(allTemplates, t => t.Title == "Template 2");

            // Act - Update Template 1
            createdTemplate1.Title = "Updated Template 1";
            var updateResult = await controller.CreateOrUpdate(createdTemplate1);
            var updateOkResult = Assert.IsType<OkObjectResult>(updateResult.Result);
            var updatedTemplate = updateOkResult.Value as Domain.Entities.PromptTemplate;

            // Assert - Update
            Assert.Equal("Updated Template 1", updatedTemplate.Title);
            Assert.NotNull(updatedTemplate.UpdatedAt);

            // Act - Get Updated
            var getUpdatedResult = await controller.GetById(createdTemplate1.Id);
            var getUpdatedOkResult = Assert.IsType<OkObjectResult>(getUpdatedResult.Result);
            var retrievedUpdated = getUpdatedOkResult.Value as Domain.Entities.PromptTemplate;

            // Assert - Updated retrieved correctly
            Assert.Equal("Updated Template 1", retrievedUpdated.Title);

            // Act - Delete Template 2
            var deleteResult = await controller.Delete(createdTemplate2.Id) as NoContentResult;

            // Assert - Delete
            Assert.NotNull(deleteResult);
            Assert.Equal(204, deleteResult.StatusCode);

            // Act - Get All after delete
            var getAllAfterDeleteResult = await controller.GetAll();
            var getAllOkResult = Assert.IsType<OkObjectResult>(getAllAfterDeleteResult.Result);
            var allAfterDelete = getAllOkResult.Value as List<Domain.Entities.PromptTemplate>;

            // Assert - One template left
            Assert.Single(allAfterDelete);
            Assert.Contains(allAfterDelete, t => t.Title == "Updated Template 1");

            // Act - Delete remaining template
            var finalDeleteResult = await controller.Delete(createdTemplate1.Id) as NoContentResult;

            // Assert - Final delete
            Assert.NotNull(finalDeleteResult);
            Assert.Equal(204, finalDeleteResult.StatusCode);
        }
    }
}