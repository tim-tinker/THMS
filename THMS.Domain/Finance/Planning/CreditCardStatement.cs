namespace THMS.Domain.Finance.Planning
{
    public class CreditCardStatement : AccountStatement
    {
        public decimal StatementBalance { get; set; }
        public decimal InterestCharged { get; set; }
        public decimal Fees { get; set; }

        public List<PromotionalBalance> Promotions { get; set; } = [];

        public override StatementType Type => StatementType.CreditCard;
    }
}
