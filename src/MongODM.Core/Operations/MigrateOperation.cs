using Etherna.MongODM.Attributes;
using System;

namespace Etherna.MongODM.Operations
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

        // Constructors.
        public MigrateOperation(IDbContext dbContext, string? author)
            : base(dbContext)
        {
            Author = author;
            CurrentStatus = Status.New;
        }

        // Properties.
        public virtual string? Author { get; protected set; }
        public virtual DateTime CompletedDateTime { get; protected set; }
        public virtual Status CurrentStatus { get; protected set; }
        public virtual string? TaskId { get; protected set; }

        // Methods.
        [PropertyAlterer(nameof(CurrentStatus))]
        public void TaskCancelled()
        {
            if (CurrentStatus == Status.Completed)
                throw new InvalidOperationException();

            CurrentStatus = Status.Cancelled;
        }

        [PropertyAlterer(nameof(CompletedDateTime))]
        [PropertyAlterer(nameof(CurrentStatus))]
        public void TaskCompleted()
        {
            if (CurrentStatus != Status.Running)
                throw new InvalidOperationException();

            CompletedDateTime = DateTime.Now;
            CurrentStatus = Status.Completed;
        }

        [PropertyAlterer(nameof(CurrentStatus))]
        [PropertyAlterer(nameof(TaskId))]
        public void TaskStarted(string taskId)
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
