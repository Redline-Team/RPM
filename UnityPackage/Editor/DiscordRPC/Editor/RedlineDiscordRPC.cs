using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Timers;
using Redline.Runtime.DiscordRPC;

namespace Redline.Editor.DiscordRPC {
  [InitializeOnLoad]
  public class RedlineDiscordRPC {
    private static readonly DiscordRpc.RichPresence Presence = new();

    private static TimeSpan _time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
    private static long _timestamp = (long) _time.TotalSeconds;

    private static RpcState _rpcState = RpcState.Editmode;
    private static RpcState _previousState = RpcState.Editmode; // Store previous state before idle
    private static readonly string GameName = Application.productName;
    private static string _sceneName = SceneManager.GetActiveScene().name;
    
    // Idle timer
    private static Timer _idleTimer;
    private static DateTime _lastActivityTime;

    static RedlineDiscordRPC() {
      if (!EditorPrefs.GetBool("RedlineDiscordRPC", true)) return;
      RedlineLog("Starting DiscordRPC");
      var eventHandlers =
        default (DiscordRpc.EventHandlers);
      DiscordRpc.Initialize("1040718653202641006", ref eventHandlers, false, string.Empty);
      
      // Initialize idle timer
      InitializeIdleTimer();
      
      // Register for editor update to detect activity
      EditorApplication.update += DetectActivity;
      
      // Record initial activity time
      RecordActivity();
      
      UpdateDrpc();
    }
    
    private static void InitializeIdleTimer() {
      // Create and configure the idle timer
      _idleTimer = new Timer();
      _idleTimer.Elapsed += OnIdleTimerElapsed;
      _idleTimer.AutoReset = false; // Only trigger once
      
      // Set the interval based on user preferences (convert minutes to milliseconds)
      UpdateIdleTimerInterval();
      
      // Start the timer
      _idleTimer.Start();
    }
    
    /// <summary>
    /// Updates the idle timer interval based on user preferences
    /// </summary>
    public static void UpdateIdleTimerInterval() {
      if (_idleTimer != null) {
        int idleMinutes = RpcStateInfo.GetIdleTimerMinutes();
        _idleTimer.Interval = idleMinutes * 60 * 1000; // Convert minutes to milliseconds
        RedlineLog($"Idle timer set to {idleMinutes} minutes");
      }
    }
    
    /// <summary>
    /// Called when the idle timer elapses
    /// </summary>
    private static void OnIdleTimerElapsed(object sender, ElapsedEventArgs e) {
      // Only switch to idle if we're not already in idle state
      if (_rpcState != RpcState.Idle) {
        // Store the current state before switching to idle
        _previousState = _rpcState;
        
        // Switch to idle state
        UpdateState(RpcState.Idle);
        RedlineLog("Switched to idle state due to inactivity");
      }
    }
    
    /// <summary>
    /// Records user activity and resets the idle timer
    /// </summary>
    private static void RecordActivity() {
      _lastActivityTime = DateTime.Now;
      
      // If we were in idle state, switch back to previous state
      if (_rpcState == RpcState.Idle) {
        UpdateState(_previousState);
        RedlineLog("Returned from idle state due to activity");
      }
      
      // Reset and restart the idle timer
      if (_idleTimer != null) {
        _idleTimer.Stop();
        _idleTimer.Start();
      }
    }
    
    /// <summary>
    /// Detects user activity in the editor
    /// </summary>
    private static void DetectActivity() {
      // Check for mouse movement, keyboard input, etc.
      // For simplicity, we'll consider any editor update as activity
      // This could be refined to be more specific about what constitutes activity
      RecordActivity();
    }

    private static void UpdateDrpc() {
      RedlineLog("Updating everything");
      _sceneName = SceneManager.GetActiveScene().name;
      Presence.Details = $"Project: {GameName} Scene: {_sceneName}";
      Presence.State = "State: " + _rpcState.StateName();
      Presence.StartTimestamp = _timestamp;
      Presence.LargeImageKey = "rpm";
      Presence.LargeImageText = "Redline Package Manager";
      Presence.SmallImageKey = "numeri";
      Presence.SmallImageText = "By LoonieLoonz";
      
      // Add buttons for GitHub repo and website
      Presence.Button1Label = "Github Repo";
      Presence.Button1Url = "https://github.com/Redline-Team/RPM";
      Presence.Button2Label = "Website";
      Presence.Button2Url = "https://redline.arch-linux.pro";
      
      DiscordRpc.UpdatePresence(Presence);
    }

    public static void UpdateState(RpcState state) {
      RedlineLog("Updating state to '" + state.StateName() + "'");
      _rpcState = state;
      Presence.State = "State: " + state.StateName();
      DiscordRpc.UpdatePresence(Presence);
      
      // If changing to a non-idle state, record activity
      if (state != RpcState.Idle) {
        RecordActivity();
      }
    }

    public static void SceneChanged(Scene newScene) {
      RedlineLog("Updating scene name");
      _sceneName = newScene.name;
      Presence.Details = $"Project: {GameName} Scene: {_sceneName}";
      DiscordRpc.UpdatePresence(Presence);
    }

    public static void ResetTime() {
      RedlineLog("Resetting timer");
      _time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
      _timestamp = (long) _time.TotalSeconds;
      Presence.StartTimestamp = _timestamp;

      DiscordRpc.UpdatePresence(Presence);
    }

    private static void RedlineLog(string message) {
      Debug.Log("[Redline] DiscordRPC: " + message);
    }
  }
}