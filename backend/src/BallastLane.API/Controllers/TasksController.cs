using BallastLane.API.Extensions;
using BallastLane.API.Services;
using BallastLane.Application.Common;
using BallastLane.Application.CQRS;
using BallastLane.Application.DTOs;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Application.Tasks.Queries;
using BallastLane.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllTasksQuery(currentUser.UserId, page, pageSize);
        var result = await queryDispatcher.SendAsync<GetAllTasksQuery, PagedResult<TaskItem>>(query, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTaskByIdQuery(id, currentUser.UserId);
        var result = await queryDispatcher.SendAsync<GetTaskByIdQuery, TaskItem>(query, cancellationToken);
        return result.ToActionResult(task => Ok(task));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(request.Title, request.Description, request.Status, request.DueDate, currentUser.UserId);
        var result = await commandDispatcher.SendAsync<CreateTaskCommand, TaskItem>(command, cancellationToken);
        return result.ToActionResult(task => CreatedAtAction(nameof(GetById), new { id = task.Id }, task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaskRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTaskCommand(id, request.Title, request.Description, request.Status, request.DueDate, currentUser.UserId);
        var result = await commandDispatcher.SendAsync<UpdateTaskCommand, TaskItem>(command, cancellationToken);
        return result.ToActionResult(task => Ok(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteTaskCommand(id, currentUser.UserId);
        var result = await commandDispatcher.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }
}
