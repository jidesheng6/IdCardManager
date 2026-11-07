using Dapper;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using Newtonsoft.Json;

namespace IdCardManager.Infrastructure.Persistence
{
    public sealed class SettingsRepository : ISettingsRepository
    {
        private readonly SqliteConnectionFactory _factory;

        public SettingsRepository(SqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public AppSettings Get()
        {
            using (var conn = _factory.Create())
            {
                SchemaInitializer.EnsureCreated(conn);
                var json = conn.QuerySingleOrDefault<string>("SELECT Json FROM settings WHERE Id = 1");
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AppSettings();
                }

                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings ?? new AppSettings());
            using (var conn = _factory.Create())
            {
                SchemaInitializer.EnsureCreated(conn);
                conn.Execute(@"
INSERT INTO settings(Id, Json) VALUES(1, @json)
ON CONFLICT(Id) DO UPDATE SET Json = excluded.Json;", new { json });
            }
        }
    }
}
