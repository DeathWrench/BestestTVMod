using System;
using Unity.Collections;
using Unity.Netcode;

namespace BestestTVModPlugin
{
    public static class NetSync
    {
        private const string MsgName = "BestestTVMod.TVState";

        private const byte KIND_FULL_STATE = 1;
        private const byte KIND_SEEK = 2;
        private const byte KIND_PAUSE = 4;

        public static bool IsApplyingRemote { get; private set; }

        private static bool registered = false;
        private static NetworkManager registeredNm = null;
        private static object registeredCmm = null;
        private static bool wasConnected = false;
        private static float lastRegistrationAttempt = -10f;
        private const float REGISTRATION_RETRY_INTERVAL = 1.0f;

        private static string StateSnapshot()
        {
            var tv = TVScriptPatches.LastTVInstance;
            var nm = NetworkManager.Singleton;
            string tvHasNm = (tv != null) ? (tv.NetworkManager != null ? "yes" : "no") : "tv=null";
            string nmInfo = "null";
            if (nm != null)
            {
                nmInfo = $"id={nm.GetInstanceID()},IsServer={nm.IsServer},IsClient={nm.IsClient},IsHost={nm.IsHost},cmm={(nm.CustomMessagingManager != null ? "ok" : "null")}";
            }
            return $"registered={registered}, wasConnected={wasConnected}, LastTV={(tv != null ? "set" : "null")}, tvHasNm={tvHasNm}, Singleton=[{nmInfo}], regNmId={(registeredNm != null ? registeredNm.GetInstanceID().ToString() : "null")}, regCmmSet={(registeredCmm != null)}";
        }

        private static void LogDebug(string msg)
        {
            if (ConfigManager.enableLogging.Value)
                BestestTVModPlugin.Log.LogInfo($"[NetSync] {msg}");
        }

        private static TVScript ResolveTV()
        {
            var tv = TVScriptPatches.LastTVInstance;
            if (tv != null && tv.NetworkManager != null) return tv;
            try
            {
                var found = UnityEngine.Object.FindObjectOfType<TVScript>();
                if (found != null)
                {
                    TVScriptPatches.LastTVInstance = found;
                    return found;
                }
            }
            catch { }
            return null;
        }

        private static NetworkManager ResolveNM()
        {
            var tv = ResolveTV();
            if (tv != null && tv.NetworkManager != null) return tv.NetworkManager;

            var singleton = NetworkManager.Singleton;
            if (singleton != null) return singleton;

            try
            {
                var found = UnityEngine.Object.FindObjectOfType<NetworkManager>();
                if (found != null) return found;
            }
            catch { }
            return null;
        }

        public static bool IsHost
        {
            get
            {
                return PlayerIsHost(ResolveTV());
            }
        }

        public static bool PlayerIsHost(TVScript instance)
        {
            if (instance != null && instance.NetworkManager != null)
            {
                return instance.NetworkManager.IsHost;
            }
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsHost;
        }

        public static bool IsNetworkReady
        {
            get
            {
                var nm = ResolveNM();
                return nm != null
                       && (nm.IsServer || nm.IsClient)
                       && nm.CustomMessagingManager != null;
            }
        }

        public static void EnsureRegistered()
        {
            var nm = ResolveNM();
            if (nm == null)
            {
                LogDebug($"EnsureRegistered: NM is null. {StateSnapshot()}");
                return;
            }
            if (nm.CustomMessagingManager == null)
            {
                LogDebug($"EnsureRegistered: CMM is null on NM. {StateSnapshot()}");
                return;
            }
            if (!(nm.IsServer || nm.IsClient))
            {
                LogDebug($"EnsureRegistered: NM is not server or client (disconnected). {StateSnapshot()}");
                return;
            }

            var cmm = nm.CustomMessagingManager;

            bool nmChanged = !ReferenceEquals(registeredNm, nm);
            bool cmmChanged = !ReferenceEquals(registeredCmm, cmm);
            if (registered && (nmChanged || cmmChanged))
            {
                LogDebug($"EnsureRegistered: stale state detected (nmChanged={nmChanged}, cmmChanged={cmmChanged}). Forcing re-register. {StateSnapshot()}");
                registered = false;
                registeredNm = null;
                registeredCmm = null;
            }

            if (registered) return;

            try
            {
                try { cmm.UnregisterNamedMessageHandler(MsgName); } catch { }

                cmm.RegisterNamedMessageHandler(MsgName, OnMessageReceived);
                registered = true;
                registeredNm = nm;
                registeredCmm = cmm;
                LogDebug($"Registered named message handler. {StateSnapshot()}");
            }
            catch (Exception e)
            {
                if (e.Message != null && e.Message.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    registered = true;
                    registeredNm = nm;
                    registeredCmm = cmm;
                    LogDebug($"Handler was already registered; recovered. {StateSnapshot()}");
                    return;
                }
                if (ConfigManager.enableLogging.Value)
                    BestestTVModPlugin.Log.LogWarning($"[NetSync] Failed to register handler: {e.Message}. {StateSnapshot()}");
            }
        }

