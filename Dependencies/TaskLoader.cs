using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class TaskLoader
    {
        private readonly ILogger<TaskLoader> _logger;
        private readonly SnapshootOptions _snapshootOptions;
        private readonly TaskManagerOptions _taskManagerOptions;

        public TaskLoader(ILogger<TaskLoader> logger, IOptions<TaskManagerOptions> taskManagerOptions, IOptions<SnapshootOptions> snapshootOptions) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new TaskLoader()");
            _taskManagerOptions = taskManagerOptions?.Value ?? throw new ArgumentNullException(nameof(taskManagerOptions));
            _snapshootOptions = snapshootOptions?.Value ?? throw new ArgumentNullException(nameof(snapshootOptions));

            // config check
            if (string.IsNullOrWhiteSpace(_taskManagerOptions.TaskList))
            {
                throw new ArgumentNullException(nameof(taskManagerOptions), "TaskList is empty !");
            }
            else if (_taskManagerOptions.LoopTaskLimit <= 0 && _taskManagerOptions.TaskList.Contains(",LOOP", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(taskManagerOptions), "LoopTaskLimit must be greater than 0 when LOOP task is used !");
            }
        }

        internal IEnumerable<string> GetTaskNameList()
        {
            _logger.LogTrace("GetTaskNameList()");

            StringBuilder tasks = new StringBuilder(_taskManagerOptions.TaskList.ToUpperInvariant());
            if (_taskManagerOptions.SaveAfterEachAction)
            {
                tasks = tasks
                    .Replace(",", ",SAVE,") // brut
                    .Replace("WAIT,SAVE", "WAIT") // useless save removed
                    .Replace("LOOP,SAVE", "LOOP") // useless save removed
                    .Replace("BEGINLOOP,SAVE", "BEGINLOOP"); // useless save removed
            }
            if (_taskManagerOptions.SaveOnEnd || _taskManagerOptions.SaveAfterEachAction) // botSaveAfterEachAction because last action doesn t have a , after and the replace didn t added it
            {
                tasks = tasks
                    .Append(",SAVE"); // last one
            }
            if (_taskManagerOptions.SaveOnLoop && !_taskManagerOptions.SaveAfterEachAction)
            {
                tasks = tasks
                    .Replace(",LOOP", ",SAVE,LOOP");
            }
            tasks = tasks
                .Replace("SAVE,LOOP,SAVE", "SAVE,LOOP") // small optim if Save on loop and save at end and finish by a loop
                .Replace("SAVE,SAVE", "SAVE"); // both config save management could have duplicated this task

            string computedTasks = tasks.ToString(); // resolve for the IndexOf

            // Loop Management
            int iEnd = computedTasks
                            .IndexOf(",LOOP");
            if (iEnd > 0)
            {
                int iStart = computedTasks.IndexOf("BEGINLOOP,");
                tasks = new StringBuilder(computedTasks); // faster work on string
                tasks = tasks
                    .Replace(",LOOP", "");
                string loopedTasks;
                if (iStart >= 0)
                {
                    iEnd -= 10;
                    loopedTasks = computedTasks.Substring(iStart + 10, iEnd - iStart);
                    tasks = tasks.Remove(iStart, 10); // BEGINLOOP,
                }
                else
                {
                    loopedTasks = computedTasks.Substring(0, iEnd);
                }

                for (int i = 0; i < _taskManagerOptions.LoopTaskLimit; i++)
                {
                    tasks = tasks.Insert(iEnd, loopedTasks);
                    tasks = tasks.Insert(iEnd, ',');
                }
                computedTasks = tasks.ToString(); // resolve
            }

            // is BEGINSNAPSHOOT and ENDSNAPSHOOT usefull and not already set manually ?
            if (_snapshootOptions.MakeSnapShootEachSeconds <= 0 || computedTasks.Contains("SNAPSHOOT,", StringComparison.Ordinal))
            {
                computedTasks = string.Concat("LOADING,LOGGING,SAVE,", computedTasks);
            }
            else
            {
                computedTasks = string.Concat("LOADING,BEGINSNAPSHOOT,LOGGING,SAVE,", computedTasks, ",ENDSNAPSHOOT");
            }

            _logger.LogDebug("Task list : {0}", computedTasks);
            return computedTasks
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}