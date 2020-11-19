//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongODM.Core.Attributes;
using Etherna.MongODM.Core.Domain.Models.DbMigrationOpAgg;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Domain.Models
{
    public class DbMigrationOperation : OperationBase
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
        private List<MigrationLogBase> _logs = new List<MigrationLogBase>();

        // Constructors.
        public DbMigrationOperation(IDbContext dbContext)
            : base(dbContext)
        {
            CurrentStatus = Status.New;
        }
        protected DbMigrationOperation() { }

        // Properties.
        public virtual DateTime? CompletedDateTime { get; protected set; }
        public virtual Status CurrentStatus { get; protected set; }
        public virtual IEnumerable<MigrationLogBase> Logs
        {
            get => _logs;
            protected set => _logs = new List<MigrationLogBase>(value ?? Array.Empty<MigrationLogBase>());
        }
        public virtual string? TaskId { get; protected set; }

        // Methods.
        [PropertyAlterer(nameof(Logs))]
        public virtual void AddLog(MigrationLogBase log)
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
