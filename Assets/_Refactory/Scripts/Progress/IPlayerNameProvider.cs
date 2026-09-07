namespace ProgressSystem
{
    public interface IPlayerNameProvider
    {
        bool TryGetPlayerName(out string playerName, out string failureReason);
    }
}
