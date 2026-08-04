using System;
using System.IO;
using UnityEngine;

namespace ProgressSystem
{
    public class JsonProgressRepository : MonoBehaviour, IProgressRepository
    {
        [SerializeField] private string fileName = "player-progress.json";

        private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        public PlayerProgress Load()
        {
            if (!File.Exists(SavePath))
            {
                return new PlayerProgress();
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                PlayerProgress progress = JsonUtility.FromJson<PlayerProgress>(json);
                return progress ?? new PlayerProgress();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SAVE: Cannot read progress file at '{SavePath}'. A new progress file will be used. {exception.Message}", this);
                return new PlayerProgress();
            }
        }

        public void Save(PlayerProgress progress)
        {
            if (progress == null)
            {
                Debug.LogWarning("SAVE: Cannot save a null PlayerProgress instance.", this);
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(progress, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SAVE: Cannot write progress file at '{SavePath}'. {exception.Message}", this);
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SAVE: Cannot delete progress file at '{SavePath}'. {exception.Message}", this);
            }
        }
    }
}
