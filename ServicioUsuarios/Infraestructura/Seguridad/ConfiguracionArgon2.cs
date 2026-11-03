namespace FERRETERIA__Joel.Infraestructura.Seguridad
{
    /// <summary>
    /// Parámetros de Argon2id (variante híbrida) usados por el hasheador.
    /// Valores alineados a la recomendación mínima de OWASP para Argon2id.
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
