using Godot;
using System.Collections.Immutable;

namespace OctTreeNamespace
{
    /// <summary>
    /// Builder pattern for creating OctTree instances
    /// </summary>
    public class OctTreeBuilder
    {
        private OctTreeState state;
        
        public OctTreeBuilder(OctTree tree)
        {
            state = tree.State;
        }
        
        public OctTreeBuilder WithElements(ImmutableList<OctElt> elements)
        {
            state = state.WithElements(elements);
            return this;
        }
        
        public OctTreeBuilder WithElementNodes(ImmutableList<OctEltNode> elementNodes)
        {
            state = state.WithElementNodes(elementNodes);
            return this;
        }
        
        public OctTreeBuilder WithNodes(ImmutableList<OctNode> nodes)
        {
            state = state.WithNodes(nodes);
            return this;
        }
        
        public OctTreeBuilder AddElement(OctElt element)
        {
            state = state.WithElements(state.Elements.Add(element));
            return this;
        }
        
        public OctTreeBuilder AddElementNode(OctEltNode elementNode)
        {
            state = state.WithElementNodes(state.ElementNodes.Add(elementNode));
            return this;
        }
        
        public OctTreeBuilder AddNode(OctNode node)
        {
            state = state.WithNodes(state.Nodes.Add(node));
            return this;
        }
        
        public OctTreeBuilder UpdateNode(int index, OctNode node)
        {
            state = state.WithNodes(state.Nodes.SetItem(index, node));
            return this;
        }
        
        public OctTreeBuilder UpdateElementNode(int index, OctEltNode elementNode)
        {
            state = state.WithElementNodes(state.ElementNodes.SetItem(index, elementNode));
            return this;
        }
        
        public OctTree Build()
        {
            return new OctTree(state);
        }
    }
}
