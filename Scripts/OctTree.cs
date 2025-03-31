using Godot;
using System.Collections.Generic;

namespace OctTreeNamespace
{
    /// <summary>
    /// </summary>
    public class OctTree
    {
        private const int  MAX_ELEMENTS_PER_NODE = 8;
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

        public OctTree(Vector3I BoundsSize){
            root_node = new OctNode(0,-1);
            this.BoundsSize = BoundsSize;
            // add root_node to list.
            allNodes.Add(root_node);
        }

        public void AddElement(OctElement element)
        {
            // we need to traverse our tree, start at root_node with a bounds_size, and subdivide if root has reached MAX_ELEMENTS_PER_NODE limit.
            OctNode current_node = root_node;
            // we need to traverse root_nodes children somehow.
            if (IsOctantForElement(current_node,element)){
                if(root_node.count >= MAX_ELEMENTS_PER_NODE){
                    AddElementToNode(current_node,element);
                } else {
                    SplitNodeForElement(current_node,element);
                }
            } else {
                current_node = allNodes[root_node.first_child];
            }
            // if we have reached our limit, we should only create the needed partition, IE (if we have an extra in quadrant four where that quadrant would be split into 4 we instead split only the needed quadrant.)
        }

        public void RemoveElement(OctElement element)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateElement(OctElement element)
        {
            throw new System.NotImplementedException();
        }

        public bool IsOctantForElement(OctNode node, OctElement element)
        {
            // check if the current_node contains the element.
            // this would include either storing the bounds of each node, or calculating on the fly using bit shifting based on BoundsSize and some sort of depth calculation, there might be a quick way to roughly calculate which Octant it is in by storing some data about the deepest Node(how deep it is should directly correlate to the size), and use that to roughly calculate which octant it would be if it was at that depth and move upwards if its more efficient.
            throw new System.NotImplementedException();
        }

        public void AddElementToNode(OctNode node, OctElement element){
            throw new System.NotImplementedException();
        }

        public void SplitNodeForElement(OctNode node, OctElement element){
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
