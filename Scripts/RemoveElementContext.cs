using Godot;

namespace OctTreeNamespace
{
    /// <summary>
    /// Immutable context for element removal operations
    /// </summary>
    public record RemoveElementContext(
        int ElementIndex,
        OctTreeState State,
        bool ElementRemoved = false,
        int RemovedFromNodeIndex = 0)
    {
        // Helper methods for updating state
        public RemoveElementContext WithState(OctTreeState state) =>
            this with { State = state };
            
        public RemoveElementContext WithElementRemoved(int nodeIndex) =>
            this with { ElementRemoved = true, RemovedFromNodeIndex = nodeIndex };
    }
}
