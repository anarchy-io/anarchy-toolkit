using Anarchy.Toolkit.Core.Extension;
using AwesomeAssertions.Execution;
using Newtonsoft.Json;
using System.Collections;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Testing.Extension.Assertions;

public static class DictionaryRegexAssertionExtensions
{
    /// <summary>
    ///     Asserts that the <paramref name="actual" /> dictionary contains at least the keys and values
    ///     described by the <paramref name="expected" /> dictionary, comparing nested dictionaries and
    ///     sequences recursively. When an expected value is a <see cref="Regex" />, the corresponding
    ///     actual value must be a string that matches that regular expression. Extra keys in
    ///     <paramref name="actual" /> and extra items in sequences are ignored.
    /// </summary>
    /// <param name="actual">
    ///     The actual dictionary to verify. It may contain additional entries beyond those described in
    ///     <paramref name="expected" />.
    /// </param>
    /// <param name="expected">
    ///     The expected dictionary whose entries define the minimum required structure and values.
    ///     Values may include nested dictionaries, sequences, or <see cref="Regex" /> instances.
    /// </param>
    /// <param name="because">
    ///     A formatted phrase explaining why the assertion is needed. If the phrase does not start with
    ///     the word <c>because</c>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    ///     Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public static void ShouldContainDictionaryWithRegex(
        this IDictionary<string, object?> actual,
        IDictionary<string, object?> expected,
        string because = "",
        params object[] becauseArgs)
    {
        using var scope = new AssertionScope();
        var assertionChain = AssertionChain.GetOrCreate().BecauseOf(because, becauseArgs);
        CompareDictionariesWithRegex(actual, expected, "$", assertionChain);
    }

    private static void CompareDictionariesWithRegex(
        IDictionary<string, object?> actual,
        IDictionary<string, object?> expected,
        string path,
        AssertionChain assertionChain)
    {
        if (actual.Count < expected.Count)
        {
            assertionChain.FailWith(
                "Dictionary item count mismatch at path {0}: expected at least {1} items but found {2}{reason}.",
                path, expected.Count, actual.Count);
        }

        foreach (var expectedEntry in expected)
        {
            var key = expectedEntry.Key;
            var expectedValue = expectedEntry.Value;
            var currentPath = $"{path}.{key}";

            if (!actual.TryGetValue(key, out var actualValue))
            {
                assertionChain.FailWith(
                    "Missing key {0} at path {1}{reason}.",
                    key, currentPath);

                continue;
            }

            AssertValuesEqualWithRegex(expectedValue, actualValue, currentPath, assertionChain);
        }
    }

    private static void AssertValuesEqualWithRegex(
        object? expected,
        object? actual,
        string path,
        AssertionChain assertionChain)
    {
        if (ReferenceEquals(expected, actual))
        {
            return;
        }

        if (expected is null || actual is null)
        {
            assertionChain.FailWith(
                "Value mismatch at path {0}: expected {1} but found {2}{reason}.",
                path, expected?.ToJson(formatting: Formatting.Indented) ?? "null",
                actual?.ToJson(formatting: Formatting.Indented) ?? "null");

            return;
        }

        if (expected is Regex regex)
        {
            if (actual is not string actualString)
            {
                assertionChain.FailWith(
                    "Type mismatch at path {0}: expected a string matching regex {1} but found value of type {2}{reason}.",
                    path, regex, actual.GetType().Name);

                return;
            }

            if (!regex.IsMatch(actualString))
            {
                assertionChain.FailWith(
                    "Regex mismatch at path {0}: pattern {1} did not match value {2}{reason}.",
                    path, regex, actualString);
            }

            return;
        }

        if (TryGetStringKeyDictionary(expected, out var expectedDict) &&
            TryGetStringKeyDictionary(actual, out var actualDict))
        {
            CompareDictionariesWithRegex(actualDict, expectedDict, path, assertionChain);
            return;
        }

        if (TryGetEnumerable(expected, out var expectedList) &&
            TryGetEnumerable(actual, out var actualList))
        {
            if (actualList.Count < expectedList.Count)
            {
                assertionChain.FailWith(
                    "Sequence length mismatch at path {0}: expected at least {1} items but found {2}{reason}.",
                    path, expectedList.Count, actualList.Count);

                return;
            }

            var usedActualIndexes = new HashSet<int>();
            for (var i = 0; i < expectedList.Count; i++)
            {
                var expectedItem = expectedList[i];
                var matchedIndex = FindMatchingItemIndex(expectedItem, actualList, usedActualIndexes);
                if (matchedIndex is null)
                {
                    assertionChain.FailWith(
                        "Missing matching sequence item at path {0}[{1}]: expected item {2} was not found in actual sequence{reason}.",
                        path,
                        i,
                        expectedItem?.ToJson(formatting: Formatting.Indented) ?? "null");

                    continue;
                }

                usedActualIndexes.Add(matchedIndex.Value);
            }

            return;
        }

        if (!Equals(expected, actual))
        {
            assertionChain.FailWith(
                "Value mismatch at path {0}: expected {1} but found {2}{reason}.",
                path, expected.ToJson(formatting: Formatting.Indented), actual.ToJson(formatting: Formatting.Indented));
        }
    }

    private static bool TryGetStringKeyDictionary(object value, out IDictionary<string, object?> dict)
    {
        if (value is IDictionary<string, object?> typed)
        {
            dict = typed;
            return true;
        }

        if (value is IDictionary untyped)
        {
            var result = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in untyped)
            {
                if (entry.Key is not string key)
                {
                    dict = null!;
                    return false;
                }

                result[key] = entry.Value;
            }

            dict = result;
            return true;
        }

        dict = null!;
        return false;
    }

    private static int? FindMatchingItemIndex(
        object? expectedItem,
        IReadOnlyList<object?> actualList,
        ISet<int> usedActualIndexes)
    {
        for (var i = 0; i < actualList.Count; i++)
        {
            if (usedActualIndexes.Contains(i))
            {
                continue;
            }

            if (AreValuesMatching(expectedItem, actualList[i]))
            {
                return i;
            }
        }

        return null;
    }

    private static bool AreValuesMatching(object? expected, object? actual)
    {
        if (ReferenceEquals(expected, actual))
        {
            return true;
        }

        if (expected is null || actual is null)
        {
            return false;
        }

        if (expected is Regex regex)
        {
            return actual is string actualString && regex.IsMatch(actualString);
        }

        if (TryGetStringKeyDictionary(expected, out var expectedDict) &&
            TryGetStringKeyDictionary(actual, out var actualDict))
        {
            foreach (var expectedEntry in expectedDict)
            {
                if (!actualDict.TryGetValue(expectedEntry.Key, out var actualEntryValue))
                {
                    return false;
                }

                if (!AreValuesMatching(expectedEntry.Value, actualEntryValue))
                {
                    return false;
                }
            }

            return true;
        }

        if (TryGetEnumerable(expected, out var expectedList) &&
            TryGetEnumerable(actual, out var actualList))
        {
            if (actualList.Count < expectedList.Count)
            {
                return false;
            }

            var usedActualIndexes = new HashSet<int>();
            foreach (var expectedItem in expectedList)
            {
                var matchedIndex = FindMatchingItemIndex(expectedItem, actualList, usedActualIndexes);
                if (matchedIndex is null)
                {
                    return false;
                }

                usedActualIndexes.Add(matchedIndex.Value);
            }

            return true;
        }

        return Equals(expected, actual);
    }

    private static bool TryGetEnumerable(object value, out IReadOnlyList<object?> list)
    {
        if (value is string)
        {
            list = null!;
            return false;
        }

        if (value is IEnumerable enumerable)
        {
            var result = new List<object?>();
            foreach (var item in enumerable)
            {
                result.Add(item);
            }

            list = result;
            return true;
        }

        list = null!;
        return false;
    }
}
