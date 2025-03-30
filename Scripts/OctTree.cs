using Godot;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace OctTreeNamespace
{
    /// <summary>
    /// An immutable octree implementation for spatial partitioning.
    /// Each operation returns a new octree with the changes applied.
    /// </summary>
    public class OctTree
    {
        // Constants for configuration
        private const int MAX_ELEMENTS_PER_NODE = 8;

        // Single state container for all octree data
        public OctTreeState State { get; }

        // Public convenience properties that delegate to the state object
        public ImmutableList<OctElt> Elements => State.Elements;
        public ImmutableList<OctEltNode> ElementNodes => State.ElementNodes;
        public ImmutableList<OctNode> Nodes => State.Nodes;
        public Vector3I BoundsSize => State.BoundsSize;
        public int MaxDepth => State.MaxDepth;

        // Constructor creates an empty octree
        public OctTree(Vector3I boundsSize, int maxDepth)
        {
            State = new OctTreeState(
                ImmutableList<OctElt>.Empty,
                ImmutableList<OctEltNode>.Empty,
                ImmutableList.Create(new OctNode(-1, -1)), // Root node
                boundsSize,
                maxDepth
            );
        }

        // Internal constructor for creating modified octrees
        internal OctTree(OctTreeState state)
        {
            State = state;
        }

        /// <summary>
        /// Add an element to the octree, returning a new octree with the element added
        /// </summary>
        public OctTree AddElement(OctElt newElement)
        {
            // Create new state with element added
            ImmutableList<OctElt> updatedElements = State.Elements.Add(newElement);
            int newElementIndex = State.Elements.Count; // Index in the new list

            // Create updated tree state
            OctTreeState updatedState = State.WithElements(updatedElements);

            // Use immutable traversal context
            AddElementContext traversalContext = new AddElementContext(
                newElement,
                newElementIndex,
                updatedState);

            // Perform traversal (returns updated context)
            AddElementContext resultContext = TraverseTreeForAdd(
                traversalContext,
                Vector3I.Zero,
                State.BoundsSize,
                0);

            // Return new octree with updated state
            return new OctTree(resultContext.State);
        }

        /// <summary>
        /// Find elements within a bounding box
        /// </summary>
        public IEnumerable<OctElt> FindElementsInBox(Vector3I minBounds, Vector3I maxBounds)
        {
            SearchContext searchContext = new SearchContext(
                State,
                minBounds,
                maxBounds,
                new List<OctElt>());

            SearchContext resultContext = TraverseTreeForSearch(
                searchContext,
                Vector3I.Zero,
                State.BoundsSize,
                0);

            return resultContext.Results;
        }

        /// <summary>
        /// Remove an element by ID, returning a new octree with the element removed
        /// </summary>
        public OctTree RemoveElement(int elementId)
        {
            // Find the element index in the Elements list
            int elementIndex = -1;
            for (int i = 0; i < State.Elements.Count; i++)
            {
                if (State.Elements[i].id == elementId)
                {
                    elementIndex = i;
                    break;
                }
            }

            // Element not found
            if (elementIndex == -1)
            {
                return this;
            }

            // Create new context for traversal with all necessary data
            RemoveElementContext context = new RemoveElementContext(
                elementIndex,
                State);

            // Traverse tree to find and remove the element
            RemoveElementContext resultContext = TraverseTreeForRemove(
                context,
                Vector3I.Zero,
                State.BoundsSize,
                0);

            // If element wasn't found in the tree structure, return unchanged
            if (!resultContext.ElementRemoved)
            {
                return this;
            }

            // Create a new element list without the removed element
            ImmutableList<OctElt> updatedElements = ImmutableList<OctElt>.Empty;
            for (int i = 0; i < State.Elements.Count; i++)
            {
                if (i != elementIndex)
                {
                    updatedElements = updatedElements.Add(State.Elements[i]);
                }
            }

            // Adjust all element indices in ElementNodes that were after the removed element
            ImmutableList<OctEltNode> updatedElementNodes = ImmutableList<OctEltNode>.Empty;
            for (int i = 0; i < resultContext.State.ElementNodes.Count; i++)
            {
                OctEltNode node = resultContext.State.ElementNodes[i];
                if (node.element > elementIndex)
                {
                    updatedElementNodes = updatedElementNodes.Add(
                        new OctEltNode(node.element - 1, node.next));
                }
                else
                {
                    updatedElementNodes = updatedElementNodes.Add(node);
                }
            }

            // Create final state and return updated tree
            OctTreeState finalState = new OctTreeState(
                updatedElements,
                updatedElementNodes,
                resultContext.State.Nodes,
                State.BoundsSize,
                State.MaxDepth);

            return new OctTree(finalState);
        }

        /// <summary>
        /// Update an element's position, returning a new octree
        /// </summary>
        public OctTree UpdateElement(int elementId, Vector3I newPosition)
        {
            // First remove the element
            OctTree treeWithoutElement = RemoveElement(elementId);

            // If the tree is unchanged (element not found), return this
            if (treeWithoutElement.Elements.Count == Elements.Count)
            {
                return this;
            }

            // Find the original element to preserve its ID
            OctElt? originalElement = null;
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].id == elementId)
                {
                    originalElement = Elements[i];
                    break;
                }
            }

            if (originalElement == null)
            {
                return this; // Should never happen if removal worked
            }

            // Create new element with updated position
            OctElt updatedElement = new OctElt(elementId, newPosition);

            // Add the updated element to the tree
            return treeWithoutElement.AddElement(updatedElement);
        }

        /// <summary>
        /// Specialized traversal for adding elements
        /// </summary>
        private AddElementContext TraverseTreeForAdd(
            AddElementContext context,
            Vector3I nodeMin,
            Vector3I nodeSize,
            int depth)
        {
            // Stack for non-recursive traversal
            Stack<(int nodeIndex, Vector3I nodeMin, Vector3I nodeSize, int depth)> traversalStack =
                new Stack<(int, Vector3I, Vector3I, int)>();

            traversalStack.Push((0, nodeMin, nodeSize, depth));

            AddElementContext currentContext = context;

            while (traversalStack.Count > 0)
            {
                (int nodeIndex, Vector3I currentMin, Vector3I currentSize, int currentDepth) = traversalStack.Pop();

                // Check if we've already found our destination node
                if (currentContext.NodeFound)
                    break;

                OctNode currentNode = currentContext.State.Nodes[nodeIndex];

                // Handle leaf node or max depth
                if (NodeOperations.IsLeaf(currentNode) || currentDepth >= State.MaxDepth)
                {
                    currentContext = HandleLeafNodeForAdd(nodeIndex, currentMin, currentSize, currentDepth, currentContext);
                    continue;
                }

                // Handle internal node
                Vector3I halfSize = currentSize.HalfSize();
                Vector3I centerPoint = currentMin.CalculateCenter(currentSize);

                // Determine which octant the element belongs in
                int targetOctant = SelectOctantForElement(centerPoint, currentContext.Element);

                // Create child nodes if needed
                if (!NodeOperations.HasChildren(currentNode))
                {
                    int firstChildIndex = currentContext.State.Nodes.Count;
                    ImmutableList<OctNode> updatedNodes = NodeOperations.AddChildNodes(
                        currentContext.State.Nodes, 8);

                    // Update parent node to point to children
                    updatedNodes = NodeOperations.UpdateNode(
                        updatedNodes,
                        nodeIndex,
                        NodeOperations.CreateInternalNode(firstChildIndex)
                    );

                    // Update context
                    currentContext = currentContext.WithState(
                        currentContext.State.WithNodes(updatedNodes));

                    // Update current node for traversal logic
                    currentNode = NodeOperations.CreateInternalNode(firstChildIndex);
                }

                // Calculate child bounds and continue traversal
                Vector3I childMin = currentMin.CalculateChildMin(halfSize, targetOctant);

                traversalStack.Push((currentNode.first_child + targetOctant, childMin, halfSize, currentDepth + 1));
            }

            return currentContext;
        }

        /// <summary>
        /// Specialized traversal for removing elements
        /// </summary>
        private RemoveElementContext TraverseTreeForRemove(
            RemoveElementContext context,
            Vector3I nodeMin,
            Vector3I nodeSize,
            int depth)
        {
            // Stack for non-recursive traversal
            Stack<(int nodeIndex, Vector3I nodeMin, Vector3I nodeSize, int depth)> traversalStack =
                new Stack<(int, Vector3I, Vector3I, int)>();

            traversalStack.Push((0, nodeMin, nodeSize, depth));

            RemoveElementContext currentContext = context;

            while (traversalStack.Count > 0 && !currentContext.ElementRemoved)
            {
                (int nodeIndex, Vector3I currentMin, Vector3I currentSize, int currentDepth) = traversalStack.Pop();

                OctNode currentNode = currentContext.State.Nodes[nodeIndex];

                // Skip non-leaf nodes if they have no children
                if (NodeOperations.IsInternalNode(currentNode) && !NodeOperations.HasChildren(currentNode))
                    continue;

                // Process leaf nodes
                if (NodeOperations.IsLeaf(currentNode))
                {
                    currentContext = ProcessLeafNodeForRemove(nodeIndex, currentContext);
                    continue;
                }

                // For internal nodes, determine which child might contain the element
                Vector3I halfSize = currentSize.HalfSize();
                Vector3I centerPoint = currentMin.CalculateCenter(currentSize);

                // Get the element we're looking for from the context
                OctElt element = currentContext.State.Elements[currentContext.ElementIndex];

                // Determine which octant the element belongs in
                int targetOctant = SelectOctantForElement(centerPoint, element);

                // Skip if no children
                if (!NodeOperations.HasChildren(currentNode))
                    continue;

                // Calculate child bounds and push to stack
                Vector3I childMin = currentMin.CalculateChildMin(halfSize, targetOctant);

                traversalStack.Push((currentNode.first_child + targetOctant, childMin, halfSize, currentDepth + 1));
            }

            return currentContext;
        }

        /// <summary>
        /// Specialized traversal for searching
        /// </summary>
        private SearchContext TraverseTreeForSearch(
            SearchContext context,
            Vector3I nodeMin,
            Vector3I nodeSize,
            int depth)
        {
            // Stack for non-recursive traversal
            Stack<(int nodeIndex, Vector3I nodeMin, Vector3I nodeSize, int depth)> traversalStack =
                new Stack<(int, Vector3I, Vector3I, int)>();

            traversalStack.Push((0, nodeMin, nodeSize, depth));

            SearchContext currentContext = context;

            while (traversalStack.Count > 0)
            {
                (int nodeIndex, Vector3I currentMin, Vector3I currentSize, int currentDepth) = traversalStack.Pop();

                // Calculate node max bounds
                Vector3I nodeMax = new Vector3I(
                    currentMin.X + currentSize.X,
                    currentMin.Y + currentSize.Y,
                    currentMin.Z + currentSize.Z
                );

                // Skip nodes that don't intersect the search area
                if (!currentMin.Intersects(nodeMax, currentContext.SearchMin, currentContext.SearchMax))
                    continue;

                OctNode currentNode = currentContext.State.Nodes[nodeIndex];

                // Process leaf nodes
                if (NodeOperations.IsLeaf(currentNode))
                {
                    currentContext = ProcessLeafNodeForSearch(nodeIndex, currentContext);
                    continue;
                }

                // Skip traversal if no children
                if (!NodeOperations.HasChildren(currentNode))
                    continue;

                // Calculate half size for children
                Vector3I halfSize = currentSize.HalfSize();

                // Push all children to stack for breadth-first traversal
                for (int octant = 7; octant >= 0; octant--)
                {
                    Vector3I childMin = currentMin.CalculateChildMin(halfSize, octant);

                    traversalStack.Push((currentNode.first_child + octant, childMin, halfSize, currentDepth + 1));
                }
            }

            return currentContext;
        }

        /// <summary>
        /// Determine which octant an element belongs in
        /// </summary>
        private int SelectOctantForElement(Vector3I center, OctElt element)
        {
            Vector3I elementPos = element.position;

            int octant = 0;
            octant |= (elementPos.X >= center.X ? 1 : 0); // X bit
            octant |= (elementPos.Z >= center.Z ? 2 : 0); // Z bit
            octant |= (elementPos.Y < center.Y ? 4 : 0);  // Y bit (Y+ is up)

            return octant;
        }

        /// <summary>
        /// Handle adding an element to a leaf node
        /// </summary>
        private AddElementContext HandleLeafNodeForAdd(
            int nodeIndex,
            Vector3I nodeMin,
            Vector3I nodeSize,
            int depth,
            AddElementContext context)
        {
            if (context.NodeFound) return context;

            ImmutableList<OctNode> updatedNodes = context.State.Nodes;
            ImmutableList<OctEltNode> updatedElementNodes = context.State.ElementNodes;

            OctNode targetNode = updatedNodes[nodeIndex];

            // Convert to leaf if necessary
            if (!NodeOperations.IsLeaf(targetNode))
            {
                updatedNodes = NodeOperations.UpdateNode(
                    updatedNodes, 
                    nodeIndex, 
                    NodeOperations.CreateEmptyLeaf());
                    
                targetNode = updatedNodes[nodeIndex];
            }

            // Check if we need to split the node based on MAX_ELEMENTS_PER_NODE
            if (targetNode.count >= MAX_ELEMENTS_PER_NODE && depth < State.MaxDepth)
            {
                // Split the node by redistributing elements
                return SplitNodeAndAddElement(nodeIndex, nodeMin, nodeSize, depth, context);
            }

            // Create new element node
            OctEltNode newElementNode = new OctEltNode(
                context.ElementIndex,
                targetNode.count > 0 ? targetNode.first_child : -1
            );

            int elementNodeIndex = updatedElementNodes.Count;
            updatedElementNodes = updatedElementNodes.Add(newElementNode);

            // Update the node
            updatedNodes = NodeOperations.UpdateNode(
                updatedNodes,
                nodeIndex,
                NodeOperations.CreateLeafNode(elementNodeIndex, targetNode.count + 1));

            // Create updated state
            OctTreeState updatedState = context.State.WithUpdatedCollections(
                context.State.Elements,
                updatedElementNodes,
                updatedNodes);

            // Return updated context
            return context.WithState(updatedState).WithNodeFound(nodeIndex);
        }

        /// <summary>
        /// Split a node and redistribute its elements
        /// </summary>
        private AddElementContext SplitNodeAndAddElement(
            int nodeIndex,
            Vector3I nodeMin,
            Vector3I nodeSize,
            int depth,
            AddElementContext context)
        {
            OctNode leafNode = context.State.Nodes[nodeIndex];

            // Create 8 child nodes
            int firstChildIndex = context.State.Nodes.Count;
            ImmutableList<OctNode> updatedNodes = NodeOperations.AddChildNodes(
                context.State.Nodes, 8);

            // Convert this node to an internal node
            updatedNodes = NodeOperations.UpdateNode(
                updatedNodes,
                nodeIndex,
                NodeOperations.CreateInternalNode(firstChildIndex));

            // Update context with new nodes
            OctTreeState updatedState = context.State.WithNodes(updatedNodes);
            AddElementContext updatedContext = context.WithState(updatedState);

            // Redistribute existing elements to child nodes
            int elementNodeIndex = leafNode.first_child;
            for (int i = 0; i < leafNode.count; i++)
            {
                if (elementNodeIndex == -1)
                    break;

                OctEltNode elementNode = context.State.ElementNodes[elementNodeIndex];

                // Get the element from the context's Elements collection
                OctElt redistributedElement = context.State.Elements[elementNode.element];

                // Calculate half size and center
                Vector3I halfSize = nodeSize.HalfSize();
                Vector3I centerPoint = nodeMin.CalculateCenter(nodeSize);

                // Determine which octant the element belongs in
                int targetOctant = SelectOctantForElement(centerPoint, redistributedElement);

                // Calculate child bounds
                Vector3I childMin = nodeMin.CalculateChildMin(halfSize, targetOctant);

                // Create context for just this redistribution
                AddElementContext elementContext = updatedContext.WithState(updatedState);
                elementContext = elementContext with 
                { 
                    Element = redistributedElement,
                    ElementIndex = elementNode.element,
                    NodeFound = false,
                    FinalNodeIndex = 0
                };

                // Handle direct insertion to proper leaf without traversal
                int childNodeIndex = firstChildIndex + targetOctant;
                OctNode childNode = updatedContext.State.Nodes[childNodeIndex];

                // If this is a leaf node or at max depth, add directly
                if (NodeOperations.IsLeaf(childNode) || depth + 1 >= State.MaxDepth)
                {
                    // Create or update leaf node
                    if (!NodeOperations.IsLeaf(childNode))
                    {
                        updatedNodes = NodeOperations.UpdateNode(
                            updatedNodes,
                            childNodeIndex,
                            NodeOperations.CreateEmptyLeaf());
                            
                        childNode = updatedNodes[childNodeIndex];
                    }

                    // Create new element node with proper links
                    OctEltNode newElementNode = new OctEltNode(
                        elementNode.element,
                        childNode.count > 0 ? childNode.first_child : -1
                    );

                    int newElementNodeIndex = updatedContext.State.ElementNodes.Count;
                    ImmutableList<OctEltNode> updatedElementNodes = updatedContext.State.ElementNodes.Add(newElementNode);

                    // Update the child node
                    updatedNodes = NodeOperations.UpdateNode(
                        updatedNodes,
                        childNodeIndex,
                        NodeOperations.CreateLeafNode(newElementNodeIndex, childNode.count + 1));

                    updatedContext = updatedContext.WithState(updatedContext.State.WithUpdatedCollections(
                        updatedContext.State.Elements,
                        updatedElementNodes,
                        updatedNodes));
                }
                else
                {
                    // Recursively traverse for deeper insertion - ONLY when needed
                    elementContext = TraverseTreeForAdd(
                        elementContext,
                        childMin,
                        halfSize,
                        depth + 1
                    );

                    // Update our context with any node changes from the traversal
                    updatedContext = updatedContext.WithState(elementContext.State);
                }

                // Move to next element
                elementNodeIndex = elementNode.next;
            }

            // Finally, add the new element
            updatedContext = TraverseTreeForAdd(
                updatedContext with
                {
                    Element = context.Element,
                    ElementIndex = context.ElementIndex,
                    NodeFound = false,
                    FinalNodeIndex = 0
                },
                nodeMin,
                nodeSize,
                depth
            );

            return updatedContext;
        }

        /// <summary>
        /// Process a leaf node during removal
        /// </summary>
        private RemoveElementContext ProcessLeafNodeForRemove(int nodeIndex, RemoveElementContext context)
        {
            OctNode leafNode = context.State.Nodes[nodeIndex];

            if (leafNode.count <= 0)
                return context;

            // Check if the element is in this leaf node
            int prevElementNodeIndex = -1;
            int elementNodeIndex = leafNode.first_child;

            for (int i = 0; i < leafNode.count; i++)
            {
                if (elementNodeIndex == -1)
                    break;

                OctEltNode elementNode = context.State.ElementNodes[elementNodeIndex];

                // If this is the element we're looking for
                if (elementNode.element == context.ElementIndex)
                {
                    // Update the node structure to remove this element
                    ImmutableList<OctNode> updatedNodes = context.State.Nodes;

                    // If this is the first element in the list
                    if (prevElementNodeIndex == -1)
                    {
                        // Update the leaf node to point to the next element
                        updatedNodes = NodeOperations.UpdateNode(
                            updatedNodes,
                            nodeIndex,
                            NodeOperations.CreateLeafNode(elementNode.next, leafNode.count - 1));
                            
                        return context.WithState(context.State.WithNodes(updatedNodes))
                                     .WithElementRemoved(nodeIndex);
                    }
                    else
                    {
                        // Update the previous element node to skip this one
                        OctEltNode prevNode = context.State.ElementNodes[prevElementNodeIndex];
                        ImmutableList<OctEltNode> updatedElementNodes = context.State.ElementNodes.SetItem(
                            prevElementNodeIndex,
                            new OctEltNode(prevNode.element, elementNode.next)
                        );

                        return context.WithState(context.State.WithUpdatedCollections(
                                        context.State.Elements,
                                        updatedElementNodes,
                                        updatedNodes))
                                     .WithElementRemoved(nodeIndex);
                    }
                }

                // Move to next element
                prevElementNodeIndex = elementNodeIndex;
                elementNodeIndex = elementNode.next;
            }

            return context;
        }

        /// <summary>
        /// Process a leaf node during search
        /// </summary>
        private SearchContext ProcessLeafNodeForSearch(int nodeIndex, SearchContext context)
        {
            OctNode leafNode = context.State.Nodes[nodeIndex];

            if (leafNode.count <= 0)
                return context;

            // Traverse the linked list of elements in this leaf
            int elementNodeIndex = leafNode.first_child;
            for (int i = 0; i < leafNode.count; i++)
            {
                if (elementNodeIndex == -1)
                    break;

                OctEltNode elementNode = context.State.ElementNodes[elementNodeIndex];
                OctElt element = context.State.Elements[elementNode.element];

                // Test if element is in search area
                if (context.SearchMin.ContainsPoint(context.SearchMax, element.position))
                {
                    context.Results.Add(element);
                }

                // Move to the next element
                elementNodeIndex = elementNode.next;
            }

            return context;
        }
    }
}
