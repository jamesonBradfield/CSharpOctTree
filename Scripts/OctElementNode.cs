public struct OctElementNode {
    // Points to the next element in the leaf node. A value of -1 
    // indicates the end of the list.
	public int next;
    // Stores the element index.
	public int element;
    public OctElementNode(int element, int next)
    {
        this.element = element;
        this.next = next;
    }
}

