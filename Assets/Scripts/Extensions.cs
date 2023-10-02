using Godot;

public static class Extensions
{
    public static T GetChildOfType<T>(this Node node) where T : Node
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T childOfType)
                return childOfType;
        }

        return null;
    }
    
    public static void ClampMagnitude(this Vector3 vector, float maxLength)
    {
        float sqrMagnitude = vector.LengthSquared();
        
        if ((double) sqrMagnitude <= (double) maxLength * (double) maxLength)
            return;
        
        float num1 = (float) Mathf.Sqrt((double) sqrMagnitude);
        float num2 = vector.X / num1;
        float num3 = vector.Y / num1;
        float num4 = vector.Z / num1;
        
        vector = new Vector3(num2 * maxLength, num3 * maxLength, num4 * maxLength);
    }
    
    //Extract and return parts from a vector that are pointing in the same direction as 'direction';
    public static Vector3 ExtractDotVector(this Vector3 vector, Vector3 direction)
    {
        //Normalize vector if necessary;
        if(direction.LengthSquared() != 1)
            direction.Normalized();
			    
        float dot = vector.Dot(direction);
			    
        return direction * dot;
    }
    
    //Returns the length of the part of a vector that points in the same direction as '_direction' (i.e., the dot product);
    public static float GetDotProduct(this Vector3 vector, Vector3 direction)
    {
        //Normalize vector if necessary;
        if(direction.LengthSquared() != 1)
            direction.Normalized();
				
        float length = vector.Dot(direction);

        return length;
    }
    
    //Remove all parts from a vector that are pointing in the same direction as '_direction';
    public static Vector3 RemoveDotVector(this Vector3 vector, Vector3 direction)
    {
        //Normalize vector if necessary;
        if(direction.LengthSquared() != 1)
            direction.Normalized();
			
        float amount = vector.Dot(direction);
			
        vector -= direction * amount;
			
        return vector;
    }
}