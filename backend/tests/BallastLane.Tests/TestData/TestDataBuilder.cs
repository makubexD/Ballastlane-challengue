using BallastLane.Application.DTOs;
using BallastLane.Domain.Entities;
using BallastLane.Domain.ValueObjects;

namespace BallastLane.Tests.TestData;

internal static class TestDataBuilder
{
    internal static CreateTaskRequest ValidCreateRequest(
        string? title = null,
        string? description = null,
        DateTime? dueDate = null,
        TaskItemStatus status = TaskItemStatus.Todo) => new(
            title ?? TestConstants.ValidTitle,
            description ?? TestConstants.ValidDescription,
            status,
            dueDate ?? TestConstants.FutureDueDate);

    internal static UpdateTaskRequest ValidUpdateRequest(
        string? title = null,
        string? description = null,
        DateTime? dueDate = null,
        TaskItemStatus status = TaskItemStatus.InProgress) => new(
            title ?? TestConstants.ValidTitle,
            description ?? TestConstants.ValidDescription,
            status,
            dueDate ?? TestConstants.FutureDueDate);

    internal static TaskItem ValidTask(
        Guid? id = null,
        Guid? userId = null,
        string? title = null) => TaskItem.Create(
            id ?? TestConstants.ValidTaskId,
            title ?? TestConstants.ValidTitle,
            TestConstants.ValidDescription,
            TestConstants.DefaultStatus,
            TestConstants.FutureDueDate,
            userId ?? TestConstants.ValidUserId);
}
