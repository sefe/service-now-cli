using CommandLine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceNowCLI.Core.Arguments
{
    public static class CommandLineArgumentsValidator
    {
        /// <summary>
        /// Validates that all properties marked with [Option(Required = true)] have a non-null,
        /// non-empty (and non-whitespace) value. Throws a single ArgumentException listing all
        /// invalid fields if any are missing.
        /// </summary>
        public static void ValidateRequiredFields<T>(T options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var missingFields = new List<string>();

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var optionAttribute = property.GetCustomAttribute<OptionAttribute>();

                if (optionAttribute is null || !optionAttribute.Required)
                {
                    continue;
                }

                var value = property.GetValue(options);

                if (IsEmpty(value))
                {
                    var optionName = !string.IsNullOrEmpty(optionAttribute.LongName)
                        ? $"--{optionAttribute.LongName}"
                        : property.Name;

                    missingFields.Add($"{property.Name} ({optionName})");
                }
            }

            if (missingFields.Count > 0)
            {
                throw new ArgumentException(
                    $"The following required argument(s) were not provided or are empty: {string.Join(", ", missingFields)}");
            }
        }

        private static bool IsEmpty(object value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str);
            }

            return false;
        }
    }
}