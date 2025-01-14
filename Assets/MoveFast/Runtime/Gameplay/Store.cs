// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Oculus.Interaction.MoveFast
{
    /// <summary>
    /// Used to save persistent data. Wraps player prefs in something observable
    /// </summary>
    public static class Store
    {
        public static event Action WhenChanged;

        public static int GetInt(string key)
        {
            if (!HasKey(key)) return 0;

            var str = GetString(key);
            int.TryParse(str, out int result);
            return result;
        }

        public static void SetInt(string key, int value) => SetString(key, value.ToString());

        public static float GetFloat(string key)
        {
            if (!HasKey(key)) return 0;

            var str = GetString(key);
            float.TryParse(str, out float result);
            return result;
        }

        public static void SetFloat(string key, float value) => SetString(key, value.ToString("N1"));

        public static void SetMaxFloat(string key, float value) => SetFloat(key, Mathf.Max(value, GetFloat(key)));

        public static void SetMaxInt(string key, int value) => SetInt(key, Math.Max(value, GetInt(key)));

        public static int Increment(string key)
        {
            int value = GetInt(key) + 1;
            SetInt(key, value);
            return value;
        }

        public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            WhenChanged?.Invoke();
        }

        public static string GetString(string key) => PlayerPrefs.GetString(key);

        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    }
}
