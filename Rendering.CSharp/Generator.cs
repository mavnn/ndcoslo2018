using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;

namespace Rendering.CSharp.Tests
{
    public class Generator
    {
        public static Arbitrary<TaglessString> TaglessString()
        {
            return Arb.From<NonNull<string>>()
                .Convert(nonNull => new TaglessString(nonNull.Get),
                         tagless => NonNull<string>.NewNonNull(tagless.Value));
        }

        public static Arbitrary<IDictionary<string, string>> Model()
        {
            var tagOpeningChars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var tagOtherChars =
                tagOpeningChars + "0123456789";

            Func<char, char[], string> merge = (first, rest) =>
            {
                var target = new char[rest.Length + 1];
                target[0] = first;
                Array.Copy(rest, 0, target, 1, rest.Length);
                return new string(target);
            };

            var tagGen =
                from opening in
                    Gen.Elements(tagOpeningChars.ToCharArray())
                from rest in
                    Gen.ArrayOf(Gen.Elements(tagOtherChars.ToCharArray()))
                from replacement in
                    Arb.Generate<NonNull<string>>()
                select
                    new KeyValuePair<string, string>(merge(opening, rest),
                                                     replacement.Get);

            var modelGen =
                Gen.NonEmptyListOf(tagGen)
                .Select(tags =>
                        new Dictionary<string, string>(
                            tags.GroupBy(kv => kv.Key)
                                .Select(g => g.First()))
                                    as IDictionary<string, string>);

            return Arb.From(modelGen);
        }
    }
}
