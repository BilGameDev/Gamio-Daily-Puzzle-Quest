using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace Gamio.Build
{
    public static class iOSBuilder
    {
        private static readonly string[] Scenes = {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Login.unity",
            "Assets/Scenes/Home.unity"
        };

        public static void iOS()
        {
            string buildPath = Path.Combine(Directory.GetCurrentDirectory(), "Build", "iOS");
            Directory.CreateDirectory(buildPath);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorApplication.Exit(0);
            }
            else
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
