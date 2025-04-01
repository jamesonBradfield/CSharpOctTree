public struct OctNode
{
	// We don't need to store an array of children because all 8 children are contiguous:
	//
	// first_child+0 = index to 1st child (UFL)
	// first_child+1 = index to 2nd child (UFR)
	// first_child+2 = index to 3nd child (UBL)
	// first_child+3 = index to 4th child (UBR)
	// first_child+4 = index to 5st child (DFL)
	// first_child+5 = index to 6nd child (DFR)
	// first_child+6 = index to 7nd child (DBL)
	// first_child+7 = index to 8th child (DBR)
	public int first_child;
    // Stores the number of elements in the leaf or -1 if this node is
    // not a leaf.
    public int count; 
    public OctNode(int first_child, int count) : this()
    {
        this.first_child = first_child;
		this.count = count;
    }
}
