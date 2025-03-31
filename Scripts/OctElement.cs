using Godot;
public struct OctElement
{
	public int id; // Unique Identifier
	public Vector3I position; // Element Position
    public Vector3I size; // Element Size
    public OctElement(int id, Vector3I position, Vector3I size)
    {
        this.id = id;
        this.position = position;
        this.size = size;
    }
}

