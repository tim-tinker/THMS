using THMS.Domain.Finance.Income;

namespace THMS.Data.Stores
{
    public interface IIncomeDataStore
    {
        void Save(Payslip payslip);
        Payslip? Get(Guid id);
        IReadOnlyList<Payslip> GetAll();
        void Delete(Guid id);

        void Save(FormW2 form);
        FormW2? GetFormW2(Guid id);
        IReadOnlyList<FormW2> GetFormW2s();
        void DeleteFormW2(Guid id);
    }
}
