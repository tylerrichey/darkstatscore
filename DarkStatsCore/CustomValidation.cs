using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using DarkStatsCore.Data;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ValidDarkStatsUrlAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var url = new Uri(value as string);
        return DataSource.GetForUrl(url.ToString()).Test();
    }
}