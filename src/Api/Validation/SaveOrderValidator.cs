using Api.Contracts;
using Core.Domain;
using FluentValidation;

namespace Api.Validation;

public sealed class SaveOrderRequestValidator : AbstractValidator<SaveOrderRequest>
{
    public SaveOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Ідентифікатор клієнта обов'язковий.")
            .MaximumLength(50).WithMessage("Ідентифікатор клієнта не може перевищувати 50 символів.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || Enum.TryParse<OrderStatus>(s, ignoreCase: true, out _))
            .WithMessage("Некоректний статус. Допустимі значення: Draft, Confirmed, Cancelled.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Замовлення повинно містити хоча б один рядок товару.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .NotEmpty().WithMessage("Ідентифікатор товару обов'язковий.");
            line.RuleFor(l => l.ProductName)
                .NotEmpty().WithMessage("Назва товару обов'язкова.")
                .MaximumLength(200).WithMessage("Назва товару не може перевищувати 200 символів.");
            line.RuleFor(l => l.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Ціна не може бути від'ємною.");
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Кількість має бути більшою за нуль.");
        });
    }
}

public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return TypedResults.BadRequest("Тіло запиту відсутнє.");

        var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}
