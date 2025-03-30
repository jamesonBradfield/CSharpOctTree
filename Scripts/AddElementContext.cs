using Godot;

namespace OctTreeNamespace
{
    /// <summary>
    /// Immutable context for element addition operations
    /// </summary>
    public record AddElementContext(
        OctElt Element,
        int ElementIndex,
        OctTreeState State,
        bool NodeFound = false,
        int FinalNodeIndex = 0)
    {
        // Helper methods for updating state
        public AddElementContext WithState(OctTreeState state) =>
            this with { State = state };
            
        public AddElementContext WithNodeFound(int finalNodeIndex) =>
            this with { NodeFound = true, FinalNodeIndex = finalNodeIndex };
    }
}
