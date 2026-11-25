using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Transportation;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Assets;

namespace THMS.Logic.Orchestrators
{
    public class AssetOrchestrator
    {
        private readonly IAssetDataStore _assets;
        private readonly IAccountDataStore _accounts;
        private readonly IAccountStatementDataStore _statements;
        private readonly IVehicleDataStore _vehicles;
        private readonly PlanningOrchestrator _planning;

        public AssetOrchestrator()
            : this(
                new DataStoreFactory().GetAssetStore(),
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetAccountStatementStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetVehicleStore())
        {
        }

        public AssetOrchestrator(
            IAssetDataStore assets,
            IAccountDataStore accounts,
            IAccountStatementDataStore statements,
            ITransactionDataStore transactions,
            IVehicleDataStore vehicles)
        {
            _assets = assets;
            _accounts = accounts;
            _statements = statements;
            _vehicles = vehicles;
            _planning = new PlanningOrchestrator(accounts, transactions, statements, assets);
        }

        public IReadOnlyList<AssetListRow> GetAssetRows()
        {
            EnsureVehicleAssets();
            var accounts = _accounts.GetAllAccounts().ToList();
            return _assets.GetAll()
                .Select(asset => new AssetListRow
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    Kind = AssetKinds.Of(asset),
                    Category = asset.Category,
                    AcquiredOn = asset.AcquiredOn?.ToString("d") ?? "",
                    CostBasis = asset.CostBasis,
                    LatestValue = LatestValue(asset, _assets.GetValuations(asset.Id)),
                    SecuredAccount = SecuredAccount(accounts, asset)?.Name ?? ""
                })
                .ToList();
        }

        public Asset? GetAsset(Guid id) => _assets.Get(id);

        public IReadOnlyList<AssetValuation> GetValuations(Guid assetId) => _assets.GetValuations(assetId);

        public IReadOnlyList<ValuationSource> GetValuationSources() => _assets.GetSources();

        public IReadOnlyList<ValuationSource> GetValuationSources(AppraisedAssetKind kind) =>
            GetValuationSources().Where(source => source.Appraises(kind)).ToList();

        public ValuationSource? GetValuationSource(Guid id) =>
            GetValuationSources().FirstOrDefault(source => source.Id == id);

