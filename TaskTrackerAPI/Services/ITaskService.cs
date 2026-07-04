using TaskTrackerAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskTrackerAPI.Services
{
    public interface ITaskService
    {
        Task<List<PersonalTask>> GetAllTasksAsync();
        Task<PersonalTask?> GetTaskByIdAsync(int id);
        Task<PersonalTask> CreateTaskAsync(PersonalTask task);
        Task<PersonalTask?> UpdateTaskAsync(PersonalTask task);
        Task<bool> DeleteTaskAsync(int id);
    }
}
