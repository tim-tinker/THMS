using THMS.Domain.Finance.Income;

namespace THMS.Logic.Finance.Income
{
    /// <summary>
    /// Chooses the payslips and W-2s for one person and employer.
    /// A document saved before it had a person stays with the household's only person.
    /// A payslip saved before it had an employer stays with the only named employer.
    /// </summary>
    public static class PersonIncome
    {
        public static bool BelongsTo(Guid documentPersonId, Guid personId, int personCount) =>
            documentPersonId == personId
            || (documentPersonId == Guid.Empty && personCount == 1);

        public static (IReadOnlyList<Payslip> Payslips, IReadOnlyList<FormW2> Forms) ForSelection(
            IEnumerable<Payslip> payslips,
            IEnumerable<FormW2> forms,
            Guid? personId,
            int personCount,
            string? employer)
        {
            ArgumentNullException.ThrowIfNull(payslips);
            ArgumentNullException.ThrowIfNull(forms);

            var slips = payslips
                .Where(slip => personId is null || BelongsTo(slip.PersonId, personId.Value, personCount))
                .ToList();
            var filed = forms
                .Where(form => personId is null || BelongsTo(form.PersonId, personId.Value, personCount))
                .ToList();
            if (personId is null || string.IsNullOrWhiteSpace(employer))
                return (slips, filed);

            var named = Employers(filed, slips);
            var onlyEmployer = named.Count == 1 ? named[0] : null;
            slips = slips
                .Where(slip => Same(slip.Employer, employer)
                    || (onlyEmployer is not null
                        && Same(onlyEmployer, employer)
                        && string.IsNullOrWhiteSpace(slip.Employer)))
                .ToList();
            filed = filed.Where(form => Same(form.Employer, employer)).ToList();
            return (slips, filed);
        }

        public static IReadOnlyList<string> Employers(IEnumerable<FormW2> forms, IEnumerable<Payslip> payslips)
        {
            var display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var form in forms)
                Remember(display, form.Employer);
            foreach (var slip in payslips)
                Remember(display, slip.Employer);
            return display.Values.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void Remember(Dictionary<string, string> display, string employer)
        {
            var trimmed = employer.Trim();
            if (trimmed.Length == 0 || display.ContainsKey(trimmed))
                return;
            display[trimmed] = trimmed;
        }

        private static bool Same(string left, string right) =>
            string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
