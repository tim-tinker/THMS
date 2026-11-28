namespace THMS.Domain.Household
{
    /// <summary>
    /// One member of the household. Birth year is 0 until it is saved.
    /// </summary>
    public sealed class HouseholdPerson : BaseDomainModel
    {
        public string Name { get; set; } = "";
        public int BirthYear { get; set; }

        public override string ToString() => Name;
    }
}
