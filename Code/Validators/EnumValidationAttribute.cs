using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SampleReact
{
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class EnumValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid( object value, ValidationContext validationContext )
        {
            Type enumType = value.GetType();
            bool valid = Enum.IsDefined(enumType, value);
            return valid
                ? ValidationResult.Success
                : new ValidationResult( String.Format( "{0} is not a valid value for type {1}", value, enumType.Name ) );
        }
    }
}
