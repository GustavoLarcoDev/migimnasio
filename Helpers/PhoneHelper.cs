namespace Gimnasio.Helpers;

/// <summary>
/// Utilidad para normalizar numeros de telefono al formato ecuatoriano (+593).
/// Ecuador usa el prefijo +593 y numeros moviles de 10 digitos (0XX XXX XXXX).
/// El formato normalizado es ideal para WhatsApp Business API.
/// </summary>
public static class PhoneHelper
{
    /// <summary>
    /// Normaliza un numero de telefono ecuatoriano al formato internacional +593XXXXXXXXX.
    /// Ejemplos:
    ///   "0991715903"     → "+593991715903"
    ///   "991715903"      → "+593991715903"
    ///   "+593991715903"  → "+593991715903" (ya normalizado)
    ///   "593991715903"   → "+593991715903"
    ///   "(099) 171-5903" → "+593991715903"
    /// </summary>
    public static string NormalizeEcuador(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return phone ?? "";

        // Eliminar todo excepto digitos y el signo +
        var cleaned = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Si ya tiene formato +593XXXXXXXXX, retornar limpio
        if (cleaned.StartsWith("+593") && cleaned.Length == 13)
            return cleaned;

        // Extraer solo digitos para normalizar
        var digits = new string(cleaned.Where(char.IsDigit).ToArray());

        // 593XXXXXXXXX (12 digitos con codigo de pais sin +)
        if (digits.StartsWith("593") && digits.Length == 12)
            return "+" + digits;

        // 0XXXXXXXXX (10 digitos con 0 inicial — formato local ecuatoriano)
        if (digits.StartsWith("0") && digits.Length == 10)
            return "+593" + digits.Substring(1);

        // XXXXXXXXX (9 digitos sin 0 inicial — movil ecuatoriano)
        if (digits.Length == 9 && (digits.StartsWith("9") || digits.StartsWith("2") || digits.StartsWith("3") || digits.StartsWith("4") || digits.StartsWith("5") || digits.StartsWith("6") || digits.StartsWith("7")))
            return "+593" + digits;

        // Si no coincide con ningun patron ecuatoriano, retornar limpio
        return cleaned;
    }

    /// <summary>
    /// Normaliza un input de telefono para busqueda en la base de datos.
    /// El usuario puede escribir en cualquier formato (0991715903, +593991715903, 991715903)
    /// y el sistema lo convierte al formato almacenado (+593XXXXXXXXX) para hacer match.
    /// Se usa en el flujo de login por telefono.
    /// </summary>
    public static string NormalizeForLookup(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? "";

        // Si contiene @ es un email, no un telefono
        if (input.Contains('@'))
            return input;

        // Normalizar al mismo formato que se guarda en la BD
        return NormalizeEcuador(input);
    }
}
