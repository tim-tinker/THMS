namespace THMS.Domain.Finance.Planning
{
    public enum PromoType
    {
        /// <summary>
        /// Pay any amounts toward the remaining balance as long as the full amount is paid by the deadline.
        /// </summary>
        LumpSum,

        /// <summary>
        /// Pay the remaining balance in equal installments over the months remaining until the deadline.
        /// </summary>
        EqualPayments
    }

    public static class PromoTypeDisplay
    {
        public static string Name(PromoType type) => type switch
        {
            PromoType.LumpSum => "Lump sum",
            PromoType.EqualPayments => "Equal payments",
            _ => type.ToString()
        };
    }
}
