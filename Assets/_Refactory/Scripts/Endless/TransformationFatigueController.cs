using CharacterSystem;
using InspectorValidation;
using UnityEngine;

namespace EndlessMode
{
    public sealed class TransformationFatigueController : MonoBehaviour
    {
        private const int TransformationManaCost = 2;

        [SerializeField, RequiredInspectorReference] private TransformationManager transformationManager;
        [SerializeField, RequiredInspectorReference] private CharacterStats characterStats;

        private void OnEnable()
        {
            if (transformationManager == null || characterStats == null)
            {
                Debug.LogError(
                    $"{name}: assign TransformationManager and CharacterStats for Transformation Fatigue.",
                    this);
                return;
            }

            transformationManager.OnTransformation += HandleTransformation;
        }

        private void OnDisable()
        {
            if (transformationManager != null)
            {
                transformationManager.OnTransformation -= HandleTransformation;
            }
        }

        private void HandleTransformation(CharacterType fromType, CharacterType toType)
        {
            LevelSettings settings = transformationManager.lightController != null
                ? transformationManager.lightController.LevelSettings
                : null;

            if (settings == null || !settings.EndlessTransformationFatigue || toType == CharacterType.Mage)
            {
                return;
            }

            characterStats.LoseMana(TransformationManaCost);
        }
    }
}
