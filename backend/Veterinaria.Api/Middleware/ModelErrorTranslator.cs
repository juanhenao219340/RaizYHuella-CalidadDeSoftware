using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Veterinaria.Api.Middleware;

// Shared by the InvalidModelStateResponseFactory (Program.cs) and by controllers that validate manually
// (e.g. AuthController), so framework text never reaches the interface (RNF-IDI-01, D-12).
public static class ModelErrorTranslator
{
    private const string BodyKey = "request";

    // Single construction of the 400 validation body, so every validation path has the same shape and traceId.
    // Field errors go to "errors" in camelCase (R14-2). Body-level errors go to "detail", never to "errors" (R14-3, D-4).
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

    // Builds the field error dictionary (keys in camelCase, see ToCamelCaseKey). Body-level entries ("" and "request")
    // are never returned here. They are returned in bodyMessage, which is set only when no field error exists:
    //   - Body was sent but could not be deserialized (e.g. {"type":0} with an invalid enum): MVC adds a misleading
    //     "request" required error. When any field has errors, body-level entries are dropped (precedence kept).
    //   - Only body-level entries exist: the body is really missing or unreadable, so bodyMessage carries it.
    // When only body-level errors exist, the dictionary is empty ({}), not omitted (D-1).
    // The same normalization applies to login (AuthController), which validates manually.
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

    // Normalizes a ModelState key to the camelCase used in "errors" (R14-2, CA-REG-03).
    // Rules: strip the JSON path prefix "$." (e.g. "$.type" becomes "type"), then camel-case each "."-separated segment.
    // Keys already in camelCase do not change. The "$." prefix is never kept.
    private static string ToCamelCaseKey(string key)
    {
        var path = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

        return string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

    // Keeps DataAnnotations messages as they are (already Spanish). Only framework-generated text is replaced.
    // Framework messages covered by the explicit patterns below:
    //   - RequiredAttribute default "The {0} field is required." applied by MVC to the body parameter named "request"
    //     when the model is null (not routed through ModelBindingMessageProvider, so the accessors in Program.cs do not cover it).
    // Any NEW framework message in English that reaches this method requires adding its own explicit pattern here.
    public static string Translate(string key, ModelError error)
    {
        // 1. Input formatter / binding exception (bad JSON, unknown enum value).
        if (error.Exception is not null)
        {
            return "El formato de uno de los datos no es válido.";
        }

        // 2. System.Text.Json conversion text reported without an exception (not covered by ModelBindingMessageProvider).
        if (new[] { "JSON value could not be converted", "Path: $", "LineNumber:", "BytePositionInLine" }
                .Any(marker => error.ErrorMessage.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return "El formato de uno de los datos no es válido.";
        }

        // 3. RequiredAttribute default message for the MVC body parameter ("request") or the empty key.
        if (error.ErrorMessage.Contains("field is required", StringComparison.OrdinalIgnoreCase))
        {
            return key is "" or "request" || error.ErrorMessage.Contains("request field", StringComparison.OrdinalIgnoreCase)
                ? "El cuerpo de la solicitud es obligatorio."
                : "El campo es obligatorio.";
        }

        // 4. Any other message (DataAnnotations, already Spanish) is preserved literally.
        return error.ErrorMessage;
    }
}
