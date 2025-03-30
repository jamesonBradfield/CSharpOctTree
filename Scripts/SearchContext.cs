using Godot;
using System.Collections.Generic;

namespace OctTreeNamespace
{
    /// <summary>
    /// Immutable context for search operations
    /// </summary>
    public record SearchContext(
        OctTreeState State,
        Vector3I SearchMin,
        Vector3I SearchMax,
        List<OctElt> Results)
    {
        public SearchContext() : this(null, Vector3I.Zero, Vector3I.Zero, new List<OctElt>()) { }
        
        // Helper method to add a result
        public SearchContext AddResult(OctElt element)
        {
            Results.Add(element);
            return this;
        }
    }
}
