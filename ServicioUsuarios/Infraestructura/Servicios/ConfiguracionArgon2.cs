namespace FERRETERIA__Joel.Infraestructura.Servicios
{
    /// <summary>
    /// ParÃ¡metros de Argon2id (variante hÃ­brida) usados por el hasheador.
    /// Valores alineados a la recomendaciÃ³n mÃ­nima de OWASP para Argon2id.
    /// </summary>
    public static class ConfiguracionArgon2
    {
        public const int MemoryCost = 19456;
        public const int TimeCost = 2;
        public const int Lanes = 1;
        public const int Threads = 1;
        public const int HashLength = 32;
        public const int SaltLength = 16;
    }
}

