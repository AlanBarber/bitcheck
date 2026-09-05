using BitCheck.Application;

namespace BitCheck.Tests
{
    [TestClass]
    public class IgnoreRuleSetTests
    {
        [TestMethod]
        public void BlankLinesAndComments_AreSkipped()
        {
            var rules = IgnoreRuleSet.Parse(new[] { "", "   ", "# a comment", "*.par2" });

            Assert.IsTrue(rules.IsIgnored("archive.par2"));
            Assert.IsFalse(rules.IsIgnored("#not-a-comment"));
        }

        [TestMethod]
        public void WildcardPattern_MatchesBasename()
        {
            var rules = IgnoreRuleSet.Parse(new[] { "*.par2" });

            Assert.IsTrue(rules.IsIgnored("data.par2"));
            Assert.IsFalse(rules.IsIgnored("data.txt"));
        }

        [TestMethod]
        public void QuestionMarkPattern_MatchesSingleCharacter()
        {
            var rules = IgnoreRuleSet.Parse(new[] { "file?.log" });

            Assert.IsTrue(rules.IsIgnored("file1.log"));
            Assert.IsFalse(rules.IsIgnored("file12.log"));
        }

        [TestMethod]
        public void Negation_OverridesEarlierMatch_LastRuleWins()
        {
            var rules = IgnoreRuleSet.Parse(new[] { "*.par2", "!keep.par2" });

            Assert.IsTrue(rules.IsIgnored("archive.par2"));
            Assert.IsFalse(rules.IsIgnored("keep.par2"));
        }

        [TestMethod]
        public void LaterPattern_OverridesEarlierNegation()
        {
            var rules = IgnoreRuleSet.Parse(new[] { "!keep.par2", "keep.par2" });

            Assert.IsTrue(rules.IsIgnored("keep.par2"));
        }

        [TestMethod]
        public void Combine_ChildRulesEvaluatedAfterParent_CanOverrideViaNegation()
        {
            var parent = IgnoreRuleSet.Parse(new[] { "*.par2" });
            var child = IgnoreRuleSet.Parse(new[] { "!special.par2" });

            var combined = IgnoreRuleSet.Combine(parent, child);

            Assert.IsTrue(combined.IsIgnored("archive.par2"));
            Assert.IsFalse(combined.IsIgnored("special.par2"));
        }

        [TestMethod]
        public void Combine_WithEmptyChild_ReturnsParentRules()
        {
            var parent = IgnoreRuleSet.Parse(new[] { "*.par2" });
            var child = IgnoreRuleSet.Parse(Array.Empty<string>());

            var combined = IgnoreRuleSet.Combine(parent, child);

            Assert.IsTrue(combined.IsIgnored("archive.par2"));
        }

        [TestMethod]
        public void Load_ReturnsEmptySet_WhenIgnoreFileMissing()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"bitcheck_ignore_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(directory);
            try
            {
                var rules = IgnoreRuleSet.Load(directory);
                Assert.IsFalse(rules.IsIgnored("anything.par2"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void Load_ParsesIgnoreFileFromDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"bitcheck_ignore_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllLines(Path.Combine(directory, BitCheckConstants.IgnoreFileName), new[] { "*.par2" });

                var rules = IgnoreRuleSet.Load(directory);

                Assert.IsTrue(rules.IsIgnored("archive.par2"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
