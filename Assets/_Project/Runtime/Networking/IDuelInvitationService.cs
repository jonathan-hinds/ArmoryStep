namespace OneStep.Networking
{
    public interface IDuelInvitationService
    {
        string CreateInvitation(string joinCode);
        bool TryParseInvitation(string invitation, out string joinCode);
    }
}
