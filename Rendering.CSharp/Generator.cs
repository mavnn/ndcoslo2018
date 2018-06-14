using System;
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
    }
}