        public static void Tick()
        {
            if (!ConfigManager.enableSync.Value) return;
            if (registered && wasConnected && registeredNm != null)
            {
                var sing = NetworkManager.Singleton;
                if (ReferenceEquals(sing, registeredNm)
                    && sing != null
                    && (sing.IsServer || sing.IsClient)
                    && ReferenceEquals(sing.CustomMessagingManager, registeredCmm))
                {
                    return;
                }
            }

            var nm = ResolveNM();
            bool connectedNow = nm != null && (nm.IsServer || nm.IsClient);

            if (connectedNow && !wasConnected)
            {
                LogDebug($"Detected reconnect transition. Forcing re-register. {StateSnapshot()}");
                registered = false;
                registeredNm = null;
                registeredCmm = null;
            }
            if (!connectedNow && wasConnected)
            {
                LogDebug($"Detected disconnect transition. Clearing state. {StateSnapshot()}");
                registered = false;
                registeredNm = null;
                registeredCmm = null;
            }
            wasConnected = connectedNow;

            if (registered && nm != null)
            {
                bool nmChanged = !ReferenceEquals(registeredNm, nm);
                bool cmmChanged = !ReferenceEquals(registeredCmm, nm.CustomMessagingManager);
                if (nmChanged || cmmChanged)
                {
                    LogDebug($"Tick: NM/CMM identity changed (nmChanged={nmChanged}, cmmChanged={cmmChanged}). {StateSnapshot()}");
                    registered = false;
                    registeredNm = null;
                    registeredCmm = null;
                }
            }

            if (registered) return;
            if (UnityEngine.Time.unscaledTime - lastRegistrationAttempt < REGISTRATION_RETRY_INTERVAL) return;
            lastRegistrationAttempt = UnityEngine.Time.unscaledTime;

            EnsureRegistered();
        }

        public static void Unregister()
        {
            if (!registered)
            {
                LogDebug($"Unregister: not currently registered. {StateSnapshot()}");
                return;
            }
            try
            {
                var nm = registeredNm ?? ResolveNM();
                if (nm != null && nm.CustomMessagingManager != null)
                    nm.CustomMessagingManager.UnregisterNamedMessageHandler(MsgName);
                LogDebug($"Unregistered named message handler.");
            }
            catch (Exception e) { LogDebug($"Unregister threw: {e.Message}"); }
            registered = false;
            registeredNm = null;
            registeredCmm = null;
            wasConnected = false;
        }

        public static void BroadcastFullState(int tvIndex, double currentTime, bool tvOn)
        {
            SendState(KIND_FULL_STATE, VideoManager.Seed, tvIndex, currentTime, tvOn, TVScriptPatches.tvIsPaused);
        }

        public static void BroadcastSeek(int tvIndex, double currentTime, bool tvOn)
        {
            SendState(KIND_SEEK, VideoManager.Seed, tvIndex, currentTime, tvOn, TVScriptPatches.tvIsPaused);
        }

        public static void BroadcastPause(int tvIndex, double currentTime, bool tvOn, bool isPaused)
        {
            SendState(KIND_PAUSE, VideoManager.Seed, tvIndex, currentTime, tvOn, isPaused);
        }

