using DotNet.Globbing;

namespace BitCheck.Application
{
    /// <summary>
    /// A single compiled ignore pattern parsed from a .bitcheckignore file or --ignore-pattern option.
    /// </summary>
    /// <param name="Pattern">The original pattern text (without the leading '!' for negations).</param>
    /// <param name="IsNegation">Whether this pattern re-includes a previously ignored name.</param>
    /// <param name="CompiledGlob">The compiled glob used to match a file or directory basename.</param>
    public sealed record IgnoreRule(string Pattern, bool IsNegation, Glob CompiledGlob);

    /// <summary>
    /// An ordered set of ignore rules matched against file/directory basenames, using git-style
    /// last-match-wins precedence with support for '!' negation.
    /// </summary>
    public sealed class IgnoreRuleSet
    {
        private static readonly IgnoreRuleSet Empty = new(Array.Empty<IgnoreRule>());

        private readonly IReadOnlyList<IgnoreRule> _rules;

        private IgnoreRuleSet(IReadOnlyList<IgnoreRule> rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Parses ignore pattern lines, skipping blank lines and comments (lines starting with '#').
        /// Lines starting with '!' are treated as negation rules.
        /// </summary>
        /// <param name="lines">The raw pattern lines to parse.</param>
        /// <returns>An <see cref="IgnoreRuleSet"/> containing the parsed rules.</returns>
        public static IgnoreRuleSet Parse(IEnumerable<string> lines)
        {
            var rules = new List<IgnoreRule>();
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var isNegation = line.StartsWith('!');
                var pattern = isNegation ? line[1..] : line;
                if (pattern.Length == 0)
                {
                    continue;
                }

                rules.Add(new IgnoreRule(pattern, isNegation, Glob.Parse(pattern)));
            }

            return rules.Count == 0 ? Empty : new IgnoreRuleSet(rules);
        }

        /// <summary>
        /// Loads the .bitcheckignore file from the specified directory, if present.
        /// </summary>
        /// <param name="directoryPath">The directory to look for a .bitcheckignore file in.</param>
        /// <returns>The parsed rule set, or an empty set if no ignore file exists.</returns>
        public static IgnoreRuleSet Load(string directoryPath)
        {
            var ignoreFilePath = Path.Combine(directoryPath, BitCheckConstants.IgnoreFileName);
            if (!File.Exists(ignoreFilePath))
            {
                return Empty;
            }

            return Parse(File.ReadAllLines(ignoreFilePath));
        }

        /// <summary>
        /// Combines a parent rule set with a child (nested directory) rule set. Child rules are
        /// evaluated after parent rules, so a nested .bitcheckignore can override inherited rules.
        /// </summary>
        /// <param name="parent">The inherited rule set.</param>
        /// <param name="child">The rule set to apply on top of the parent.</param>
        /// <returns>A combined rule set.</returns>
        public static IgnoreRuleSet Combine(IgnoreRuleSet parent, IgnoreRuleSet child)
        {
            if (child._rules.Count == 0)
            {
                return parent;
            }

            if (parent._rules.Count == 0)
            {
                return child;
            }

            var combined = new List<IgnoreRule>(parent._rules.Count + child._rules.Count);
            combined.AddRange(parent._rules);
            combined.AddRange(child._rules);
            return new IgnoreRuleSet(combined);
        }

        /// <summary>
        /// Determines whether the specified file or directory name is ignored, using last-match-wins
        /// precedence: the most recently added matching rule decides the outcome.
        /// </summary>
        /// <param name="name">The file or directory basename to test.</param>
        /// <returns><c>true</c> if the name is ignored, otherwise <c>false</c>.</returns>
        public bool IsIgnored(string name)
        {
            var ignored = false;
            foreach (var rule in _rules)
            {
                if (rule.CompiledGlob.IsMatch(name))
                {
                    ignored = !rule.IsNegation;
                }
            }

            return ignored;
        }
    }
}
