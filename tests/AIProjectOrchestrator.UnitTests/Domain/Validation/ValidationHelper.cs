using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public static class ValidationHelper
    {
        public static IList<ValidationResult> ValidateObject(object instance, bool validateAllProperties = true)
        {
            var validationContext = new ValidationContext(instance, null, null);
            var validationResults = new List<ValidationResult>();
            
            Validator.TryValidateObject(instance, validationContext, validationResults, validateAllProperties);
            
            return validationResults;
        }

        public static bool IsValid(object instance)
        {
            return ValidateObject(instance).Count == 0;
        }
    }
}