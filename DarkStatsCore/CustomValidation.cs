using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using DarkStatsCore.Data;

public class ValidDarkStatsUrlAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var url = new Uri(value as string);
        int count = 0;
        try
        {
            count = Scraper.ScrapeData(url.ToString()).Count();
        }
        catch
        {
            return new ValidationResult("Could not connect to the specified URL, please try again.");
        }
        if (count == 0)
        {
            return new ValidationResult("URL returned no results when scraping for data, please try again.");
        }
        return ValidationResult.Success;
    }
}