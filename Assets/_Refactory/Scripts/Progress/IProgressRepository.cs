namespace ProgressSystem
{
    public interface IProgressRepository
    {
        PlayerProgress Load();
        void Save(PlayerProgress progress);
        void Delete();
    }
}
