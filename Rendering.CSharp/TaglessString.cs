using System;

namespace Rendering.CSharp.Tests
{
    public class TaglessString
    {
        public string Value { get; set; }

        public TaglessString(string rawInput)
        {
            Value = rawInput;
            while (Value.Contains("{{"))
            {
                Value = Value.Replace("{{", "{");
            }
            if(Value.EndsWith('{'))
            {
                Value = Value + "a";
            }
        }

        public override string ToString()
        {
            return String.Format("Tagless: {0}", Value);
        }
    }
}
