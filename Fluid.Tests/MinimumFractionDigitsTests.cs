using System;
using Xunit;

namespace Fluid.Tests
{
    /// <summary>
    /// Covers <see cref="TemplateOptions.MinimumFractionDigits"/>: how many fraction digits an output
    /// statement pads a number to when it has a fractional part.
    /// </summary>
    public class MinimumFractionDigitsTests
    {
        private static string Render(string source, int? minimumFractionDigits = null)
        {
            var options = new TemplateOptions();
            if (minimumFractionDigits.HasValue)
            {
                options.MinimumFractionDigits = minimumFractionDigits.Value;
            }

            var parser = new FluidParser();
            Assert.True(parser.TryParse(source, out var template, out var error), error);

            return template.Render(new TemplateContext(options));
        }

        [Theory]
        [InlineData("{{ 20.00 }}", "20.0")]
        [InlineData("{{ 20.5 }}", "20.5")]
        [InlineData("{{ 0.125 }}", "0.125")]
        [InlineData("{{ 18 }}", "18")]
        public void DefaultsToTheReferenceRendering(string source, string expected)
        {
            Assert.Equal(expected, Render(source));
            Assert.Equal(expected, Render(source, 1));
        }

        [Theory]
        [InlineData("{{ 20.00 }}", "20.00")]
        [InlineData("{{ 20.5 }}", "20.50")]
        [InlineData("{{ 20.50 }}", "20.50")]
        public void PadsAnAmountToTheRequestedDigits(string source, string expected)
        {
            Assert.Equal(expected, Render(source, 2));
        }

        [Theory]
        [InlineData("{{ 18 }}", "18")]
        [InlineData("{{ 0 }}", "0")]
        [InlineData("{{ 1200 }}", "1200")]
        public void LeavesAWholeNumberAlone(string source, string expected)
        {
            // A number with no fractional part is a count, not an amount.
            Assert.Equal(expected, Render(source, 2));
        }

        [Fact]
        public void NeverDropsADigitToMeetTheMinimum()
        {
            // Padding adds zeros; it must not round 0.125 to 0.13 to land on two digits.
            Assert.Equal("0.125", Render("{{ 0.125 }}", 2));
            Assert.Equal("1.0005", Render("{{ 1.0005 }}", 2));
        }

        [Fact]
        public void AppliesToAComputedValue()
        {
            Assert.Equal("41.00", Render("{{ 20.5 | plus: 20.5 }}", 2));
            Assert.Equal("6.50", Render("{{ 13.00 | divided_by: 2.0 }}", 2));
        }

        [Fact]
        public void DoesNotAffectAStringifiedNumber()
        {
            // Filters that take the string form are unchanged: only the output statement pads.
            Assert.Equal("20.00", Render("{{ 20.00 | append: '' }}", 2));
        }

        [Fact]
        public void RejectsADigitCountADecimalCannotCarry()
        {
            var options = new TemplateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => options.MinimumFractionDigits = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MinimumFractionDigits = 29);
        }
    }
}
