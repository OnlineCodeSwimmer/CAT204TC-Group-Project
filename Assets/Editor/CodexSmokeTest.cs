/*
 * Temporary Unity smoke-test harness used to verify the transferred project.
 * It is intentionally commented instead of deleted so the validation can be
 * restored later if package-cache or runtime startup problems reappear.
 *
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CodexSmokeTest
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ResultPath = "Logs/CodexSmokeTestResult.txt";
    private static readonly List<string> Failures = new List<string>();
    private const string TestActiveKey = "CodexSmokeTest.Active";
    private static double playDeadline;
    private static bool enteredPlayMode;

    static CodexSmokeTest()
    {
        if (!SessionState.GetBool(TestActiveKey, false))
            return;

        Application.logMessageReceived -= CaptureLog;
        Application.logMessageReceived += CaptureLog;
        EditorApplication.update -= MonitorPlayMode;
        EditorApplication.update += MonitorPlayMode;
    }

    [MenuItem("Tools/Codex/Smoke Test")]
    public static void Run()
    {
        Debug.Log("[CodexSmokeTest] Starting");
        Failures.Clear();
        enteredPlayMode = false;
        SessionState.SetBool(TestActiveKey, true);
        Application.logMessageReceived -= CaptureLog;
        Application.logMessageReceived += CaptureLog;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.update -= MonitorPlayMode;
        EditorApplication.update += MonitorPlayMode;
        EditorApplication.EnterPlaymode();
    }

    private static void MonitorPlayMode()
    {
        if (!enteredPlayMode)
        {
            if (!EditorApplication.isPlaying)
                return;

            enteredPlayMode = true;
            playDeadline = EditorApplication.timeSinceStartup + 6d;
            return;
        }

        if (EditorApplication.timeSinceStartup < playDeadline)
            return;

        ValidateRuntime();
        EditorApplication.update -= MonitorPlayMode;
        Application.logMessageReceived -= CaptureLog;
        SessionState.SetBool(TestActiveKey, false);
        EditorApplication.ExitPlaymode();

        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "Logs");
        string report = Failures.Count == 0
            ? "PASS\nNo runtime errors were observed during the smoke test.\n"
            : "FAIL\n" + string.Join("\n", Failures) + "\n";
        File.WriteAllText(ResultPath, report);
        Debug.Log("[CodexSmokeTest]\n" + report);
        EditorApplication.Exit(Failures.Count == 0 ? 0 : 1);
    }

    private static void ValidateRuntime()
    {
        Require(GameManager.instance != null, "GameManager.instance is missing.");
        Require(UIManager.instance != null, "UIManager.instance is missing.");
        Require(PassiveCardInventory.instance != null, "PassiveCardInventory.instance is missing.");
        Require(PoolManager.instance != null, "PoolManager.instance is missing.");
        Require(GameFeedback.Instance != null, "GameFeedback.Instance is missing.");

        if (GameManager.instance == null || GameFeedback.Instance == null)
            return;

        GameManager manager = GameManager.instance;
        Require(manager.rounds > 0, "No rounds were configured.");
        Require(manager.currentTargetScore > 0f, "The first target score is not valid.");

        float scoreBefore = manager.score;
        manager.GainScore(25f);
        Require(manager.score > scoreBefore, "GainScore did not increase the score.");

        float ballValueBefore = manager.pinballScore;
        manager.ChangeBallValue(50f);
        Require(Mathf.Approximately(manager.pinballScore, ballValueBefore + 50f),
            "ChangeBallValue did not update the ball value.");

        int goldBefore = manager.gold;
        manager.ChangeShopGold(2);
        Require(manager.gold == goldBefore + 2, "ChangeShopGold did not add gold.");

        UIManager.instance.OpenShop();
        Require(UIManager.instance.shop.activeSelf, "The shop did not open.");
        UIManager.instance.CloseShop();
        Require(!UIManager.instance.shop.activeSelf, "The shop did not close.");
    }

    private static void Require(bool condition, string failure)
    {
        if (!condition)
            Failures.Add(failure);
    }

    private static void CaptureLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error)
            return;

        Failures.Add(type + ": " + message + "\n" + stackTrace);
    }
}
*/
