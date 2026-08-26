using UnityEditor;

[InitializeOnLoad]
internal static class UnityMcpAutoConnect
{
    static UnityMcpAutoConnect()
    {
        // Keep the Unity-side stdio bridge ready whenever this project opens.
        MCPForUnity.Editor.McpCiBoot.StartStdioForCi();
    }
}
