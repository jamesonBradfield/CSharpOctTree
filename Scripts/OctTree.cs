using Godot;
using System.Collections.Generic;

namespace OctTreeNamespace
{
    /// <summary>
    /// </summary>
    public class OctTree
    {
        private const int MAX_ELEMENTS_PER_NODE = 8;
        // maybe make loose?
        // private const int NODE_PADDING = 2
        private Vector3I BoundsSize;

        // Global element storage
        public List<OctElement> allElements = new List<OctElement>();

        // Global linked list nodes
        public List<OctElementNode> allElementNodes = new List<OctElementNode>();

        // Global tree nodes
        public List<OctNode> allNodes = new List<OctNode>();

        public OctNode root_node;

        public OctTree(Vector3I BoundsSize)
        {
            root_node = new OctNode(0, -1);
            this.BoundsSize = BoundsSize;
            allNodes.Add(root_node);
        }

        public void AddElement(OctElement element)
        {
            // Add element to global storage
            int elementIndex = allElements.Count;
            allElements.Add(element);

            // Start recursive traversal from root with the entire bounds
            OctNode? targetNode = GetOctantRecursive(0, element, Vector3I.Zero, BoundsSize);

            if (targetNode != null)
            {
                // Find the index of the target node
                int nodeIndex = allNodes.IndexOf(targetNode.Value);

                // Add the element to the node
                AddElementToNode(nodeIndex, element, elementIndex);
            }
        }

        public void AddElementToNode(int nodeIndex, OctElement element, int elementIndex)
        {
            OctNode node = allNodes[nodeIndex];

            // Create a new element node and add it to the linked list
            OctElementNode elementNode = new OctElementNode(elementIndex, node.first_child);
            int elementNodeIndex = allElementNodes.Count;
            allElementNodes.Add(elementNode);

            // Update the node to point to the new element node
            node.first_child = elementNodeIndex;
            node.count++;
            allNodes[nodeIndex] = node;
        }


        private OctNode? GetOctantRecursive(int nodeIndex, OctElement element, Vector3I currentNodeOrigin, Vector3I currentNodeSize)
        {
            // Check if the element fits in this node
            if (!OctantHasElement(nodeIndex, element, currentNodeOrigin, currentNodeSize))
            {
                return null;
            }

            OctNode node = allNodes[nodeIndex];

            // If this is a leaf node, return it
            if (node.count != -1) // Leaf node
            {
                return node;
            }

            // This is an internal node, check each child
            Vector3I halfSize = currentNodeSize.HalfSize();
            for (int octant = 0; octant < 8; octant++)
            {
                Vector3I childOrigin = currentNodeOrigin.CalculateChildMin(halfSize, octant);
                int childIndex = node.first_child + octant;

                // Recursively check this child
                OctNode? childResult = GetOctantRecursive(childIndex, element, childOrigin, halfSize);

                if (childResult != null)
                {
                    return childResult; // Found a suitable node in this branch
                }
            }
            // If we reach here, none of the children contain the element
            return node; // Return this node as a fallback
        }

        private bool OctantHasElement(int nodeIndex, OctElement element, Vector3I currentNodeOrigin, Vector3I currentNodeSize)
        {
            Vector3I nodeMax = currentNodeOrigin.CalculateNodeMax(currentNodeSize);
            Vector3I elementMax = element.position.CalculateNodeMax(element.size);
            return currentNodeOrigin.Intersects(nodeMax, element.position, elementMax);
        }

        public void RemoveElement(OctElement element)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateElement(OctElement element)
        {
            throw new System.NotImplementedException();
        }

        // TODO: might want to add batch element adding,updating,removal
        // ------------------------------------------------------
        //
        // public void UpdateElement(OctElement element)
        // {
        //
        // }
        //
        // public void RemoveElement(OctElement element)
        // {
        //
        // }
        //
        // public void AddElement(OctElement element)
        // {
        //
        // }
        // -----------------------------------------------------
    }
}
