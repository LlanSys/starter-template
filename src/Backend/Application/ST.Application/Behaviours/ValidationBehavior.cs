using ST.SharedKernel.Dtos.Common;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using ValidationException = FluentValidation.ValidationException;

namespace ST.Application.Behaviours;

public class ValidationBehavior<TRequest, TResponse>
    (IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!_validators.Any())
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Extract the generic type of TResponse (e.g., the 'T' in ApiResponse<T>)
            var responseType = typeof(TResponse);

            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(AppResponse<>))
            {
                var resultType = responseType.GetGenericArguments()[0];
                var validationErrorDict = failures
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup
                    .ToList());

                var method = typeof(AppResponses)
                    .GetMethods()
                    .SingleOrDefault(method =>
                        method.Name == nameof(AppResponses.ValidationFailure) &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters() is [{ ParameterType: var parameterType }] &&
                        parameterType == typeof(Dictionary<string, List<string>>));

                if (method != null)
                {
                    return (TResponse)method.MakeGenericMethod(resultType).Invoke(null, [validationErrorDict])!;
                }
            }

            // Fallback: If for some reason reflection fails, or it's not a standard response
            throw new ValidationException(failures);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
