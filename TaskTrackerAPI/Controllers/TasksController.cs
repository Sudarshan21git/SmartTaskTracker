using Microsoft.AspNetCore.Mvc;
using TaskTrackerAPI.Services;
using TaskTrackerAPI.Models;

// Add these aliases to resolve naming conflicts
using ModelsTaskStatus = TaskTrackerAPI.Models.TaskStatus;
using ModelsTaskPriority = TaskTrackerAPI.Models.TaskPriority;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PersonalTask>>>> GetTasks()
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(new ApiResponse<List<PersonalTask>>
            {
                Success = true,
                Data = tasks
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<PersonalTask>>
            {
                Success = false,
                Message = "Error fetching tasks"
            });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<PersonalTask>>>> GetTasksByUser(int userId)
    {
        try
        {
            // Get all tasks and filter by user ID
            var allTasks = await _taskService.GetAllTasksAsync();
            var userTasks = allTasks.Where(t => t.UserId == userId).ToList();

            return Ok(new ApiResponse<List<PersonalTask>>
            {
                Success = true,
                Data = userTasks
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<PersonalTask>>
            {
                Success = false,
                Message = "Error fetching user tasks"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PersonalTask>>> CreateTask(PersonalTask task)
    {
        try
        {
            task.CreatedAt = DateTime.UtcNow;
            var createdTask = await _taskService.CreateTaskAsync(task);

            return Ok(new ApiResponse<PersonalTask>
            {
                Success = true,
                Message = "Task created successfully",
                Data = createdTask
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PersonalTask>
            {
                Success = false,
                Message = "Error creating task"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(int id)
    {
        try
        {
            var result = await _taskService.DeleteTaskAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Task not found"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Task deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Error deleting task"
            });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<PersonalTask>>> UpdateTaskStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var existingTask = await _taskService.GetTaskByIdAsync(id);
            if (existingTask == null)
            {
                return NotFound(new ApiResponse<PersonalTask>
                {
                    Success = false,
                    Message = "Task not found"
                });
            }

            // Update only the status
            existingTask.Status = (ModelsTaskStatus)request.Status;

            var updatedTask = await _taskService.UpdateTaskAsync(existingTask);

            return Ok(new ApiResponse<PersonalTask>
            {
                Success = true,
                Message = "Task status updated successfully",
                Data = updatedTask
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PersonalTask>
            {
                Success = false,
                Message = "Error updating task status"
            });
        }
    }

    // Optional: Add full task update endpoint
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PersonalTask>>> UpdateTask(int id, PersonalTask task)
    {
        try
        {
            if (id != task.Id)
            {
                return BadRequest(new ApiResponse<PersonalTask>
                {
                    Success = false,
                    Message = "Task ID mismatch"
                });
            }

            var updatedTask = await _taskService.UpdateTaskAsync(task);
            if (updatedTask == null)
            {
                return NotFound(new ApiResponse<PersonalTask>
                {
                    Success = false,
                    Message = "Task not found"
                });
            }

            return Ok(new ApiResponse<PersonalTask>
            {
                Success = true,
                Message = "Task updated successfully",
                Data = updatedTask
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PersonalTask>
            {
                Success = false,
                Message = "Error updating task"
            });
        }
    }

    [HttpPost("daily-schedule")]
    public async Task<ActionResult<ApiResponse<DailyScheduleResponse>>> GenerateDailySchedule([FromBody] DailyScheduleRequest request)
    {
        try
        {
            if (request.AvailableMinutes <= 0)
            {
                return BadRequest(new ApiResponse<DailyScheduleResponse>
                {
                    Success = false,
                    Message = "Available minutes must be greater than zero"
                });
            }

            var schedule = await _taskService.GenerateDailyScheduleAsync(request.UserId, request.AvailableMinutes);

            return Ok(new ApiResponse<DailyScheduleResponse>
            {
                Success = true,
                Message = schedule.ScheduledTasks.Count == 0
                    ? "No tasks could be scheduled for the available time"
                    : "Daily schedule generated successfully",
                Data = schedule
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new ApiResponse<DailyScheduleResponse>
            {
                Success = false,
                Message = "Error generating daily schedule"
            });
        }
    }
}

public class UpdateStatusRequest
{
    public int Status { get; set; }
}