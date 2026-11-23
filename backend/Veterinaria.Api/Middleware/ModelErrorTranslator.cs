using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Veterinaria.Api.Middleware;



public static class ModelErrorTranslator
{
    private const string BodyKey = "request";



    public static ValidationProblemDetails CreateValidationProblem(ModelStateDictionary modelState, HttpContext httpContext)
    {
        var fieldErrors = ToErrorDictionary(modelState, out var bodyMessage);

        var problem = new ValidationProblemDetails(fieldErrors)
        {
            Title = "Los datos enviados no son válidos.",
            Status = StatusCodes.Status400BadRequest,
            Detail = bodyMessage
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problem;
    }








    public static IDictionary<string, string[]> ToErrorDictionary(ModelStateDictionary modelState, out string? bodyMessage)
    {
        var translated = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => Translate(entry.Key, error))
                    .ToArray());

        static bool IsBodyKey(string key) => key is "" or BodyKey;

        var bodyMessages = translated
            .Where(entry => IsBodyKey(entry.Key))
            .SelectMany(entry => entry.Value)
            .Distinct()
            .ToArray();

        var fields = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var entry in translated.Where(entry => !IsBodyKey(entry.Key)))
        {
            var key = ToCamelCaseKey(entry.Key);

            if (!fields.TryGetValue(key, out var messages))
            {
                messages = [];
                fields[key] = messages;
            }

            foreach (var message in entry.Value)
            {
                if (!messages.Contains(message))
                {
                    messages.Add(message);
                }
            }
        }

        bodyMessage = fields.Count == 0 && bodyMessages.Length > 0
            ? string.Join(' ', bodyMessages)
            : null;

        return fields.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
    }




    private static string ToCamelCaseKey(string key)
    {
        var path = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

        return string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }






    public static string Translate(string key, ModelError error)
    {

        if (error.Exception is not null)
        {
            return "El formato de uno de los datos no es válido.";
        }


        if (new[] { "JSON value could not be converted", "Path: $", "LineNumber:", "BytePositionInLine" }
                .Any(marker => error.ErrorMessage.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return "El formato de uno de los datos no es válido.";
        }


        if (error.ErrorMessage.Contains("field is required", StringComparison.OrdinalIgnoreCase))
        {
            return key is "" or "request" || error.ErrorMessage.Contains("request field", StringComparison.OrdinalIgnoreCase)
                ? "El cuerpo de la solicitud es obligatorio."
                : "El campo es obligatorio.";
        }


        return error.ErrorMessage;
    }
}
