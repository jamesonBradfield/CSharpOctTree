using System.Collections.Immutable;

namespace OctTreeNamespace
{
    /// <summary>
    /// Helper methods for operations on OctNodes
    /// </summary>
    public static class NodeOperations
    {
        public static bool IsLeaf(OctNode node) => node.count != -1;
        
        public static bool IsInternalNode(OctNode node) => node.count == -1;
        
        public static bool HasChildren(OctNode node) => node.first_child != -1;
        
        public static OctNode CreateLeafNode(int elementNodeIndex, int count) =>
            new OctNode(elementNodeIndex, count);
            
        public static OctNode CreateInternalNode(int firstChildIndex) =>
            new OctNode(firstChildIndex, -1);
            
        public static OctNode CreateEmptyLeaf() =>
            new OctNode(-1, 0);
            
        /// <summary>
        /// Update a node in an immutable list
        /// </summary>
        public static ImmutableList<OctNode> UpdateNode(
            ImmutableList<OctNode> nodes, 
            int nodeIndex,
            OctNode newNode) =>
            nodes.SetItem(nodeIndex, newNode);
            
        /// <summary>
        /// Add multiple child nodes to the list
        /// </summary>
        public static ImmutableList<OctNode> AddChildNodes(
            ImmutableList<OctNode> nodes,
            int count)
        {
            var builder = nodes.ToBuilder();
            for (int i = 0; i < count; i++)
            {
                builder.Add(new OctNode(-1, -1));
            }
            return builder.ToImmutable();
        }
    }
}
