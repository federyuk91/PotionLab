using System;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    public interface ISpriteToolboxNotifications
    {
        void Show(string message);
    }

    internal sealed class SpriteToolboxNotificationService : ISpriteToolboxNotifications
    {
        private readonly EditorWindow owner;

        public SpriteToolboxNotificationService(EditorWindow owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            owner.ShowNotification(new GUIContent(message));
        }
    }
}
