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
        ///<Summary>
        /// Adds given element to the octTree, if the node exists we add to it, if it's full, we split and reorder.
        ///</Summary>
        ///<param name="element">OctElement to add</param>
        public void AddElement(OctElement element)
        {
            // add new element to allElements.
            allElements.Add(element);
            // grab its index (0 based)
            int elementIndex = allElements.Count - 1;

            // Find or create the appropriate node for this element
            int nodeIndex = FindOrCreateNodeForElement(0, element, Vector3I.Zero, BoundsSize);

            // Add the element to the node
            AddElementToNode(nodeIndex, element, elementIndex);
        }

        ///<Summary>
        ///</Summary>
        private int FindOrCreateNodeForElement(int nodeIndex, OctElement element, Vector3I currentNodeOrigin, Vector3I currentNodeSize)
        {
            //get reference for node given its index.
            OctNode node = allNodes[nodeIndex];

            // If this is a leaf node with space
            // IE if count is in range 0- MAX_ELEMENTS_PER_NODE (inclusive) (we can assume it isn't a leaf cause count isn't -1), we are at the node and return its index
            if (node.count >= 0 && node.count < MAX_ELEMENTS_PER_NODE)
                return nodeIndex;

            // If this is a leaf node that's full, split it
            // IE if count isn't in range 0- MAX_ELEMENTS_PER_NODE (inclusive) (we can assume it isn't a leaf cause count isn't -1), we split and continue
            if (node.count >= 0 && node.count >= MAX_ELEMENTS_PER_NODE)
            {
                SplitNode(nodeIndex, currentNodeOrigin, currentNodeSize);
            }
            // calculate the nodes HalfSize 
            Vector3I halfSize = currentNodeSize.HalfSize();
            //loop through its octants.
            for (int octant = 0; octant < 8; octant++)
            {
                Vector3I childOrigin = currentNodeOrigin.CalculateChildMin(halfSize, octant);

                if (OctantHasElement(element, childOrigin, halfSize))
                {
                    int childIndex = node.first_child + octant;
                    return FindOrCreateNodeForElement(childIndex, element, childOrigin, halfSize);
                }
            }

            // If no suitable child found (should be rare), return this node
            return nodeIndex;
        }

        ///<Summary>
        /// Adds given element to node given the nodes index and its elements index
        ///</Summary>
        ///<param name="nodeIndex">Index of the Target Node</param>
        ///<param name="element">Element to add to Target Node</param>
        ///<param name="elementIndex">Index of element in allElements</param>
        private void AddElementToNode(int nodeIndex, OctElement element, int elementIndex)
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

        ///<Summary>
        /// Recursively search Octants and find "lowest octant" that holds an element.
        ///</Summary>
        ///<param name= "nodeIndex">Index to start at (usually root, but this is recursive so it needs to be exposed)</param>
        ///<param name= "currentNodeOrigin"> the position of the Node we are looking at, we are calculating "on the fly" so this needs to be passed as it is found from root nodes bounds_size.
        ///<param name= "currentNodeSize"> the size of the node we are looking at, we are calculating "on the fly" so this needs to be passed as it is found from root nodes bounds_size. 
        ///<param name="element">element to search for</param>
        private OctNode? GetOctantRecursive(int nodeIndex, OctElement element, Vector3I currentNodeOrigin, Vector3I currentNodeSize)
        {
            // Check if the element fits in this node
            if (!OctantHasElement(element, currentNodeOrigin, currentNodeSize))
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

        ///<Summary>
        /// helper function to check if Octant has <paramref name="element"/>
        ///</Summary>
        private bool OctantHasElement(OctElement element, Vector3I currentNodeOrigin, Vector3I currentNodeSize)
        {
            Vector3I nodeMax = currentNodeOrigin.CalculateNodeMax(currentNodeSize);
            Vector3I elementMax = element.position.CalculateNodeMax(element.size);
            return currentNodeOrigin.Intersects(nodeMax, element.position, elementMax);
        }

        ///<Summary>
        /// split a node, and reorders the elements
        ///</Summary>
        private void SplitNode(int nodeIndex, Vector3I currentNodeOrigin, Vector3I currentNodeSize)
        {
            OctNode node = allNodes[nodeIndex];

            int oldFirstChild = node.first_child;
            int oldCount = node.count;

            int firstChildIndex = allNodes.Count;
            for (int i = 0; i < 8; i++)
            {
                allNodes.Add(new OctNode(-1, 0));
            }
            node.first_child = firstChildIndex;
            node.count = -1;
            allNodes[nodeIndex] = node;

            int currentElementNodeIndex = oldFirstChild;
            Vector3I halfSize = currentNodeSize.HalfSize();

            for (int i = 0; i < oldCount; i++)
            {
                if (currentElementNodeIndex == -1)
                    break;

                OctElementNode elementNode = allElementNodes[currentElementNodeIndex];
                OctElement element = allElements[elementNode.element];
                int nextElementNodeIndex = elementNode.next;

                // Find the child octant for this element
                for (int octant = 0; octant < 8; octant++)
                {
                    Vector3I childOrigin = currentNodeOrigin.CalculateChildMin(halfSize, octant);

                    if (OctantHasElement(element, childOrigin, halfSize))
                    {
                        int childNodeIndex = firstChildIndex + octant;
                        AddElementToNode(childNodeIndex, element, elementNode.element);
                        break;
                    }
                }

                // Move to next element in linked list
                currentElementNodeIndex = nextElementNodeIndex;
            }
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
