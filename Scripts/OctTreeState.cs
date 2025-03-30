using Godot;
using System.Collections.Immutable;

namespace OctTreeNamespace
{
    /// <summary>
    /// Immutable state container for OctTree data
    /// </summary>
    public record OctTreeState(
        ImmutableList<OctElt> Elements,
        ImmutableList<OctEltNode> ElementNodes,
        ImmutableList<OctNode> Nodes,
        Vector3I BoundsSize,
        int MaxDepth)
    {
        public OctTreeState WithElements(ImmutableList<OctElt> elements) =>
            this with { Elements = elements };

        public OctTreeState WithElementNodes(ImmutableList<OctEltNode> elementNodes) =>
            this with { ElementNodes = elementNodes };

        public OctTreeState WithNodes(ImmutableList<OctNode> nodes) =>
            this with { Nodes = nodes };

        public OctTreeState WithUpdatedCollections(
            ImmutableList<OctElt> elements,
            ImmutableList<OctEltNode> elementNodes,
            ImmutableList<OctNode> nodes) =>
            this with
            {
                Elements = elements,
                ElementNodes = elementNodes,
                Nodes = nodes
            };
    }
}
