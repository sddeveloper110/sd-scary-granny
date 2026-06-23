using Firebase;
using Firebase.Crashlytics;
using Firebase.RemoteConfig;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Analytics;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    public bool isFeatureEnabled { get; private set; } = true;
    public bool adPermissionTemp;

    // ✅ AdsManager listens to this before loading any ads
    public static event Action OnRemoteConfigFetched;
    public bool isRemoteConfigReady { get; private set; } = false;

    public static event Action<bool, bool> OnAdsPermissionGranted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        Debug.Log("🔥 [Firebase] Starting dependency check...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            Debug.Log($"🔥 [Firebase] Dependency status: {dependencyStatus}");

            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                var options = app.Options;

                Debug.Log("╔════════════════════════════════════════════════════════════════╗");
                Debug.Log("║                  🔥 FIREBASE CONNECTIVITY REPORT               ║");
                Debug.Log("╠════════════════════════════════════════════════════════════════╣");
                Debug.Log($"║  App Name:         {app.Name,-43} ║");
                Debug.Log($"║  Project ID:       {options.ProjectId,-43} ║");
                Debug.Log($"║  App ID (Android): {options.AppId,-43} ║");
                Debug.Log($"║  API Key:          {options.ApiKey,-43} ║");
                Debug.Log($"║  Storage Bucket:   {options.StorageBucket,-43} ║");
                Debug.Log($"║  Sender ID:        {options.MessageSenderId,-43} ║");
                Debug.Log("╚════════════════════════════════════════════════════════════════╝");

                // Verify we're connected to the CORRECT project
                if (options.ProjectId != "scary-granny-da973")
                {
                    Debug.LogError("🚨 [Firebase] WRONG PROJECT DETECTED! App is NOT connected to 'scary-granny-da973'.");
                    Debug.LogError($"🚨 Current project in app options: '{options.ProjectId}'");
                }
                else
                {
                    Debug.Log("✅ [Firebase] Connection verified for project: scary-granny-da973");
                }

                Debug.Log("🔥 [Firebase] Setup Remote Config...");
                SetupRemoteConfig();
            }
            else
            {
                Debug.LogError($"🚨 [Firebase] Dependency error: {dependencyStatus}");

                // ✅ Even on failure, unblock ads with defaults
                isFeatureEnabled = true;
                isRemoteConfigReady = true;
                OnRemoteConfigFetched?.Invoke();
            }
        });
    }

    private void SetupRemoteConfig()
    {
        var defaults = new Dictionary<string, object>
        {
            { "Ads_Manager", true }
        };

        Debug.Log("🔥 [Firebase] Setting Remote Config defaults...");

        FirebaseRemoteConfig.DefaultInstance
            .SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError($"🚨 [Firebase] SetDefaultsAsync FAILED: {t.Exception}");
                    return;
                }
                Debug.Log("✅ [Firebase] Defaults set. Starting fetch...");
                FetchRemoteConfig();
            });
    }

    public void FetchRemoteConfig()
    {
        // Use TimeSpan.Zero for testing to bypass cache, increase in production
        TimeSpan fetchTime = TimeSpan.Zero; // ⚠️ Change to TimeSpan.FromHours(2) for production
        Debug.Log($"🔥 [Firebase] Fetching Remote Config (cache expiry: {fetchTime.TotalSeconds}s)...");

        FirebaseRemoteConfig.DefaultInstance
            .FetchAsync(fetchTime)
            .ContinueWithOnMainThread(FetchComplete);
    }

    private void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Debug.LogWarning("⚠️ [Firebase] Remote Config fetch was CANCELED.");
            FallbackToDefaults();
            return;
        }

        if (fetchTask.IsFaulted)
        {
            Debug.LogError($"🚨 [Firebase] Remote Config fetch FAULTED: {fetchTask.Exception}");
            foreach (var inner in fetchTask.Exception.InnerExceptions)
            {
                Debug.LogError($"🚨 [Firebase] Inner Exception: {inner.Message}");
                Debug.LogError($"🚨 [Firebase] Stack: {inner.StackTrace}");
            }
            FallbackToDefaults();
            return;
        }

        var info = FirebaseRemoteConfig.DefaultInstance.Info;
        Debug.Log($"🔥 [Firebase] Fetch completed. Status: {info.LastFetchStatus}, Time: {info.FetchTime}");

        if (info.LastFetchStatus == LastFetchStatus.Success)
        {
            Debug.Log("✅ [Firebase] Fetch SUCCESS — now activating...");
        }
        else if (info.LastFetchStatus == LastFetchStatus.Failure)
        {
            Debug.LogError($"🚨 [Firebase] Fetch reported FAILURE. LastFetchFailureReason: {info.LastFetchFailureReason}");
        }
        else if (info.LastFetchStatus == LastFetchStatus.Pending)
        {
            Debug.LogWarning("⚠️ [Firebase] Fetch still PENDING.");
        }

        FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"🚨 [Firebase] ActivateAsync FAILED: {task.Exception}");
                    FallbackToDefaults();
                    return;
                }

                bool activated = task.Result;
                Debug.Log($"🔥 [Firebase] ActivateAsync result: {activated} (true = new values applied, false = already up-to-date)");
                ApplyValues();
            });
    }

    private void FallbackToDefaults()
    {
        Debug.LogWarning("⚠️ [Firebase] Falling back to defaults — ads will be enabled.");
        isFeatureEnabled = true;
        isRemoteConfigReady = true;
        OnRemoteConfigFetched?.Invoke();
    }

    private void ApplyValues()
    {
        var config = FirebaseRemoteConfig.DefaultInstance;
        var info = config.Info;

        // 1. Gather all keys and statistics
        var allKeys = config.Keys;
        int totalKeys = 0;
        int remoteCount = 0;
        int defaultCount = 0;
        int staticCount = 0;

        List<string> keyDetails = new List<string>();

        foreach (var key in allKeys)
        {
            totalKeys++;
            var val = config.GetValue(key);
            string sourceStr = "UNKNOWN";

            if (val.Source == ValueSource.RemoteValue)
            {
                remoteCount++;
                sourceStr = "REMOTE (Server)";
            }
            else if (val.Source == ValueSource.DefaultValue)
            {
                defaultCount++;
                sourceStr = "DEFAULT (Local)";
            }
            else if (val.Source == ValueSource.StaticValue)
            {
                staticCount++;
                sourceStr = "STATIC (SDK)";
            }

            keyDetails.Add($"   🔑 Key: '{key}' | Source: {sourceStr} | Value: '{val.StringValue}'");
        }

        // 2. Inspect key parameters specifically
        var adsValue = config.GetValue("Ads_Manager");
        isFeatureEnabled = adsValue.BooleanValue;

        // 3. Print the formatted diagnostics block
        Debug.Log("╔════════════════════════════════════════════════════════════════╗");
        Debug.Log("║                 📋 FIREBASE REMOTE CONFIG VALUES               ║");
        Debug.Log("╠════════════════════════════════════════════════════════════════╣");
        Debug.Log($"║  Last Fetch Status:   {info.LastFetchStatus,-40} ║");
        Debug.Log($"║  Last Fetch Time:     {info.FetchTime,-40} ║");
        Debug.Log($"║  Failure Reason:      {info.LastFetchFailureReason,-40} ║");
        Debug.Log($"║  Total Keys Found:    {totalKeys,-40} ║");
        Debug.Log($"║    -> Server Keys:    {remoteCount,-40} ║");
        Debug.Log($"║    -> Default Keys:   {defaultCount,-40} ║");
        Debug.Log($"║    -> Static Keys:    {staticCount,-40} ║");
        Debug.Log("╠════════════════════════════════════════════════════════════════╣");
        Debug.Log("║  DETAILED PARAMETERS LIST:                                     ║");
        foreach (var detail in keyDetails)
        {
            Debug.Log(detail);
        }
        Debug.Log("╠════════════════════════════════════════════════════════════════╣");
        Debug.Log("║  RESULT SUMMARY:                                               ║");
        Debug.Log($"║  - isFeatureEnabled (Ads_Manager):           {isFeatureEnabled,-17} ║");
        Debug.Log("╚════════════════════════════════════════════════════════════════╝");

        // ✅ Single Source of Truth for AdsManager
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.isRemoteAdsEnabled = isFeatureEnabled;
            Debug.Log($"🔥 [Firebase] AdsManager.isRemoteAdsEnabled set to: {isFeatureEnabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ [Firebase] AdsManager.Instance is NULL — cannot propagate enabled status yet.");
        }

        // 4. Source warning diagnostics
        isRemoteConfigReady = true;

        if (adsValue.Source == ValueSource.DefaultValue)
        {
            Debug.LogError("🚨 [Firebase] WARNING: 'Ads_Manager' value is still coming from Local Defaults.");
            Debug.LogError("🚨 This confirms the app is NOT reading parameters from the Firebase server.");
            Debug.LogError("🚨 Troubleshoot: Verify Remote Config parameters are PUBLISHED on the Firebase Console.");
        }
        else if (adsValue.Source == ValueSource.RemoteValue)
        {
            Debug.Log("✅ [Firebase] SUCCESS: 'Ads_Manager' value successfully retrieved from Firebase Server.");
        }

        // Dispatch events
        OnAdsPermissionGranted?.Invoke(isFeatureEnabled, isFeatureEnabled);
        OnRemoteConfigFetched?.Invoke();
    }

    public static void TrackEvent(string eventMessage)
    {
        try
        {
            FirebaseAnalytics.LogEvent(eventMessage);
            Crashlytics.Log($"Event: {eventMessage}");
            Debug.Log($"[Analytics] Logged: {eventMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError("Analytics Log Failed: " + e.Message);
        }
    }

    [ContextMenu("Force Firebase Crash")]
    public void CauseCrash()
    {
        throw new Exception("Manual Crash Test for Firebase");
    }

}