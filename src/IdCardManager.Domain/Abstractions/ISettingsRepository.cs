using IdCardManager.Domain.Entities;

namespace IdCardManager.Domain.Abstractions
{
    public interface ISettingsRepository
    {
        AppSettings Get();
        void Save(AppSettings settings);
    }
}
