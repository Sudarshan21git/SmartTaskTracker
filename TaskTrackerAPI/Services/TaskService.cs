using Microsoft.EntityFrameworkCore;
using TaskTrackerAPI.Data;
using TaskTrackerAPI.Models;
using TaskTrackerAPI.Services;

// Aliases to avoid naming conflicts
using ModelsTaskStatus = TaskTrackerAPI.Models.TaskStatus;
using ModelsTaskPriority = TaskTrackerAPI.Models.TaskPriority;

namespace TaskTrackerAPI.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        // Get tasks for a user (Completed tasks always at bottom)
        public async Task<List<PersonalTask>> GetUserTasksAsync(int userId)
        {
            return await _context.Tasks
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Status == ModelsTaskStatus.Completed) // Completed at bottom
                .ThenByDescending(t => t.Score)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<PersonalTask> CreateTaskAsync(PersonalTask task)
        {
            var userTasks = await GetUserTasksAsync(task.UserId);
            task.Score = CalculateExactPriorityScore(task, userTasks);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<PersonalTask?> UpdateTaskAsync(PersonalTask task)
        {
            var existingTask = await _context.Tasks.FindAsync(task.Id);
            if (existingTask == null)
                throw new Exception("Task not found");

            var oldStatus = existingTask.Status;

            // Update fields
            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Status = task.Status;
            existingTask.Priority = task.Priority;

            // ---------- SCORE LOGIC ----------
            var userTasks = await GetUserTasksAsync(existingTask.UserId);

            if (existingTask.Status == ModelsTaskStatus.Completed)
            {
                // Any task marked Completed → minimal score
                existingTask.Score = 0.001;
            }
            else if (oldStatus == ModelsTaskStatus.Completed && existingTask.Status != ModelsTaskStatus.Completed)
            {
                // Task reopened from Completed → recalc score dynamically
                existingTask.Score = CalculateExactPriorityScore(existingTask, userTasks);

                // Optional: recalc all other incomplete tasks
                await RecalculateAllIncompleteTaskScores(existingTask.UserId);
            }
            else
            {
                // Pending ↔ InProgress updates → recalc dynamically
                existingTask.Score = CalculateExactPriorityScore(existingTask, userTasks);
            }

            await _context.SaveChangesAsync();
            return existingTask;
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<PersonalTask?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks.FindAsync(taskId);
        }

        public async Task<List<PersonalTask>> GetAllTasksAsync()
        {
            return await _context.Tasks
                .OrderBy(t => t.Status == ModelsTaskStatus.Completed)
                .ThenByDescending(t => t.Score)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PersonalTask>> GetTasksByPriorityAsync(string priority)
        {
            if (!Enum.TryParse<ModelsTaskPriority>(priority, true, out var parsedPriority))
            {
                return new List<PersonalTask>();
            }

            return await _context.Tasks
                .Where(t => t.Priority == parsedPriority)
                .OrderBy(t => t.Status == ModelsTaskStatus.Completed)
                .ThenByDescending(t => t.Score)
                .ToListAsync();
        }

        public async Task<List<PersonalTask>> GetOverdueTasksAsync()
        {
            return await _context.Tasks
                .Where(t => t.DueDate < DateTime.UtcNow && t.Status != ModelsTaskStatus.Completed)
                .OrderByDescending(t => t.Score)
                .ToListAsync();
        }

        // Recalculate all incomplete tasks for a user
        private async Task RecalculateAllIncompleteTaskScores(int userId)
        {
            var userTasks = await GetUserTasksAsync(userId);
            var incompleteTasks = userTasks.Where(t => t.Status != ModelsTaskStatus.Completed).ToList();

            foreach (var task in incompleteTasks)
            {
                task.Score = CalculateExactPriorityScore(task, userTasks);
                _context.Entry(task).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        // Calculate task score (ignores completed tasks)
        private double CalculateExactPriorityScore(PersonalTask task, List<PersonalTask> allUserTasks)
        {
            if (task.Status == ModelsTaskStatus.Completed)
                return 0.001; // Completed → minimal score

            var incompleteTasks = allUserTasks
                .Where(t => t.Status != ModelsTaskStatus.Completed && t.Id != task.Id)
                .ToList();

            var tasksForCalculation = incompleteTasks.ToList();
            tasksForCalculation.Add(task);

            if (tasksForCalculation.Count == 0) return 0.5;
            if (tasksForCalculation.Count == 1) return 0.9;

            var priorityValues = tasksForCalculation.Select(t => GetPriorityNumericValue(t.Priority)).ToList();
            var statusValues = tasksForCalculation.Select(t => GetStatusNumericValue(t.Status)).ToList();
            var dueDateValues = tasksForCalculation.Select(t => GetDueDateUrgencyValue(t.DueDate)).ToList();

            var normalizedPriorities = NormalizeToSumOne(priorityValues);
            var normalizedStatuses = NormalizeToSumOne(statusValues);
            var normalizedDueDates = NormalizeToSumOne(dueDateValues);

            double priorityEntropy = CalculateEntropy(normalizedPriorities);
            double statusEntropy = CalculateEntropy(normalizedStatuses);
            double dueDateEntropy = CalculateEntropy(normalizedDueDates);

            double priorityInfo = 1 - priorityEntropy;
            double statusInfo = 1 - statusEntropy;
            double dueDateInfo = 1 - dueDateEntropy;

            double totalInfo = priorityInfo + statusInfo + dueDateInfo;

            int taskIndex = tasksForCalculation.FindIndex(t => t.Id == task.Id);
            if (taskIndex == -1) return 0.5;

            if (totalInfo == 0)
            {
                return (normalizedPriorities[taskIndex] + normalizedStatuses[taskIndex] + normalizedDueDates[taskIndex]) / 3.0;
            }

            double priorityWeight = priorityInfo / totalInfo;
            double statusWeight = statusInfo / totalInfo;
            double dueDateWeight = dueDateInfo / totalInfo;

            double finalScore =
                (priorityWeight * normalizedPriorities[taskIndex]) +
                (statusWeight * normalizedStatuses[taskIndex]) +
                (dueDateWeight * normalizedDueDates[taskIndex]);

            return Math.Round(Math.Max(0.001, Math.Min(0.999, finalScore)), 4);
        }

        private double GetPriorityNumericValue(ModelsTaskPriority priority)
        {
            return priority switch
            {
                ModelsTaskPriority.Low => 1.0,
                ModelsTaskPriority.Medium => 2.0,
                ModelsTaskPriority.High => 3.0,
                _ => 2.0
            };
        }

        private double GetStatusNumericValue(ModelsTaskStatus status)
        {
            return status switch
            {
                ModelsTaskStatus.Pending => 1.0,
                ModelsTaskStatus.InProgress => 2.0,
                ModelsTaskStatus.Completed => 0.1, // Should never be used in calc
                _ => 1.0
            };
        }

        private double GetDueDateUrgencyValue(DateTime? dueDate)
        {
            if (dueDate == null) return 0.3;

            var daysRemaining = (dueDate.Value - DateTime.UtcNow).TotalDays;

            if (daysRemaining < 0) return 1.0;
            else if (daysRemaining <= 1) return 0.9;
            else if (daysRemaining <= 3) return 0.8;
            else if (daysRemaining <= 7) return 0.7;
            else if (daysRemaining <= 30) return 0.5;
            else return 0.3;
        }

        private double CalculateEntropy(List<double> normalizedValues)
        {
            if (normalizedValues == null || normalizedValues.Count <= 1) return 1.0;

            double entropy = 0.0;
            int n = normalizedValues.Count;
            double k = 1.0 / Math.Log(n);

            foreach (var value in normalizedValues)
            {
                if (value > 0)
                    entropy += value * Math.Log(value);
            }

            return -k * entropy;
        }

        private List<double> NormalizeToSumOne(List<double> values)
        {
            if (values == null || values.Count == 0) return new List<double>();

            double sum = values.Sum();
            if (sum == 0) return values.Select(v => 1.0 / values.Count).ToList();

            return values.Select(v => v / sum).ToList();
        }
    }
}
