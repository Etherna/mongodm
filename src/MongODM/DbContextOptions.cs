using Digicando.ExecContext;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Tasks;
using System;
using System.Collections.Generic;

namespace Digicando.MongODM
{
    public abstract class DbContextOptions
    {
        public DbContextOptions(
            string connectionString,
            string dbName,
            DocumentVersion documentVersion,
            IEnumerable<IContextAccessor> execContextAccessors,
            IProxyGenerator proxyGenerator,
            ITaskRunner taskRunner)
        {
            ConnectionString = connectionString;
            DBName = dbName;
            DocumentVersion = documentVersion;
            ExecContextAccessors = execContextAccessors ?? throw new ArgumentNullException(nameof(execContextAccessors));
            ProxyGenerator = proxyGenerator;
            TaskRunner = taskRunner;
        }

        public string ConnectionString { get; }
        public string DBName { get; }
        public DocumentVersion DocumentVersion { get; }
        public IEnumerable<IContextAccessor> ExecContextAccessors { get; }
        public IProxyGenerator ProxyGenerator { get; }
        public ITaskRunner TaskRunner { get; }
    }

    public class DbContextOptions<TDbContext> : DbContextOptions
        where TDbContext : DbContext
    {
        public DbContextOptions(
            string connectionString,
            string dbName,
            DocumentVersion documentVersion,
            IEnumerable<IContextAccessor> execContextAccessors,
            IProxyGenerator proxyGenerator,
            ITaskRunner taskRunner)
            : base(connectionString, dbName, documentVersion, execContextAccessors, proxyGenerator, taskRunner)
        { }

        public Type DbContextType => typeof(TDbContext);
    }
}
