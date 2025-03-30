using Godot;
public struct OctElt
{
	public int id; // Unique Identifier
	public Vector3I position; // Bounding box of the element itself.

    public OctElt(int id, Vector3I position)
    {
        this.id = id;
        this.position = position;
    }
}

