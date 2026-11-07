using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class Utf8TextAttribute(int maxBytes) : ValidationAttribute(
    $"{{0}} must not contain null characters or exceed {maxBytes} UTF-8 bytes.")
{
    public override bool IsValid(object? value) => value is null ||
        value is string text && !text.Contains('\0') && Encoding.UTF8.GetByteCount(text) <= maxBytes;
}
