using System;
using UnityEngine;

namespace InspectorValidation
{
    public enum Severity
    {
        Warning,
        Error
    }

    public enum ResolveMode
    {
        None,
        Local,
        SceneSingleton
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredInspectorReferenceAttribute : PropertyAttribute
    {
        public ResolveMode ResolveMode { get; }
        public Severity Severity { get; }
        public string Message { get; }

        public RequiredInspectorReferenceAttribute(
            ResolveMode resolveMode = ResolveMode.None,
            Severity severity = Severity.Warning,
            string message = null)
        {
            ResolveMode = resolveMode;
            Severity = severity;
            Message = message;
        }

        public RequiredInspectorReferenceAttribute(
            Severity severity,
            string message = null)
        {
            ResolveMode = ResolveMode.None;
            Severity = severity;
            Message = message;
        }
    }
}
