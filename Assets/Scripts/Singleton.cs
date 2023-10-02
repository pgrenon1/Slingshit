using Godot;

public partial class Singleton<T> : Node where T : Node
{
    public static T Instance
    {
        get
        {
            MainLoop mainLoop = Godot.Engine.GetMainLoop();
            if (mainLoop is not SceneTree sceneTree)
                return null;
            
            T instance = sceneTree.Root.GetNode<T>($"/root/{typeof(T).FullName}");

            return instance;
        }
    }
}