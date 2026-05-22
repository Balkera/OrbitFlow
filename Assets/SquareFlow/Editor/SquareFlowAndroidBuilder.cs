using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SquareFlow.Editor
{
    public static class SquareFlowAndroidBuilder
    {
        private const string DefaultApkPath = "Builds/Android/SquareFlow-dev.apk";

        [MenuItem("Square Flow/Build Android APK")]
        public static void BuildDevelopmentApk()
        {
            string apkPath = GetArgumentValue("-apkPath", DefaultApkPath);
            BuildApk(apkPath, BuildOptions.Development, ScriptingImplementation.Mono2x);
        }

        private static void BuildApk(string apkPath, BuildOptions buildOptions, ScriptingImplementation scriptingBackend)
        {
            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
                throw new BuildFailedException("Android APK build failed because no enabled scenes are configured.");

            string fullApkPath = Path.GetFullPath(apkPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullApkPath));

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            ScriptingImplementation previousScriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            AndroidArchitecture previousTargetArchitectures = PlayerSettings.Android.targetArchitectures;

            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, scriptingBackend);
                if (scriptingBackend == ScriptingImplementation.Mono2x)
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = fullApkPath,
                    target = BuildTarget.Android,
                    options = buildOptions
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException(
                        $"Android APK build failed with result {summary.result}. Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
                }

                Debug.Log($"Android APK built successfully at {fullApkPath}. Size: {summary.totalSize} bytes.");
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = previousTargetArchitectures;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, previousScriptingBackend);
            }
        }

        private static string[] GetEnabledScenes()
        {
            return Array.ConvertAll(
                Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled),
                scene => scene.path);
        }

        private static string GetArgumentValue(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return fallback;
        }
    }
}
