using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SampleReact
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class LanguageValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid( object value, ValidationContext validationContext )
        {
            var data = (IDataManager) validationContext.GetService(typeof(IDataManager));
            var lang = data.ReadLanguages();

            if( lang.FirstOrDefault( c=> c.Enum == value.ToString() ) != null )
                return ValidationResult.Success;

            string values = string.Join( ',', lang.Select( c=> c.Enum ) );
            return new ValidationResult( $"Invalid language. Must be one of the following: {values}" );
        }

    }
}
