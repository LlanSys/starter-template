using ST.SharedKernel.Features.Shared.Lookups.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.Shared.Lookups.Validators;



public class GetLookupRequestValidator : Validator<GetLookupRequest>
{
    public GetLookupRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.LookupType)
            .IsInEnum()
            .WithMessage("Invalid lookup type requested.");
    }

}