        public void SaveValuationSource(ValuationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            source.Name = source.Name?.Trim() ?? "";
            if (source.Name.Length == 0)
                throw new InvalidOperationException("Name is required.");
            if (source.Kinds == AppraisedAssetKind.None)
                throw new InvalidOperationException("Choose the assets this source can appraise.");
            if (!ValuationSource.TryNormalizeWebsite(source.WebsiteUrl, out var website))
                throw new InvalidOperationException("Website must be an http or https address.");
            source.WebsiteUrl = website;
            var duplicate = GetValuationSources().FirstOrDefault(item =>
                item.Id != source.Id && string.Equals(item.Name, source.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
                throw new InvalidOperationException($"A source named {source.Name} already exists.");
            _assets.SaveSource(source);
        }

        public void DeleteValuationSource(Guid id) => _assets.DeleteSource(id);

        public IReadOnlyList<PropertyTaxListRow> GetPropertyTaxRows(Guid assetId)
        {
            var accounts = _accounts.GetAllAccounts().ToDictionary(account => account.Id);
            return _statements.GetForAsset(assetId)
                .OfType<PropertyTaxStatement>()
                .Select(statement => new PropertyTaxListRow
                {
                    Id = statement.Id,
                    TaxYear = statement.TaxYear,
                    ProcessDate = statement.StatementDate,
                    DueDate = statement.DueDate,
                    AmountDue = statement.AmountDue,
                    Collector = accounts.TryGetValue(statement.AccountId, out var account) ? account.Name : ""
                })
                .ToList();
        }

        public PropertyTaxStatement? GetPropertyTaxStatement(Guid id) =>
            _statements.Get(id) as PropertyTaxStatement;

        public IReadOnlyList<PropertyTaxStatement> GetPropertyTaxStatements(Guid assetId) =>
            _statements.GetForAsset(assetId).OfType<PropertyTaxStatement>().ToList();

        public IReadOnlyList<Account> GetTaxCollectors() =>
            _accounts.GetAllAccounts()
                .Where(account => account is UntrackedAccount && account.Type == AccountType.Tax)
                .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public IReadOnlyList<Account> GetSecurableAccounts(Asset asset)
        {
            IEnumerable<Account> accounts = asset switch
            {
                PropertyAsset => _accounts.GetAllAccounts().OfType<MortgageAccount>(),
                _ => _accounts.GetAllAccounts().OfType<LoanAccount>()
            };
            return accounts.OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public Account? GetSecuredAccount(Guid assetId) =>
            SecuredAccount(_accounts.GetAllAccounts(), _assets.Get(assetId));

        /// <summary>
        /// A transportation vehicle is also an asset. Fueling, mileage, and charging keep the
        /// vehicle id; this only adds the asset row when one is missing.
        /// </summary>
        public void EnsureVehicleAssets()
        {
            var assets = _assets.GetAll();
            var linked = assets.OfType<VehicleAsset>()
                .Where(asset => asset.VehicleId != Guid.Empty)
                .Select(asset => asset.VehicleId)
                .ToHashSet();
            var names = new HashSet<string>(
                assets.Select(asset => asset.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (var vehicle in _vehicles.GetAllVehicles())
            {
                if (vehicle.Id == Guid.Empty || !linked.Add(vehicle.Id))
                    continue;

                var name = UniqueName(VehicleAssetName(vehicle), names);
                names.Add(name);
                _assets.Save(new VehicleAsset
                {
                    Name = name,
                    Category = "Vehicle",
                    VehicleId = vehicle.Id
                });
            }
        }

        public static void RenameLinkedVehicle(IAssetDataStore assets, Guid vehicleId, string name)
        {
            ArgumentNullException.ThrowIfNull(assets);
            if (vehicleId == Guid.Empty || string.IsNullOrWhiteSpace(name))
                return;

            var asset = assets.GetAll()
                .OfType<VehicleAsset>()
                .FirstOrDefault(item => item.VehicleId == vehicleId);
            if (asset is null)
                return;

            name = name.Trim();
            if (string.Equals(asset.Name, name, StringComparison.Ordinal))
                return;

            var taken = assets.GetAll().Any(item =>
                item.Id != asset.Id
                && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (taken)
                throw new InvalidOperationException($"An asset named {name} already exists.");

            asset.Name = name;
            assets.Save(asset);
        }

        public IReadOnlyList<VehicleBase> GetVehicles() =>
            _vehicles.GetAllVehicles()
                .OrderBy(vehicle => vehicle.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public IReadOnlyList<VehicleBase> GetVehiclesAvailableTo(Guid assetId) 
        {
            var taken = _assets.GetAll()
                .OfType<VehicleAsset>()
                .Where(asset => asset.Id != assetId && asset.VehicleId != Guid.Empty)
                .Select(asset => asset.VehicleId)
                .ToHashSet();
            return GetVehicles().Where(vehicle => !taken.Contains(vehicle.Id)).ToList();
        }

        private static string VehicleAssetName(VehicleBase vehicle)
        {
            if (!string.IsNullOrWhiteSpace(vehicle.Name))
                return vehicle.Name.Trim();

            var described = $"{vehicle.Year} {vehicle.Make} {vehicle.Model}".Trim();
            return described.Length == 0 ? "Vehicle" : described;
        }

        private static string UniqueName(string name, HashSet<string> names)
        {
            if (!names.Contains(name))
                return name;

            for (var suffix = 2; ; suffix++)
            {
                var candidate = $"{name} ({suffix})";
                if (!names.Contains(candidate))
                    return candidate;
            }
        }

        public VehicleBase? GetVehicle(Guid id) => _vehicles.GetVehicle(id);

        public void SaveAsset(Asset asset, Guid? securedAccountId, VehicleBase? vehicle)
        {
            ArgumentNullException.ThrowIfNull(asset);
            if (string.IsNullOrWhiteSpace(asset.Name))
                throw new InvalidOperationException("Name is required.");

            asset.Name = asset.Name.Trim();
            var duplicate = _assets.GetAll().FirstOrDefault(item =>
                item.Id != asset.Id
                && string.Equals(item.Name, asset.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
                throw new InvalidOperationException($"An asset named {asset.Name} already exists.");

            if (asset.Id != Guid.Empty
                && _assets.Get(asset.Id) is PropertyAsset
                && asset is not PropertyAsset
                && _statements.GetForAsset(asset.Id).Count > 0)
            {
                throw new InvalidOperationException(
                    "This property has tax statements. Delete those statements before changing the asset kind.");
            }

            if (asset is VehicleAsset vehicleAsset)
            {
                if (vehicle is null)
                    throw new InvalidOperationException("A vehicle asset needs a make, model, and VIN.");
                if (vehicle.Id == Guid.Empty)
                    vehicle.Id = Guid.NewGuid();
                var other = _assets.GetAll()
                    .OfType<VehicleAsset>()
                    .FirstOrDefault(item => item.Id != asset.Id && item.VehicleId == vehicle.Id);
                if (other is not null)
                    throw new InvalidOperationException($"{other.Name} is already linked to this vehicle.");
                vehicle.Name = asset.Name;
                _vehicles.UpsertVehicle(vehicle);
                vehicleAsset.VehicleId = vehicle.Id;
            }

            if (asset.Id == Guid.Empty)
                asset.Id = Guid.NewGuid();
            _assets.Save(asset);
            LinkSecurity(asset, securedAccountId);
        }

        public void SaveValuation(AssetValuation valuation)
        {
            ArgumentNullException.ThrowIfNull(valuation);
            if (valuation.AssetId == Guid.Empty)
                throw new InvalidOperationException("Choose an asset.");
            if (string.IsNullOrWhiteSpace(valuation.ValueType))
                throw new InvalidOperationException("Choose a value type.");
            valuation.ValueType = valuation.ValueType.Trim();
            valuation.Source = valuation.Source?.Trim() ?? "";
            _assets.SaveValuation(valuation);
        }

        public IReadOnlyList<PropertyAppraisal> GetPropertyAppraisals(Guid assetId) =>
            PropertyAppraisal.Group(GetValuations(assetId));

        public void SavePropertyAppraisal(PropertyAsset property, PropertyAppraisal appraisal)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(appraisal);
            if (property.Id == Guid.Empty)
                throw new InvalidOperationException("Save the property before adding a valuation.");
            if (appraisal.AsOf == default)
                throw new InvalidOperationException("Enter the valuation date.");

            appraisal.AssetId = property.Id;
            appraisal.Source = appraisal.Source?.Trim() ?? "";
            if (appraisal.Id == Guid.Empty)
                appraisal.Id = Guid.NewGuid();

            var method = property.AssessmentMethod;
            if (method == AssessmentMethod.EqualMarketValue || method == AssessmentMethod.AssessmentRatio)
                appraisal.HomesteadCap = 0m;
            if (method == AssessmentMethod.AssessmentRatio
                && (appraisal.AssessmentRatioPercent <= 0m || appraisal.AssessmentRatioPercent > 100m))
                throw new InvalidOperationException("Enter an assessment ratio greater than 0 and at most 100 percent.");
            if (method != AssessmentMethod.Manual)
            {
                appraisal.TotalMarketValue = PropertyAssessment.TotalMarket(
                    appraisal.LandMarketValue,
                    appraisal.ImprovementMarketValue);
                var ratio = method == AssessmentMethod.AssessmentRatio ? appraisal.AssessmentRatioPercent : 0m;
                appraisal.AssessedValue = PropertyAssessment.AssessedValue(
                    method,
                    appraisal.TotalMarketValue,
                    appraisal.HomesteadCap,
                    ratio);
            }

            var existing = GetValuations(property.Id);
            var group = existing.Where(row => BelongsToAppraisal(row, appraisal)).ToList();
            foreach (var (type, amount) in AppraisalComponents(appraisal))
            {
                var row = group.FirstOrDefault(item => item.ValueType == type) ?? new AssetValuation();
                row.AssetId = property.Id;
                row.AppraisalId = appraisal.Id;
                row.ValueType = type;
                row.Amount = amount;
                row.AsOf = appraisal.AsOf.Date;
                row.Source = appraisal.Source;
                row.AssessmentRatioPercent = appraisal.AssessmentRatioPercent;
                row.SourceStatementId = appraisal.SourceStatementId;
                SaveValuation(row);
                appraisal.ComponentIds[type] = row.Id;
            }
        }

        private static bool BelongsToAppraisal(AssetValuation row, PropertyAppraisal appraisal) =>
            (appraisal.Id != Guid.Empty && row.AppraisalId == appraisal.Id)
            || appraisal.ComponentIds.Values.Contains(row.Id)
            || (appraisal.SourceStatementId is Guid source
                && source != Guid.Empty
                && row.SourceStatementId == source);

        private static (string Type, decimal Amount)[] AppraisalComponents(PropertyAppraisal appraisal) =>
        [
            (ValuationTypes.LandMarket, appraisal.LandMarketValue),
            (ValuationTypes.ImprovementMarket, appraisal.ImprovementMarketValue),
            (ValuationTypes.TotalMarket, appraisal.TotalMarketValue),
            (ValuationTypes.HomesteadCap, appraisal.HomesteadCap),
            (ValuationTypes.Assessed, appraisal.AssessedValue)
        ];

        public void DeleteValuation(Guid id)
        {
            var valuation = _assets.GetAll()
                .SelectMany(asset => _assets.GetValuations(asset.Id))
                .FirstOrDefault(item => item.Id == id);
            if (valuation?.SourceStatementId is Guid)
                throw new InvalidOperationException("This value came from a tax statement. Edit that statement to change it.");
            _assets.DeleteValuation(id);
        }

        public void DeleteAsset(Guid id)
        {
            if (_statements.GetForAsset(id).Count > 0)
            {
                throw new InvalidOperationException(
                    "Delete this property's tax statements before deleting the asset.");
            }

            LinkSecurity(_assets.Get(id), null);
            _assets.Delete(id);
        }

        public Account EnsureTaxCollector(string name)
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0)
                throw new InvalidOperationException("Enter the tax collector's name.");

            var existing = _accounts.GetAllAccounts()
                .FirstOrDefault(account => string.Equals(account.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (existing is UntrackedAccount && existing.Type == AccountType.Tax)
                return existing;
            if (existing is not null)
            {
                throw new InvalidOperationException(
                    $"An account named {trimmed} already exists and is not a tax collector.");
            }

            var created = new UntrackedAccount
            {
                Name = trimmed,
                Institution = trimmed,
                AccountNumber = "",
                WebsiteUrl = "",
                Type = AccountType.Tax
            };
            _accounts.UpsertAccount(created);
            return created;
        }

        public void SavePropertyTaxStatement(PropertyAsset property, PropertyTaxStatement statement)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(statement);
            if (property.Id == Guid.Empty)
                property.Id = Guid.NewGuid();
            statement.AssetId = property.Id;
            statement.AssessmentMethod = property.AssessmentMethod;
            statement.LevyMethod = property.LevyMethod;
            if (statement.Id == Guid.Empty)
                statement.Id = Guid.NewGuid();
            statement.AmountDue = decimal.Round(
                statement.Jurisdictions.Sum(line => line.TaxAmount),
                2,
                MidpointRounding.AwayFromZero);
            if (string.IsNullOrWhiteSpace(statement.Notes))
                statement.Notes = $"{property.Name} {statement.TaxYear} property tax";

            AccountStatementValidator.EnsureValid(statement);
            SaveAsset(property, GetSecuredAccount(property.Id)?.Id, vehicle: null);
            _planning.SaveStatement(statement);
        }

        public void DeletePropertyTaxStatement(Guid id) => _planning.DeleteStatement(id);

        private void LinkSecurity(Asset? asset, Guid? accountId)
        {
            if (asset is null)
                return;

            foreach (var account in _accounts.GetAllAccounts())
            {
                switch (account)
                {
                    case LoanAccount loan when loan.SecuredAssetId == asset.Id && loan.Id != accountId:
                        loan.SecuredAssetId = null;
                        _accounts.UpsertAccount(loan);
                        break;
                    case MortgageAccount mortgage when mortgage.SecuredAssetId == asset.Id && mortgage.Id != accountId:
                        mortgage.SecuredAssetId = null;
                        _accounts.UpsertAccount(mortgage);
                        break;
                }
            }

            if (accountId is not Guid id || id == Guid.Empty)
                return;

            var chosen = _accounts.GetAllAccounts().FirstOrDefault(account => account.Id == id)
                ?? throw new InvalidOperationException("The selected loan, lease, or mortgage was not found.");
            switch (asset)
            {
                case PropertyAsset when chosen is MortgageAccount mortgage:
                    mortgage.SecuredAssetId = asset.Id;
                    _accounts.UpsertAccount(mortgage);
                    break;
                case not PropertyAsset when chosen is LoanAccount loan:
                    loan.SecuredAssetId = asset.Id;
                    _accounts.UpsertAccount(loan);
                    break;
                case PropertyAsset:
                    throw new InvalidOperationException("Link a property to a mortgage.");
                default:
                    throw new InvalidOperationException("Link this asset to a loan or lease.");
            }
        }

        private static Account? SecuredAccount(IEnumerable<Account> accounts, Asset? asset)
        {
            if (asset is null)
                return null;
            return asset switch
            {
                PropertyAsset => accounts.OfType<MortgageAccount>().FirstOrDefault(item => item.SecuredAssetId == asset.Id),
                _ => accounts.OfType<LoanAccount>().FirstOrDefault(item => item.SecuredAssetId == asset.Id)
            };
        }

        public decimal? LatestAmount(Guid assetId)
        {
            var asset = GetAsset(assetId);
            if (asset is null)
                return null;
            return ValuationPolicy.Choose(asset, GetValuations(assetId), DateTime.Today)?.Amount;
        }

        private static string LatestValue(Asset asset, IReadOnlyList<AssetValuation> values)
        {
            var chosen = ValuationPolicy.Choose(asset, values, DateTime.Today);
            if (chosen is null)
                return "";
            var kind = chosen.ValueType.ToLowerInvariant();
            var asOf = chosen.AsOf.ToString("d");
            return chosen.Figures.Count == 1
                ? $"{chosen.Amount:c2} {kind} as of {asOf}"
                : $"{chosen.Amount:c2} {kind} median of {chosen.Figures.Count} as of {asOf}";
        }
    }
}
