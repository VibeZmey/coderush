using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace Api.Validation;

public sealed class RequestValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().Single();
        try
        {
            ValidateRequest(request);
        }
        catch (ValidationException error)
        {
            return ValueTask.FromResult<object?>(Results.Problem(
                statusCode: StatusCodes.Status400BadRequest, title: "Invalid request", detail: error.Message));
        }

        return next(context);
    }

    private static void ValidateRequest(object request)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        foreach (var property in request.GetType().GetProperties())
        {
            var value = property.GetValue(request);
            if (value is string || value is null)
                continue;
            if (value is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is null)
                        throw new ValidationException($"{property.Name} must not contain null items.");
                    ValidateRequest(item);
                }
            }
            else if (property.PropertyType.IsClass)
                ValidateRequest(value);
        }
    }
}
