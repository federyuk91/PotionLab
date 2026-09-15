namespace FedericoTools.SpriteToolBox
{
    public sealed class DuplicateLayerCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int sourceLayerIndex;
        private readonly string duplicateName;
        private SpriteLayerTrackSnapshot snapshot;

        public DuplicateLayerCommand(SpriteDocument document, int sourceLayerIndex, string duplicateName)
        {
            this.document = document;
            this.sourceLayerIndex = sourceLayerIndex;
            this.duplicateName = duplicateName;
        }

        public long EstimatedMemoryBytes => snapshot == null ? 0L : snapshot.EstimateMemoryBytes();

        public void Execute()
        {
            if (snapshot == null)
            {
                snapshot = document.CreateDuplicateLayerTrackSnapshotForCommand(sourceLayerIndex, duplicateName);
            }

            document.InsertLayerTrackForCommand(sourceLayerIndex + 1, snapshot);
        }

        public void Undo()
        {
            document.RemoveLayerTrackForCommand(sourceLayerIndex + 1);
        }
    }
}
