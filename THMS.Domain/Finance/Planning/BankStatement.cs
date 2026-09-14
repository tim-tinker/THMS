namespace THMS.Domain.Finance.Planning
{
    public class BankStatement : AccountStatement
    {
        public decimal StatementBalance { get; set; }

        public override StatementType Type => StatementType.Bank;
    }
}
