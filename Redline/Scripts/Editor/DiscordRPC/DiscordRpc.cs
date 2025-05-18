using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;

namespace Redline.Scripts.Editor.DiscordRPC {
  public abstract class DiscordRpc {
    // Static constructor to ensure the native library is loaded before any P/Invoke calls
    static DiscordRpc() {
      // Reference the loader to trigger its static constructor
      var loaderType = typeof(DiscordRpcNativeLoader);
      UnityEngine.Debug.Log("[Redline] DiscordRPC: Native library loader initialized");
    }
    [MonoPInvokeCallback(typeof (OnReadyInfo))]
    public static void ReadyCallback(ref DiscordUser connectedUser) {
      Callbacks.ReadyCallback(ref connectedUser);
    }

    public delegate void OnReadyInfo(ref DiscordUser connectedUser);

    [MonoPInvokeCallback(typeof (OnDisconnectedInfo))]
    public static void DisconnectedCallback(int errorCode, string message) {
      Callbacks.DisconnectedCallback(errorCode, message);
    }

    public delegate void OnDisconnectedInfo(int errorCode, string message);

    [MonoPInvokeCallback(typeof (OnErrorInfo))]
    public static void ErrorCallback(int errorCode, string message) {
      Callbacks.ErrorCallback(errorCode, message);
    }

    public delegate void OnErrorInfo(int errorCode, string message);

    [MonoPInvokeCallback(typeof (OnJoinInfo))]
    public static void JoinCallback(string secret) {
      Callbacks.JoinCallback(secret);
    }

    public delegate void OnJoinInfo(string secret);

    [MonoPInvokeCallback(typeof (OnSpectateInfo))]
    public static void SpectateCallback(string secret) {
      Callbacks.SpectateCallback(secret);
    }

    public delegate void OnSpectateInfo(string secret);

    [MonoPInvokeCallback(typeof (OnRequestInfo))]
    public static void RequestCallback(ref DiscordUser request) {
      Callbacks.RequestCallback(ref request);
    }

    public delegate void OnRequestInfo(ref DiscordUser request);

    private static EventHandlers Callbacks {
      get;
      set;
    }

    public struct EventHandlers {
      internal OnReadyInfo ReadyCallback;
      internal OnDisconnectedInfo DisconnectedCallback;
      internal OnErrorInfo ErrorCallback;
      internal OnJoinInfo JoinCallback;
      internal OnSpectateInfo SpectateCallback;
      internal OnRequestInfo RequestCallback;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RichPresenceStruct {
      public IntPtr state; /* max 128 bytes */
      public IntPtr details; /* max 128 bytes */
      public long startTimestamp;
      public long endTimestamp;
      public IntPtr largeImageKey; /* max 32 bytes */
      public IntPtr largeImageText; /* max 128 bytes */
      public IntPtr smallImageKey; /* max 32 bytes */
      public IntPtr smallImageText; /* max 128 bytes */
      public IntPtr partyId; /* max 128 bytes */
      public int partySize;
      public int partyMax;
      public IntPtr matchSecret; /* max 128 bytes */
      public IntPtr joinSecret; /* max 128 bytes */
      public IntPtr spectateSecret; /* max 128 bytes */
      public bool instance;
      public IntPtr button1Label; /* max 32 bytes */
      public IntPtr button1Url; /* max 512 bytes */
      public IntPtr button2Label; /* max 32 bytes */
      public IntPtr button2Url; /* max 512 bytes */
    }

    [Serializable]
    public struct DiscordUser {
      public string userId;
      public string username;
      public string discriminator;
      public string avatar;
    }

    public enum Reply {
    }

