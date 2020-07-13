using Etherna.MongODM.Attributes;
using Etherna.MongODM.Models.Internal.MigrateOpAgg;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Models.Internal
{
    public class MigrateOperation : OperationBase
    {
        // Enums.
        public enum Status
        {
            New,
            Running,
            Completed,
            Cancelled
        }

        // Fields.
        private List<MigrateExecutionLog> _logs = new List<MigrateExecutionLog>();

        // Constructors.
        public MigrateOperation(IDbContext dbContext, string? author)
            : base(dbContext)
        {
            Author = author;
            CurrentStatus = Status.New;
        }
        protected MigrateOperation() { }

        // Properties.
        public virtual string? Author { get; protected set; }
        public virtual DateTime CompletedDateTime { get; protected set; }
        public virtual Status CurrentStatus { get; protected set; }
        public virtual IEnumerable<MigrateExecutionLog> Logs
        {
            get => _logs;
            protected set => _logs = new List<MigrateExecutionLog>(value ?? Array.Empty<MigrateExecutionLog>());
        }
        public virtual string? TaskId { get; protected set; }

        // Methods.
        [PropertyAlterer(nameof(Logs))]
        public virtual void AddLog(MigrateExecutionLog log)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));

            _logs.Add(log);
        }

        [PropertyAlterer(nameof(CurrentStatus))]
        public virtual void TaskCancelled()
        {
            if (CurrentStatus == Status.Completed)
                throw new InvalidOperationException();

            CurrentStatus = Status.Cancelled;
        }

        [PropertyAlterer(nameof(CompletedDateTime))]
        [PropertyAlterer(nameof(CurrentStatus))]
        public virtual void TaskCompleted()
        {
            if (CurrentStatus != Status.Running)
                throw new InvalidOperationException();

            CompletedDateTime = DateTime.Now;
            CurrentStatus = Status.Completed;
        }

        [PropertyAlterer(nameof(CurrentStatus))]
        [PropertyAlterer(nameof(TaskId))]
        public virtual void TaskStarted(string taskId)
        {
            if (taskId is null)
                throw new ArgumentNullException(nameof(taskId));

            if (CurrentStatus != Status.New)
                throw new InvalidOperationException();

            CurrentStatus = Status.Running;
            TaskId = taskId;
        }
    }
}
