using BallastLane.API.Extensions;
using BallastLane.API.Services;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController(ITaskService taskService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await taskService.GetAllTasksAsync(currentUser.UserId, cancellationToken);
        return result.ToActionResult(tasks => Ok(tasks));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await taskService.GetTaskByIdAsync(id, currentUser.UserId, cancellationToken);
        return result.ToActionResult(task => Ok(task));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.CreateTaskAsync(request, currentUser.UserId, cancellationToken);
        return result.ToActionResult(task => CreatedAtAction(nameof(GetById), new { id = task.Id }, task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.UpdateTaskAsync(id, request, currentUser.UserId, cancellationToken);
        return result.ToActionResult(task => Ok(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await taskService.DeleteTaskAsync(id, currentUser.UserId, cancellationToken);
        return result.ToActionResult();
    }
}
