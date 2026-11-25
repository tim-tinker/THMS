using THMS.Data.Stores.SQLite;
using THMS.Domain.Assets;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class ValuationSourceTests
    {
        [Test]
        public void Website_AcceptsAHostWithoutAScheme()
        {
            Assert.That(ValuationSource.TryNormalizeWebsite("www.fbcad.org", out var url), Is.True);
            Assert.That(url, Does.StartWith("https://www.fbcad.org"));
            Assert.That(ValuationSource.TryNormalizeWebsite("", out var blank), Is.True);
            Assert.That(blank, Is.EqualTo(""));
            Assert.That(ValuationSource.TryNormalizeWebsite("not a website", out _), Is.False);
        }

        [Test]
        public void Sqlite_RoundTripsASourceAndTheAssetsItAppraises()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-sources-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteAssetDataStore(path);
                store.SaveSource(new ValuationSource
                {
                    Name = "Local appraiser",
                    WebsiteUrl = "https://example.com/request",
                    Kinds = AppraisedAssetKind.Property | AppraisedAssetKind.Personal
                });

                var saved = new SQLiteAssetDataStore(path).GetSources().Single();
                Assert.That(saved.Name, Is.EqualTo("Local appraiser"));
                Assert.That(saved.WebsiteUrl, Is.EqualTo("https://example.com/request"));
                Assert.That(saved.Appraises(AppraisedAssetKind.Property), Is.True);
                Assert.That(saved.Appraises(AppraisedAssetKind.Personal), Is.True);
                Assert.That(saved.Appraises(AppraisedAssetKind.Vehicle), Is.False);
                Assert.That(saved.AppraisesLabel, Is.EqualTo("Property, Personal property"));

                new SQLiteAssetDataStore(path).DeleteSource(saved.Id);
                Assert.That(new SQLiteAssetDataStore(path).GetSources(), Is.Empty);
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }
}
