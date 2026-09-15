using System;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    /// <summary>Editor-only bridge between the LightPad canvas and Unity's active Scene View.</summary>
    internal sealed class LightPadSceneViewNavigationController : IDisposable
    {
        private const float MouseRotationDegreesPerPixel = 0.18f;
        private readonly SpriteToolboxLightPadController lightPadController;
        private readonly Action repaint;
        private bool isNavigating;
        private bool moveForward;
        private bool moveBackward;
        private bool moveLeft;
        private bool moveRight;
        private bool moveDown;
        private bool moveUp;
        private Vector2 previousMousePosition;
        private double lastUpdateTime;
        private float currentSpeed;

        public LightPadSceneViewNavigationController(SpriteToolboxLightPadController lightPadController, Action repaint)
        {
            this.lightPadController = lightPadController ?? throw new ArgumentNullException(nameof(lightPadController));
            this.repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
        }

        public bool HandleInput(Event currentEvent, bool isPointerInViewport)
        {
            if (currentEvent == null || lightPadController.Settings.SourceType != SpriteLightPadSourceType.SceneView)
            {
                StopNavigation();
                return false;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && isPointerInViewport)
            {
                isNavigating = true;
                previousMousePosition = currentEvent.mousePosition;
                lastUpdateTime = EditorApplication.timeSinceStartup;
                currentSpeed = 0f;
                EditorApplication.update += UpdateNavigation;
                currentEvent.Use();
                return true;
            }

            if (!isNavigating)
            {
                return false;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1)
            {
                StopNavigation();
                currentEvent.Use();
                return true;
            }

            if (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag)
            {
                RotateFromMouseDelta(currentEvent.mousePosition - previousMousePosition);
                previousMousePosition = currentEvent.mousePosition;
                currentEvent.Use();
                return true;
            }

            if (currentEvent.type == EventType.KeyDown || currentEvent.type == EventType.KeyUp)
            {
                bool isPressed = currentEvent.type == EventType.KeyDown;
                if (SetMovementKey(currentEvent.keyCode, isPressed))
                {
                    currentEvent.Use();
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            StopNavigation();
        }

        public void CancelNavigation()
        {
            StopNavigation();
        }

        private void UpdateNavigation()
        {
            if (!isNavigating)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - lastUpdateTime), 0f, 0.1f);
            lastUpdateTime = now;
            if (deltaTime <= 0f || !HasMovementInput())
            {
                return;
            }

            SceneView sceneView = lightPadController.CapturedSceneView;
            if (sceneView == null)
            {
                StopNavigation();
                return;
            }

            SceneView.CameraSettings cameraSettings = sceneView.cameraSettings;
            float targetSpeed = cameraSettings.speed;
            if (cameraSettings.accelerationEnabled || cameraSettings.easingEnabled)
            {
                float easingDuration = Mathf.Max(0.1f, cameraSettings.easingDuration);
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, targetSpeed * deltaTime / easingDuration);
            }
            else
            {
                currentSpeed = targetSpeed;
            }

            Vector3 movement = GetMovementDirection(sceneView.in2DMode);
            if (movement.sqrMagnitude <= 0f)
            {
                return;
            }

            if (sceneView.in2DMode)
            {
                sceneView.pivot += new Vector3(movement.x, movement.y, 0f) * currentSpeed * deltaTime;
            }
            else
            {
                Vector3 cameraRelativeMovement = sceneView.camera.transform.TransformDirection(new Vector3(movement.x, 0f, movement.z));
                Vector3 worldMovement = (cameraRelativeMovement + Vector3.up * movement.y).normalized;
                sceneView.pivot += worldMovement * currentSpeed * deltaTime;
            }

            RefreshSceneReference(sceneView);
        }

        private void RotateFromMouseDelta(Vector2 mouseDelta)
        {
            SceneView sceneView = lightPadController.CapturedSceneView;
            if (sceneView == null || sceneView.in2DMode || mouseDelta == Vector2.zero)
            {
                return;
            }

            Quaternion yaw = Quaternion.AngleAxis(mouseDelta.x * MouseRotationDegreesPerPixel, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(mouseDelta.y * MouseRotationDegreesPerPixel, Vector3.right);
            sceneView.rotation = yaw * sceneView.rotation * pitch;
            RefreshSceneReference(sceneView);
        }

        private bool SetMovementKey(KeyCode keyCode, bool isPressed)
        {
            switch (keyCode)
            {
                case KeyCode.W: moveForward = isPressed; return true;
                case KeyCode.S: moveBackward = isPressed; return true;
                case KeyCode.A: moveLeft = isPressed; return true;
                case KeyCode.D: moveRight = isPressed; return true;
                case KeyCode.Q: moveDown = isPressed; return true;
                case KeyCode.E: moveUp = isPressed; return true;
                default: return false;
            }
        }

        private Vector3 GetMovementDirection(bool is2D)
        {
            float horizontal = (moveRight ? 1f : 0f) - (moveLeft ? 1f : 0f);
            float vertical = (moveUp ? 1f : 0f) - (moveDown ? 1f : 0f);
            if (is2D)
            {
                float vertical2D = (moveForward ? 1f : 0f) - (moveBackward ? 1f : 0f)
                    + (moveUp ? 1f : 0f) - (moveDown ? 1f : 0f);
                return new Vector3(horizontal, vertical2D, 0f);
            }

            float depth = (moveForward ? 1f : 0f) - (moveBackward ? 1f : 0f);
            return new Vector3(horizontal, vertical, depth);
        }

        private bool HasMovementInput()
        {
            return moveForward || moveBackward || moveLeft || moveRight || moveDown || moveUp;
        }

        private void RefreshSceneReference(SceneView sceneView)
        {
            sceneView.Repaint();
            lightPadController.RefreshSceneView(sceneView);
            repaint();
        }

        private void StopNavigation()
        {
            if (!isNavigating)
            {
                return;
            }

            isNavigating = false;
            moveForward = false;
            moveBackward = false;
            moveLeft = false;
            moveRight = false;
            moveDown = false;
            moveUp = false;
            EditorApplication.update -= UpdateNavigation;
        }
    }
}