    public static void Initialize(string applicationId, ref EventHandlers handlers, bool autoRegister,
      string optionalSteamId) {
      Callbacks = handlers;

      var staticEventHandlers = new EventHandlers();
      staticEventHandlers.ReadyCallback += ReadyCallback;
      staticEventHandlers.DisconnectedCallback += DisconnectedCallback;
      staticEventHandlers.ErrorCallback += ErrorCallback;
      staticEventHandlers.JoinCallback += JoinCallback;
      staticEventHandlers.SpectateCallback += SpectateCallback;
      staticEventHandlers.RequestCallback += RequestCallback;

      InitializeInternal(applicationId, ref staticEventHandlers, autoRegister, optionalSteamId);
    }

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
    #endif
    private static extern void InitializeInternal(string applicationId, ref EventHandlers handlers, bool autoRegister,
      string optionalSteamId);

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
    #endif
    public static extern void Shutdown();

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
    #endif
    public static extern void RunCallbacks();

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
    #endif
    private static extern void UpdatePresenceNative(ref RichPresenceStruct presence);

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_ClearPresence", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_ClearPresence", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_ClearPresence", CallingConvention = CallingConvention.Cdecl)]
    #endif
    public static extern void ClearPresence();

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_Respond", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_Respond", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_Respond", CallingConvention = CallingConvention.Cdecl)]
    #endif
    public static extern void Respond(string userId, Reply reply);

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("discord-rpc", EntryPoint = "Discord_UpdateHandlers", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libdiscord-rpc", EntryPoint = "Discord_UpdateHandlers", CallingConvention = CallingConvention.Cdecl)]
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    [DllImport("discord-rpc", EntryPoint = "Discord_UpdateHandlers", CallingConvention = CallingConvention.Cdecl)]
    #endif
    public static extern void UpdateHandlers(ref EventHandlers handlers);

    public static void UpdatePresence(RichPresence presence) {
      var presencestruct = presence.GetStruct();
      UpdatePresenceNative(ref presencestruct);
      presence.FreeMem();
    }

    public class RichPresence {
      private RichPresenceStruct _presence;
      private readonly List < IntPtr > _buffers = new(10);

      public string State; /* max 128 bytes */
      public string Details; /* max 128 bytes */
      public long StartTimestamp;
      public long EndTimestamp;
      public string LargeImageKey; /* max 32 bytes */
      public string LargeImageText; /* max 128 bytes */
      public string SmallImageKey; /* max 32 bytes */
      public string SmallImageText; /* max 128 bytes */
      public string PartyId; /* max 128 bytes */
      public int PartySize;
      public int PartyMax;
      public string MatchSecret; /* max 128 bytes */
      public string JoinSecret; /* max 128 bytes */
      public string SpectateSecret; /* max 128 bytes */
      public bool Instance;
      public string Button1Label; /* max 32 bytes */
      public string Button1Url; /* max 512 bytes */
      public string Button2Label; /* max 32 bytes */
      public string Button2Url; /* max 512 bytes */

      /// <summary>
      /// Get the <see cref="RichPresenceStruct"/> reprensentation of this instance
      /// </summary>
      /// <returns><see cref="RichPresenceStruct"/> reprensentation of this instance</returns>
      internal RichPresenceStruct GetStruct() {
        if (_buffers.Count > 0) {
          FreeMem();
        }

        _presence.state = StrToPtr(State);
        _presence.details = StrToPtr(Details);
        _presence.startTimestamp = StartTimestamp;
        _presence.endTimestamp = EndTimestamp;
        _presence.largeImageKey = StrToPtr(LargeImageKey);
        _presence.largeImageText = StrToPtr(LargeImageText);
        _presence.smallImageKey = StrToPtr(SmallImageKey);
        _presence.smallImageText = StrToPtr(SmallImageText);
        _presence.partyId = StrToPtr(PartyId);
        _presence.partySize = PartySize;
        _presence.partyMax = PartyMax;
        _presence.matchSecret = StrToPtr(MatchSecret);
        _presence.joinSecret = StrToPtr(JoinSecret);
        _presence.spectateSecret = StrToPtr(SpectateSecret);
        _presence.instance = Instance;
        _presence.button1Label = StrToPtr(Button1Label);
        _presence.button1Url = StrToPtr(Button1Url);
        _presence.button2Label = StrToPtr(Button2Label);
        _presence.button2Url = StrToPtr(Button2Url);

        return _presence;
      }

      /// <summary>
      /// Returns a pointer to a representation of the given string with a size of maxbytes
      /// </summary>
      /// <param name="input">String to convert</param>
      /// <returns>Pointer to the UTF-8 representation of <see cref="input"/></returns>
      private IntPtr StrToPtr(string input) {
        if (string.IsNullOrEmpty(input)) return IntPtr.Zero;
        var convbytecnt = Encoding.UTF8.GetByteCount(input);
        var buffer = Marshal.AllocHGlobal(convbytecnt + 1);
        for (var i = 0; i < convbytecnt + 1; i++) {
          Marshal.WriteByte(buffer, i, 0);
        }

        _buffers.Add(buffer);
        Marshal.Copy(Encoding.UTF8.GetBytes(input), 0, buffer, convbytecnt);
        return buffer;
      }

      /// <summary>
      /// Convert string to UTF-8 and add null termination
      /// </summary>
      /// <param name="toconv">string to convert</param>
      /// <returns>UTF-8 representation of <see cref="toconv"/> with added null termination</returns>
      private static string StrToUtf8NullTerm(string toconv) {
        var str = toconv.Trim();
        var bytes = Encoding.Default.GetBytes(str);
        if (bytes.Length > 0 && bytes[^1] != 0) {
          str += "\0\0";
        }

        return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(str));
      }

      /// <summary>
      /// Free the allocated memory for conversion to <see cref="RichPresenceStruct"/>
      /// </summary>
      internal void FreeMem() {
        for (var i = _buffers.Count - 1; i >= 0; i--) {
          Marshal.FreeHGlobal(_buffers[i]);
          _buffers.RemoveAt(i);
        }
      }
    }
  }
}