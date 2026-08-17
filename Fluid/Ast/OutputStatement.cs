using System.Text.Encodings.Web;
using Fluid.Values;
using Fluid.SourceGeneration;

namespace Fluid.Ast
{
    public sealed class OutputStatement : Statement, ISourceable
    {
        public OutputStatement(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        public IReadOnlyList<FilterExpression> Filters { get; }

        public override ValueTask<Completion> WriteToAsync(IFluidOutput output, TextEncoder encoder, TemplateContext context)
        {
            static async ValueTask<Completion> Awaited(
                ValueTask<FluidValue> t,
                IFluidOutput o,
                TextEncoder enc,
                TemplateContext ctx)
            {
                var value = await t;
                await Write(value, o, enc, ctx);
                return Completion.Normal;
            }

            context.IncrementSteps();

            var task = Expression.EvaluateAsync(context);
            if (task.IsCompletedSuccessfully)
            {
                var valueTask = Write(task.Result, output, encoder, context);

                if (valueTask.IsCompletedSuccessfully)
                {
                    return Statement.NormalCompletion;
                }

                return AwaitedWriteTo(valueTask);

                static async ValueTask<Completion> AwaitedWriteTo(ValueTask t)
                {
                    await t;
                    return Completion.Normal;
                }
            }

            return Awaited(task, output, encoder, context);
        }

        /// <summary>
        /// Writes a value the way <c>{{ }}</c> renders it. Only the output statement consults
        /// <see cref="TemplateOptions.MinimumFractionDigits"/>: it is about how a template prints an amount,
        /// not about the counters <c>increment</c> and <c>cycle</c> emit.
        /// </summary>
        public static ValueTask Write(FluidValue value, IFluidOutput output, TextEncoder encoder, TemplateContext context)
        {
            return value is NumberValue number
                ? number.WriteToAsync(output, encoder, context.CultureInfo, context.Options.MinimumFractionDigits)
                : value.WriteToAsync(output, encoder, context.CultureInfo);
        }

        protected internal override Statement Accept(AstVisitor visitor) => visitor.VisitOutputStatement(this);

        public void WriteTo(SourceGenerationContext context)
        {
            var exprMethod = context.GetExpressionMethodName(Expression);

            context.WriteLine($"{context.ContextName}.IncrementSteps();");

            context.WriteLine($"var task = {exprMethod}({context.ContextName});");
            context.WriteLine("if (task.IsCompletedSuccessfully)");
            context.WriteLine("{");
            using (context.Indent())
            {
                context.WriteLine($"var valueTask = OutputStatement.Write(task.Result, {context.WriterName}, {context.EncoderName}, {context.ContextName});");
                context.WriteLine("if (valueTask.IsCompletedSuccessfully)");
                context.WriteLine("{");
                using (context.Indent())
                {
                    context.WriteLine("return Completion.Normal;");
                }
                context.WriteLine("}");

                context.WriteLine("return await AwaitedWriteTo(valueTask);");
                context.WriteLine();
                context.WriteLine("static async ValueTask<Completion> AwaitedWriteTo(ValueTask t)");
                context.WriteLine("{");
                using (context.Indent())
                {
                    context.WriteLine("await t;");
                    context.WriteLine("return Completion.Normal;");
                }
                context.WriteLine("}");
            }
            context.WriteLine("}");

            context.WriteLine($"return await Awaited(task, {context.WriterName}, {context.EncoderName}, {context.ContextName});");
            context.WriteLine();
            context.WriteLine("static async ValueTask<Completion> Awaited(ValueTask<FluidValue> t, IFluidOutput w, TextEncoder enc, TemplateContext ctx)");
            context.WriteLine("{");
            using (context.Indent())
            {
                context.WriteLine("var value = await t;");
                context.WriteLine("await OutputStatement.Write(value, w, enc, ctx);");
                context.WriteLine("return Completion.Normal;");
            }
            context.WriteLine("}");
        }
    }
}
