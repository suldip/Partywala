namespace PartyClap.Services
{
    /// <summary>
    /// Shared authentication scheme names.
    /// </summary>
    public static class AuthConstants
    {
        /// <summary>
        /// Temporary cookie scheme that holds the external provider (Google)
        /// identity between the OAuth challenge and our callback.
        /// </summary>
        public const string ExternalScheme = "External";
    }
}
