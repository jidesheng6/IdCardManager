using IdCardManager.Application.Services;
using IdCardManager.Domain;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Infrastructure.Card;
using IdCardManager.Infrastructure.Import;
using IdCardManager.Infrastructure.Persistence;

namespace IdCardManager.UI.Composition
{
    public sealed class AppComposition
    {
        public AppSettings Settings { get; }
        public ICardFacade Card { get; }
        public IReaderMonitor Monitor { get; }
        public ReaderStatusHub StatusHub { get; }
        public CardSession Session { get; }
        public StoreValueService StoreValue { get; }
        public WithdrawValueService WithdrawValue { get; }
        public CardDataService CardData { get; }
        public SettingsService SettingsService { get; }
        public ITransactionRepository Trades { get; }

        public AppComposition()
        {
            var db = new SqliteConnectionFactory();
            var settingsRepo = new SettingsRepository(db);
            // 站点参数写死；仅保留库里的「模拟读卡器」开关便于调试
            Settings = StationDefaults.Merge(settingsRepo.Get());
            settingsRepo.Save(Settings);
            Trades = new TransactionRepository(db);

            Card = Settings.UseFakeCard
                ? (ICardFacade)new FakeCardFacade()
                : new SpCardFacade(Settings);

            Monitor = new ReaderMonitor(Card, Settings.ReaderPollMs);
            StatusHub = new ReaderStatusHub(Monitor, Card);
            Session = new CardSession();
            StoreValue = new StoreValueService(Card, Monitor, Trades, settingsRepo);
            WithdrawValue = new WithdrawValueService(Card, Monitor, Trades, settingsRepo);
            CardData = new CardDataService(Card, Monitor);
            SettingsService = new SettingsService(settingsRepo, new LrCenterParamImporter());

            OilTypeCatalog.TryLoadFromSql(".", "DatCent_Single");
        }
    }
}
