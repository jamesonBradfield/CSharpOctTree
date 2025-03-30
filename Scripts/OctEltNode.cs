public struct OctEltNode {
    // Points to the next element in the leaf node. A value of -1 
    // indicates the end of the list.
	public int next;
    // Stores the element index.
	public int element;
    public OctEltNode(int element, int next)
    {
        this.element = element;
        this.next = next;
    }
}