        private static void SendState(byte kind, int seed, int tvIndex, double currentTime, bool tvOn, bool isPaused)
        {
            if (!ConfigManager.enableSync.Value)
            {
                LogDebug($"SendState({kind}): sync disabled in config.");
                return;
            }
            EnsureRegistered();

            var nm = ResolveNM();
            if (nm == null)
            {
                LogDebug($"SendState({kind}): NM is null, dropping. {StateSnapshot()}");
                return;
            }
            if (!(nm.IsServer || nm.IsClient))
            {
                LogDebug($"SendState({kind}): NM not connected, dropping. {StateSnapshot()}");
                return;
            }
            var cmm = nm.CustomMessagingManager;
            if (cmm == null)
            {
                LogDebug($"SendState({kind}): CMM null, dropping. {StateSnapshot()}");
                return;
            }
            if (!registered)
            {
                LogDebug($"SendState({kind}): handler not registered, dropping. {StateSnapshot()}");
                return;
            }

            // 1 (kind) + 4 (seed) + 4 (index) + 8 (time) + 1 (tvOn) + 1 (isPaused) = 19 bytes
            const int size = 1 + 4 + 4 + 8 + 1 + 1;

            try
            {
                using (var writer = new FastBufferWriter(size, Allocator.Temp))
                {
                    writer.WriteValueSafe(kind);
                    writer.WriteValueSafe(seed);
                    writer.WriteValueSafe(tvIndex);
                    writer.WriteValueSafe(currentTime);
                    writer.WriteValueSafe(tvOn);
                    writer.WriteValueSafe(isPaused);

                    if (nm.IsServer)
                    {
                        // Host -> all clients
                        cmm.SendNamedMessageToAll(MsgName, writer, NetworkDelivery.Reliable);
                        LogDebug($"Sent kind={kind} to all clients (host). seed={seed}, idx={tvIndex}");
                    }
                    else
                    {
                        // Client -> host (host re-broadcasts if needed)
                        cmm.SendNamedMessage(MsgName, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
                        LogDebug($"Sent kind={kind} to host (client). seed={seed}, idx={tvIndex}");
                    }
                }
            }
            catch (Exception e)
            {
                if (ConfigManager.enableLogging.Value)
                    BestestTVModPlugin.Log.LogWarning($"[NetSync] Send failed: {e.Message}");
            }
        }

        private static void OnMessageReceived(ulong senderClientId, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out byte kind);
                reader.ReadValueSafe(out int seed);
                reader.ReadValueSafe(out int tvIndex);
                reader.ReadValueSafe(out double currentTime);
                reader.ReadValueSafe(out bool tvOn);
                reader.ReadValueSafe(out bool isPaused);

                var localTV = ResolveTV();
                LogDebug($"Received kind={kind} from client {senderClientId}. seed={seed}, idx={tvIndex}, t={currentTime:F2}, on={tvOn}, paused={isPaused}. {StateSnapshot()}");

                IsApplyingRemote = true;
                bool seedChanged = false;
                try
                {
                    if (VideoManager.Seed != seed)
                    {
                        VideoManager.SetSeed(seed);
                        seedChanged = true;
                    }

                    if (kind == KIND_FULL_STATE)
                    {
                        TVScriptPatches.ApplyRemoteState(tvIndex, currentTime, tvOn, isPaused);
                    }
                    else if (kind == KIND_SEEK)
                    {
                        TVScriptPatches.ApplyRemoteSeek(currentTime);
                    }
                    else if (kind == KIND_PAUSE)
                    {
                        TVScriptPatches.ApplyRemotePause(currentTime, isPaused);
                    }
                }
                finally
                {
                    IsApplyingRemote = false;
                }

                if (seedChanged)
                {
                    BestestTVModPlugin.ShowHudTip(
                        "Shuffle",
                        $"Another player shuffled the videos.\nNew seed: {seed}",
                        "ShuffleTip");
                }

                var nmLocal = ResolveNM();
                if (PlayerIsHost(localTV) && nmLocal != null && senderClientId != nmLocal.LocalClientId)
                {
                    if (kind == KIND_FULL_STATE)
                        BroadcastFullState(tvIndex, currentTime, tvOn);
                    else if (kind == KIND_SEEK)
                        BroadcastSeek(tvIndex, currentTime, tvOn);
                    else if (kind == KIND_PAUSE)
                        BroadcastPause(tvIndex, currentTime, tvOn, isPaused);
                }
            }
            catch (Exception e)
            {
                if (ConfigManager.enableLogging.Value)
                    BestestTVModPlugin.Log.LogWarning($"[NetSync] Receive failed: {e.Message}");
            }
        }

    }
}
