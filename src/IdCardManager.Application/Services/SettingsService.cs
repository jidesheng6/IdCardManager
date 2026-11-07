using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Application.Services
{
    public interface IParamImporter
    {
        ParamImportResult ImportFromLrCenter(string lrCenterRoot, AppSettings current);
    }

    public sealed class ParamImportResult
    {
        public AppSettings Settings { get; set; }
        public string Message { get; set; }
        public bool Partial { get; set; }
    }

    public sealed class SettingsService
    {
        private readonly ISettingsRepository _repo;
        private readonly IParamImporter _importer;

        public SettingsService(ISettingsRepository repo, IParamImporter importer)
        {
            _repo = repo;
            _importer = importer;
        }

        public AppSettings Get() => _repo.Get();

        public void Save(AppSettings settings) => _repo.Save(settings);

        public ParamImportResult Import(string lrCenterRoot)
        {
            var current = _repo.Get();
            var result = _importer.ImportFromLrCenter(lrCenterRoot, current);
            if (result.Settings != null)
            {
                _repo.Save(result.Settings);
            }

            return result;
        }
    }
}
