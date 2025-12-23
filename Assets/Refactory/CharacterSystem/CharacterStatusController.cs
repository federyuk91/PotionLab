using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class CharacterStatusController : MonoBehaviour
    {
        private readonly HashSet<Status> activeStatuses = new();

        public event Action<Status> StatusAdded;
        public event Action<Status> StatusRemoved;
        public event Action<Status> StatusLevelChanged;
        public event Action OnImmunity;
        public event Action OnExplosion;


        public int fireLevel = 0, algaeLevel = 0, groundLevel = 0, poisonLevel = 0;

        public bool Has(Status status)
        {
            return activeStatuses.Contains(status);
        }

        public void Add(Status status)
        {
            if (activeStatuses.Add(status))
            {
                StatusAdded?.Invoke(status);
            }

        }

        public void Remove(Status status)
        {

            if (activeStatuses.Remove(status))
            {
                StatusRemoved?.Invoke(status);
                ResetLevel(status);
            }


        }
        public void Increase(Status status)
        {
            switch (status)
            {
                case Status.Burned:
                    fireLevel = Mathf.Min(fireLevel + 1, 3);
                    break;
                case Status.Grounded:
                    groundLevel = Mathf.Min(groundLevel + 1, 3);
                    break;
                case Status.Algae:
                    algaeLevel = Mathf.Min(algaeLevel + 1, 3);
                    break;
                case Status.Poisoned:
                    poisonLevel = 3;
                    break;
            }
            Add(status);
            StatusLevelChanged?.Invoke(status);
        }

        public void Decrease(Status status)
        {
            switch (status)
            {
                case Status.Burned:
                    fireLevel = Mathf.Max(fireLevel - 1, 0);
                    if (fireLevel == 0)
                        Remove(Status.Burned);
                    break;
                case Status.Grounded:
                    groundLevel = Mathf.Max(groundLevel - 1, 0);
                    if (groundLevel == 0)
                        Remove(Status.Grounded);
                    break;
                case Status.Algae:
                    algaeLevel = Mathf.Max(algaeLevel - 1, 0);
                    if (algaeLevel == 0)
                        Remove(Status.Algae);
                    break;
            }

            StatusLevelChanged?.Invoke(status);
        }

        private void ResetLevel(Status status)
        {
            switch (status)
            {
                case Status.Algae: algaeLevel = 0; break;
                case Status.Grounded: groundLevel = 0; break;
                case Status.Burned: fireLevel = 0; break;
            }
            StatusLevelChanged?.Invoke(status);
        }
        public void TriggerImmunity()
        {
            OnImmunity?.Invoke();
        }

        public void TriggerExplosion()
        {
            Remove(Status.Freezed);
            Remove(Status.Poisoned);
            Remove(Status.Burned);
            OnExplosion?.Invoke();
        }

        public void Clear()
        {
            
            Remove(Status.Wet);
            Remove(Status.Freezed);
            Remove(Status.Poisoned);
            Remove(Status.Burned);
            Remove(Status.Grounded);
            Remove(Status.Algae);
            Remove(Status.Grass);

            activeStatuses.Clear();
        }

        public HashSet<Status> GetAllStatuses()
        {
            return activeStatuses;
        }

    }
}
