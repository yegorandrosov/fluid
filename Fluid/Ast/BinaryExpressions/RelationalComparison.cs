using Fluid.SourceGeneration;

namespace Fluid.Ast.BinaryExpressions
{
    /// <summary>
    /// Shared source generation for the relational operators, whose handling of a type mismatch depends on
    /// <see cref="TemplateOptions.LenientComparisons"/> and must read the same in generated code as it does in
    /// <see cref="GreaterThanBinaryExpression"/> and <see cref="LowerThanBinaryExpression"/>.
    /// </summary>
    internal static class RelationalComparison
    {
        /// <summary>
        /// Emits the branch taken when the operands are not of the same type.
        /// </summary>
        public static void WriteMismatch(SourceGenerationContext context, string message)
        {
            context.WriteLine($"if ({context.ContextName}.Options.LenientComparisons)");
            context.WriteLine("{");
            using (context.Indent())
            {
                context.WriteLine("return new BinaryExpressionFluidValue(NilValue.Instance, false);");
            }
            context.WriteLine("}");
            context.WriteLine($"throw new LiquidException(\"{message}\");");
        }
    }
}
