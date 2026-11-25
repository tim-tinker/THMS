namespace THMS.Domain.Assets
{
    [Flags]
    public enum AppraisedAssetKind
    {
        None = 0,
        Property = 1,
        Vehicle = 2,
        Personal = 4
    }

    /// <summary>
    /// A place that publishes or prepares a valuation, such as a county appraisal district or Kelley Blue Book.
    /// </summary>
    public class ValuationSource : BaseDomainModel
    {
        public string Name { get; set; } = "";
        public string WebsiteUrl { get; set; } = "";
        public AppraisedAssetKind Kinds { get; set; }

        public bool Appraises(AppraisedAssetKind kind) =>
            kind != AppraisedAssetKind.None && (Kinds & kind) == kind;

        public string AppraisesLabel
        {
            get
            {
                var parts = new List<string>();
                if (Kinds.HasFlag(AppraisedAssetKind.Property))
                    parts.Add("Property");
                if (Kinds.HasFlag(AppraisedAssetKind.Vehicle))
                    parts.Add("Vehicles");
                if (Kinds.HasFlag(AppraisedAssetKind.Personal))
                    parts.Add("Personal property");
                return string.Join(", ", parts);
            }
        }

        public static AppraisedAssetKind KindOf(Asset asset) => asset switch
        {
            PropertyAsset => AppraisedAssetKind.Property,
            VehicleAsset => AppraisedAssetKind.Vehicle,
            PersonalAsset => AppraisedAssetKind.Personal,
            _ => AppraisedAssetKind.None
        };

        public Uri? Website
        {
            get
            {
                if (!TryNormalizeWebsite(WebsiteUrl, out var normalized) || normalized.Length == 0)
                    return null;
                return new Uri(normalized);
            }
        }

        public static bool TryNormalizeWebsite(string? text, out string normalized)
        {
            normalized = "";
            var url = text?.Trim() ?? "";
            if (url.Length == 0)
                return true;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && !Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
                return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;
            if (string.IsNullOrWhiteSpace(uri.Host) || !uri.Host.Contains('.'))
                return false;

            normalized = uri.ToString();
            return true;
        }
    }
}
