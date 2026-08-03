using System;

namespace OneStep.Networking
{
    public sealed class SessionCodeDuelInvitationService : IDuelInvitationService
    {
        private const string Prefix = "onestep-duel:";

        public string CreateInvitation(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                throw new ArgumentException("A session join code is required.", nameof(joinCode));
            }

            return Prefix + joinCode.Trim().ToUpperInvariant();
        }

        public bool TryParseInvitation(string invitation, out string joinCode)
        {
            joinCode = string.Empty;
            if (string.IsNullOrWhiteSpace(invitation))
            {
                return false;
            }

            var trimmed = invitation.Trim();
            if (trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[Prefix.Length..];
            }

            if (trimmed.Length < 4 || trimmed.Length > 16)
            {
                return false;
            }

            joinCode = trimmed.ToUpperInvariant();
            return true;
        }
    }
}
