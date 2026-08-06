using System;
using UnityEngine;

namespace InspectorValidation
{
    public enum RequiredReferenceSeverity
    {
        Warning,
        Error
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredInspectorReferenceAttribute : PropertyAttribute
    {
        public RequiredReferenceSeverity Severity { get; }
        public string Message { get; }

        public RequiredInspectorReferenceAttribute(
            RequiredReferenceSeverity severity = RequiredReferenceSeverity.Warning,
            string message = null)
        {
            Severity = severity;
            Message = message;
        }
    }
}
