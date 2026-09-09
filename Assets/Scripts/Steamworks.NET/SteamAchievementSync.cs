#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using System.Collections.Generic;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using Steamworks;
using UnityEngine;

/// <summary>Reconciles Unity and external-game saves with Steam achievements.</summary>
public sealed class SteamAchievementSync : MonoBehaviour
{
    private GameSaveRepository _saves;
    private readonly HashSet<string> _pending = new();
    private readonly HashSet<string> _confirmed = new();
    private readonly HashSet<string> _warnings = new();
    private readonly List<string> _inFlight = new();
    private Callback<UserStatsStored_t> _stored;
    private float _nextPoll;
    private float _nextStore;
    private bool _storing;
    private float _storeStarted;

    private void OnEnable()
    {
        _saves = new GameSaveRepository();
        _stored = Callback<UserStatsStored_t>.Create(OnStatsStored);
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < _nextPoll) return;
        _nextPoll = Time.realtimeSinceStartup + 5f;

        // The external executable writes this file too, so observing only Unity saves
        // would miss item pickups and extra-stage progress. Never write back here.
        foreach (var id in SteamAchievementRules.GetUnlockedIds(_saves.Load()))
            if (!_confirmed.Contains(id)) _pending.Add(id);

        if (_storing && Time.realtimeSinceStartup - _storeStarted >= 60f)
        {
            _storing = false;
            Debug.LogWarning("[Steam] StoreStats callback timed out; retrying.");
        }
        if (_storing || _pending.Count == 0 || Time.realtimeSinceStartup < _nextStore) return;
        _inFlight.Clear();
        foreach (var id in _pending)
        {
            if (!SteamUserStats.GetAchievement(id, out var unlocked))
            {
                if (_warnings.Add(id))
                    Debug.LogWarning($"[Steam] Achievement unavailable: {id}. Check the App ID and published achievement definitions.");
                continue;
            }

            if (unlocked || SteamUserStats.SetAchievement(id)) _inFlight.Add(id);
        }

        if (_inFlight.Count == 0) return;
        _nextStore = Time.realtimeSinceStartup + 30f;
        // This SDK synchronizes stats before launch; RequestCurrentStats was removed.
        // Keep pending IDs until the server acknowledges StoreStats, even when the
        // local Steam cache already reports them unlocked after a failed upload.
        _storing = SteamUserStats.StoreStats();
        _storeStarted = Time.realtimeSinceStartup;
        if (!_storing) Debug.LogWarning("[Steam] StoreStats failed; retrying in 30 seconds.");
    }

    private void OnStatsStored(UserStatsStored_t result)
    {
        if (result.m_nGameID != SteamUtils.GetAppID().m_AppId) return;
        if (!_storing) return;
        _storing = false;
        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogWarning($"[Steam] Achievement upload failed: {result.m_eResult}. Will retry.");
            return;
        }

        foreach (var id in _inFlight)
        {
            _pending.Remove(id);
            _confirmed.Add(id);
        }
        _inFlight.Clear();
    }

    private void OnDisable()
    {
        _stored?.Dispose();
        _stored = null;
    }
}
#endif
