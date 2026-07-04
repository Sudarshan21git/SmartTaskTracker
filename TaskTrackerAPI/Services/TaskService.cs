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
        private const int IndividualKnapsackCapacity = 5;
        private const int DefaultEstimatedMinutes = 60;
        private readonly AppDbContext _context;

        private sealed class ScheduleCandidate
        {
            public PersonalTask Task { get; init; } = default!;
            public int EstimatedMinutes { get; init; }
            public double Score { get; init; }
        }

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
            if (task.EstimatedMinutes == null || task.EstimatedMinutes <= 0)
            {
                task.EstimatedMinutes = DefaultEstimatedMinutes;
            }

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            if (await IsIndividualProfileAsync(task.UserId))
            {
                await RecalculateAllIncompleteTaskScores(task.UserId, useKnapsack: true);

                var createdTask = await _context.Tasks.FindAsync(task.Id);
                return createdTask ?? task;
            }

            var userTasks = await GetUserTasksAsync(task.UserId);
            task.Score = CalculateExactPriorityScore(task, userTasks);
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
            existingTask.EstimatedMinutes = task.EstimatedMinutes > 0 ? task.EstimatedMinutes : DefaultEstimatedMinutes;
            existingTask.Status = task.Status;
            existingTask.Priority = task.Priority;

            var isIndividualProfile = await IsIndividualProfileAsync(existingTask.UserId);

            if (isIndividualProfile)
            {
                await _context.SaveChangesAsync();
                await RecalculateAllIncompleteTaskScores(existingTask.UserId, useKnapsack: true);

                return await _context.Tasks.FindAsync(existingTask.Id);
            }

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
        private async Task RecalculateAllIncompleteTaskScores(int userId, bool useKnapsack = false)
        {
            var userTasks = await GetUserTasksAsync(userId);
            var incompleteTasks = userTasks.Where(t => t.Status != ModelsTaskStatus.Completed).ToList();

            if (useKnapsack)
            {
                var taskScores = CalculateKnapsackScores(userTasks, incompleteTasks);

                foreach (var task in incompleteTasks)
                {
                    task.Score = taskScores.TryGetValue(task.Id, out var score) ? score : 0.001;
                    _context.Entry(task).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                return;
            }

            foreach (var task in incompleteTasks)
            {
                task.Score = CalculateExactPriorityScore(task, userTasks);
                _context.Entry(task).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> IsIndividualProfileAsync(int userId)
        {
            var role = await _context.Users
                .Where(user => user.UserId == userId)
                .Select(user => user.Role)
                .FirstOrDefaultAsync();

            return role == UserRole.User;
        }

        public async Task<DailyScheduleResponse> GenerateDailyScheduleAsync(int userId, int availableMinutes)
        {
            if (availableMinutes <= 0)
            {
                return new DailyScheduleResponse
                {
                    UserId = userId,
                    AvailableMinutes = 0,
                    RemainingMinutes = 0
                };
            }

            if (!await IsIndividualProfileAsync(userId))
            {
                return new DailyScheduleResponse
                {
                    UserId = userId,
                    AvailableMinutes = availableMinutes,
                    RemainingMinutes = availableMinutes
                };
            }

            var userTasks = await GetUserTasksAsync(userId);
            var incompleteTasks = userTasks
                .Where(task => task.Status != ModelsTaskStatus.Completed)
                .ToList();

            if (incompleteTasks.Count == 0)
            {
                return new DailyScheduleResponse
                {
                    UserId = userId,
                    AvailableMinutes = availableMinutes,
                    RemainingMinutes = availableMinutes
                };
            }

            var taskData = incompleteTasks.Select(task => new ScheduleCandidate
            {
                Task = task,
                EstimatedMinutes = Math.Max(1, task.EstimatedMinutes ?? DefaultEstimatedMinutes),
                Score = task.Score ?? CalculateExactPriorityScore(task, userTasks)
            }).ToList();

            var selectedIds = SolveScheduleKnapsack(taskData, availableMinutes);
            var scheduledTasks = new List<DailyScheduleItem>();
            var unscheduledTasks = new List<DailyScheduleItem>();
            var plannedOrder = 1;
            var scheduledMinutes = 0;

            foreach (var item in taskData.OrderByDescending(x => x.Score).ThenBy(x => x.Task.DueDate ?? DateTime.MaxValue))
            {
                var scheduleItem = new DailyScheduleItem
                {
                    TaskId = item.Task.Id,
                    Title = item.Task.Title,
                    Description = item.Task.Description,
                    DueDate = item.Task.DueDate,
                    Priority = (int)item.Task.Priority,
                    Status = (int)item.Task.Status,
                    EstimatedMinutes = item.EstimatedMinutes,
                    Score = Math.Round(item.Score, 4),
                    PlannedOrder = 0
                };

                if (selectedIds.Contains(item.Task.Id))
                {
                    scheduleItem.PlannedOrder = plannedOrder++;
                    scheduledTasks.Add(scheduleItem);
                    scheduledMinutes += item.EstimatedMinutes;
                }
                else
                {
                    unscheduledTasks.Add(scheduleItem);
                }
            }

            scheduledTasks = scheduledTasks
                .OrderBy(item => item.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(item => item.Score)
                .ToList();

            for (var i = 0; i < scheduledTasks.Count; i++)
            {
                scheduledTasks[i].PlannedOrder = i + 1;
            }

            return new DailyScheduleResponse
            {
                UserId = userId,
                AvailableMinutes = availableMinutes,
                ScheduledMinutes = scheduledMinutes,
                RemainingMinutes = Math.Max(0, availableMinutes - scheduledMinutes),
                ScheduledTasks = scheduledTasks,
                UnscheduledTasks = unscheduledTasks
            };
        }

        private HashSet<int> SolveScheduleKnapsack(List<ScheduleCandidate> taskData, int availableMinutes)
        {
            var count = taskData.Count;
            var capacity = availableMinutes;
            var dp = new double[count + 1, capacity + 1];
            var keep = new bool[count + 1, capacity + 1];

            for (var i = 1; i <= count; i++)
            {
                int weight = taskData[i - 1].EstimatedMinutes;
                double value = taskData[i - 1].Score;

                for (var minutes = 0; minutes <= capacity; minutes++)
                {
                    var withoutTask = dp[i - 1, minutes];
                    var withTask = minutes >= weight
                        ? dp[i - 1, minutes - weight] + value
                        : double.MinValue;

                    if (withTask > withoutTask)
                    {
                        dp[i, minutes] = withTask;
                        keep[i, minutes] = true;
                    }
                    else
                    {
                        dp[i, minutes] = withoutTask;
                    }
                }
            }

            var selectedTaskIds = new HashSet<int>();
            var remainingMinutes = capacity;

            for (var i = count; i >= 1 && remainingMinutes >= 0; i--)
            {
                if (keep[i, remainingMinutes])
                {
                    selectedTaskIds.Add(taskData[i - 1].Task.Id);
                    remainingMinutes -= taskData[i - 1].EstimatedMinutes;
                    if (remainingMinutes < 0)
                    {
                        remainingMinutes = 0;
                    }
                }
            }

            return selectedTaskIds;
        }

        private Dictionary<int, double> CalculateKnapsackScores(
            List<PersonalTask> allUserTasks,
            List<PersonalTask> incompleteTasks)
        {
            if (incompleteTasks.Count == 0)
            {
                return new Dictionary<int, double>();
            }

            var baseCap = Math.Min(IndividualKnapsackCapacity, incompleteTasks.Count);
            var taskUtilities = incompleteTasks.ToDictionary(
                task => task.Id,
                task => CalculateExactPriorityScore(task, allUserTasks));

            var selectedTaskIds = SolveKnapsackSelection(incompleteTasks, taskUtilities, baseCap);
            var scores = new Dictionary<int, double>();

            foreach (var task in incompleteTasks)
            {
                var utility = taskUtilities[task.Id];
                var score = selectedTaskIds.Contains(task.Id)
                    ? 0.5 + (utility * 0.5)
                    : utility * 0.25;

                scores[task.Id] = Math.Round(Math.Max(0.001, Math.Min(0.999, score)), 4);
            }

            return scores;
        }

        private HashSet<int> SolveKnapsackSelection(
            List<PersonalTask> tasks,
            Dictionary<int, double> utilities,
            int capacity)
        {
            var count = tasks.Count;
            var dp = new double[count + 1, capacity + 1];
            var keep = new bool[count + 1, capacity + 1];

            for (var i = 1; i <= count; i++)
            {
                var task = tasks[i - 1];
                var value = utilities[task.Id];

                for (var currentCapacity = 0; currentCapacity <= capacity; currentCapacity++)
                {
                    var withoutTask = dp[i - 1, currentCapacity];
                    var withTask = currentCapacity >= 1
                        ? dp[i - 1, currentCapacity - 1] + value
                        : double.MinValue;

                    if (withTask > withoutTask)
                    {
                        dp[i, currentCapacity] = withTask;
                        keep[i, currentCapacity] = true;
                    }
                    else
                    {
                        dp[i, currentCapacity] = withoutTask;
                    }
                }
            }

            var selectedTaskIds = new HashSet<int>();
            var remainingCapacity = capacity;

            for (var i = count; i >= 1 && remainingCapacity >= 0; i--)
            {
                if (keep[i, remainingCapacity])
                {
                    var task = tasks[i - 1];
                    selectedTaskIds.Add(task.Id);
                    remainingCapacity--;
                }
            }

            return selectedTaskIds;
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
