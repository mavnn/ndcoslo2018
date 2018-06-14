using System.Collections.Generic;
using Xunit;
using static Rendering.Implementation;
using Result =
    Microsoft.FSharp.Core.FSharpResult<
        string,
        Rendering.Implementation.RenderingError>;

namespace Rendering.CSharp.Tests
{
    public class Tests
    {
        [Fact]
        public void StringWithoutTagDoesNotChange()
        {
            var template = "No tags";
            var model = new Dictionary<string, string>();
            var result = renderText(model, template);
            Assert.Equal(Result.NewOk(template), result);
        }

        [Fact]
        public void TagsAreReplaced()
        {
            var template = "a {{ TAG }}";
            var model = new Dictionary<string, string>{
                { "TAG", "REPLACED" }
            };
            var result = renderText(model, template);
            Assert.Equal(Result.NewOk("a REPLACED"), result);
        }
    }
}
