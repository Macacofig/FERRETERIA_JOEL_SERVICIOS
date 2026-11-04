namespace FERRETERIA__Joel.Dominio.Utilidades
{
    /// <summary>
    /// Convierte un importe en bolivianos a su representación en literal,
    /// usada en el comprobante de venta. Ejemplo: 202,50 produce
    /// "doscientos dos bolivianos con 50/100".
    /// </summary>
    public static class NumeroALetras
    {
        private const string UnidadSingular = "boliviano";
        private const string UnidadPlural = "bolivianos";

        private static readonly string[] Unidades =
        {
            "", "un", "dos", "tres", "cuatro", "cinco", "seis", "siete",
            "ocho", "nueve"
        };

        private static readonly string[] Especiales =
        {
            "diez", "once", "doce", "trece", "catorce", "quince"
        };

        private static readonly string[] Dieces =
        {
            "dieciséis", "diecisiete", "dieciocho", "diecinueve"
        };

        private static readonly string[] Decenas =
        {
            "", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta",
            "setenta", "ochenta", "noventa"
        };

        private static readonly string[] Cientos =
        {
            "", "ciento", "doscientos", "trescientos", "cuatrocientos",
            "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
        };

        public static string EnLetras(decimal monto)
        {
            decimal redondeado = Math.Round(
                monto,
                2,
                MidpointRounding.AwayFromZero);

            if (redondeado == 0)
            {
                return $"cero {UnidadPlural} con 00/100";
            }

            string prefijo = redondeado < 0 ? "menos " : string.Empty;
            decimal absoluto = Math.Abs(redondeado);

            long entero = (long)Math.Truncate(absoluto);
            int centavos = (int)Math.Round(
                (absoluto - entero) * 100M,
                0,
                MidpointRounding.AwayFromZero);

            string numero = entero == 1 ? "un" : ConvertirNumero(entero);
            string moneda = entero == 1 ? UnidadSingular : UnidadPlural;

            return $"{prefijo}{numero} {moneda} con {centavos:00}/100";
        }

        private static string ConvertirNumero(long n)
        {
            if (n == 0)
            {
                return "cero";
            }

            long millones = n / 1_000_000;
            long resto = n % 1_000_000;
            string? resultado = null;

            if (millones > 0)
            {
                resultado = millones == 1
                    ? "un millón"
                    : $"{ConvertirNumero(millones)} millones";
            }

            long miles = resto / 1000;
            resto %= 1000;

            if (miles > 0)
            {
                string milesEnPalabras = miles == 1
                    ? "mil"
                    : $"{ConvertirNumero(miles)} mil";

                resultado = resultado is null
                    ? milesEnPalabras
                    : $"{resultado} {milesEnPalabras}";
            }

            if (resto > 0)
            {
                string restoSinCentenas = ConvertirNumeroMenor(resto);

                resultado = resultado is null
                    ? restoSinCentenas
                    : $"{resultado} {restoSinCentenas}";
            }

            return resultado ?? "cero";
        }

        private static string ConvertirNumeroMenor(long n)
        {
            if (n is < 1 or > 999)
            {
                return n switch
                {
                    0 => "",
                    _ => ConvertirDecenas(n)
                };
            }

            long cientos = n / 100;
            long resto = n % 100;

            string cientosEnPalabras = cientos == 1 && resto == 0
                ? "cien"
                : Cientos[cientos];

            string restoEnPalabras = ConvertirDecenas(resto);

            if (resto == 0)
            {
                return cientosEnPalabras;
            }

            return cientos == 0
                ? restoEnPalabras
                : $"{cientosEnPalabras} {restoEnPalabras}";
        }

        private static string ConvertirDecenas(long n)
        {
            if (n is >= 1 and <= 9)
            {
                return Unidades[n];
            }

            if (n is >= 10 and <= 15)
            {
                return Especiales[n - 10];
            }

            if (n is >= 16 and <= 19)
            {
                return Dieces[n - 16];
            }

            if (n is >= 21 and <= 29)
            {
                return n switch
                {
                    21 => "veintiún",
                    22 => "veintidós",
                    23 => "veintitrés",
                    26 => "veintiséis",
                    _ => $"veinti{Unidades[n % 10]}"
                };
            }

            long unidad = n % 10;

            if (unidad == 0)
            {
                return Decenas[n / 10];
            }

            return unidad == 1
                ? $"{Decenas[n / 10]} y un"
                : $"{Decenas[n / 10]} y {Unidades[unidad]}";
        }
    }
}
