using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Validation.Validators.Common;


public abstract class Validator<T> : AbstractValidator<T>
{
    public Func<object, string, Task<IEnumerable<string>>> ValidateValue =>
        async (model, propertyName) =>
        {
            if (model is not T validModel) return [];

            var result = await ValidateAsync(
                ValidationContext<T>.CreateWithOptions(validModel,
                x => x.IncludeProperties(propertyName))).ConfigureAwait(false);

            return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
        };
}
