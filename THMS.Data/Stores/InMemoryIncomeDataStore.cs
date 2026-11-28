using THMS.Domain.Finance.Income;

namespace THMS.Data.Stores
{
    public class InMemoryIncomeDataStore : IIncomeDataStore
    {
        private readonly List<Payslip> _payslips = [];
        private readonly List<FormW2> _forms = [];

        public void Save(Payslip payslip)
        {
            ArgumentNullException.ThrowIfNull(payslip);
            if (payslip.Id == Guid.Empty)
                payslip.Id = Guid.NewGuid();
            payslip.ModifiedOn = DateTime.UtcNow;

            var index = _payslips.FindIndex(item => item.Id == payslip.Id);
            if (index >= 0)
                _payslips[index] = payslip;
            else
                _payslips.Add(payslip);
            FinanceDataRevision.NoteChanged();
        }

        public Payslip? Get(Guid id) => _payslips.FirstOrDefault(item => item.Id == id);

        public IReadOnlyList<Payslip> GetAll() =>
            _payslips.OrderByDescending(item => item.PayDate).ThenByDescending(item => item.CreatedOn).ToList();

        public void Delete(Guid id)
        {
            _payslips.RemoveAll(item => item.Id == id);
            FinanceDataRevision.NoteChanged();
        }

        public void Save(FormW2 form)
        {
            ArgumentNullException.ThrowIfNull(form);
            if (form.Id == Guid.Empty)
                form.Id = Guid.NewGuid();
            form.ModifiedOn = DateTime.UtcNow;

            var index = _forms.FindIndex(item => item.Id == form.Id);
            if (index >= 0)
                _forms[index] = form;
            else
                _forms.Add(form);
            FinanceDataRevision.NoteChanged();
        }

        public FormW2? GetFormW2(Guid id) => _forms.FirstOrDefault(item => item.Id == id);

        public IReadOnlyList<FormW2> GetFormW2s() =>
            _forms
                .OrderByDescending(item => item.TaxYear)
                .ThenBy(item => item.Employer, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => item.CreatedOn)
                .ToList();

        public void DeleteFormW2(Guid id)
        {
            _forms.RemoveAll(item => item.Id == id);
            FinanceDataRevision.NoteChanged();
        }
    }
}
