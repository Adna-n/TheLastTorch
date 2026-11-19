// AudioManager: plays every sound in the game. Holds a list of SoundId -> clips (filled by the
// scene builder with the generated WAVs), a pool of AudioSources for one-shot effects (2D or 3D
// at a position), and one looping source for ambience/music. Volumes = GameSettings x PlayerOptions.
// Missing clips are ignored silently, so the game still runs before audio is generated.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTorch
{
    public enum SoundId
    {
        None, TorchLoop, AmbientDrone, Footstep, PickupGem, PickupOil, PickupKey, DoorGrind, DoorSlam,
        SpikeUp, DartWhoosh, BoulderRoll, Hurt, HeartHum, CollapseRumble, LowFuelBeat,
        WinJingle, LoseSting, UiClick, UiHover
    }

    [Serializable]
    public class SoundEntry
    {
        public SoundId id;
        [Tooltip("Several clips = one is picked at random (e.g. footsteps).")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public List<SoundEntry> sounds = new List<SoundEntry>();

        readonly Dictionary<SoundId, SoundEntry> lookup = new Dictionary<SoundId, SoundEntry>();
        AudioSource[] pool;
        int nextSource;
        AudioSource loopSource;

        float SfxVolume => GameManager.Settings.sfxVolume * PlayerOptions.Current.sfxVolume;

        void Awake()
        {
            Instance = this;
            foreach (var entry in sounds)
                if (entry != null) lookup[entry.id] = entry;

            int size = Mathf.Max(1, GameManager.Settings.sfxPoolSize);
            pool = new AudioSource[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject("SFX_" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.rolloffMode = AudioRolloffMode.Linear;
                pool[i] = src;
            }

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.spatialBlend = 0f;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Returns a clip for the id (random variant), or null if none is assigned.</summary>
        public AudioClip GetClip(SoundId id, out float entryVolume)
        {
            entryVolume = 1f;
            if (!lookup.TryGetValue(id, out var entry) || entry.clips == null || entry.clips.Length == 0) return null;
            entryVolume = entry.volume;
            return entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
        }

        /// <summary>Plays a 2D (non-positional) sound, e.g. UI or pickups.</summary>
        public void Play(SoundId id, float volume = 1f, float pitch = 1f)
        {
            PlayInternal(id, Vector3.zero, false, volume, pitch);
        }

        /// <summary>Plays a 3D sound at a world position (gets quieter with distance).</summary>
        public void PlayAt(SoundId id, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            PlayInternal(id, position, true, volume, pitch);
        }

        void PlayInternal(SoundId id, Vector3 position, bool spatial, float volume, float pitch)
        {
            var clip = GetClip(id, out float entryVolume);
            if (clip == null || pool == null) return;

            var src = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Length;

            src.transform.position = position;
            src.spatialBlend = spatial ? 1f : 0f;
            src.maxDistance = GameManager.Settings.sfxMaxDistance;
            src.pitch = pitch;
            src.volume = volume * entryVolume * SfxVolume;
            src.clip = clip;
            src.Play();
        }

        /// <summary>Starts the ambience/music loop (replaces any current loop).</summary>
        public void PlayLoop(SoundId id)
        {
            var clip = GetClip(id, out float entryVolume);
            if (clip == null) return;
            loopSource.clip = clip;
            loopSource.volume = entryVolume * GameManager.Settings.musicVolume;
            loopSource.Play();
        }

        public void StopLoop() { if (loopSource != null) loopSource.Stop(); }

        /// <summary>Gives a looping clip to a source owned by another object (torch crackle, heart hum...).</summary>
        public void SetupLoopSource(AudioSource source, SoundId id, float volume = 1f, bool playNow = true)
        {
            if (source == null) return;
            var clip = GetClip(id, out float entryVolume);
            if (clip == null) return;
            source.clip = clip;
            source.loop = true;
            source.volume = volume * entryVolume * SfxVolume;
            if (playNow) source.Play();
        }
    }
}
