using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CloudStreamForms.Core.CloudStreamCore;

public static class App
{
    public const string NEXT_AIRING = "NextAiringEpisode";

    public static int ConvertDPtoPx(int dp)
    {
        return dp;
    }

    public static void ShowToast(string toast)
    {

    }

    public static int GetSizeOfJumpOnSystem()
    {
        return 1024;
    }

    static string GetKeyPath(string folder, string name = "")
    {
        string _s = ":" + folder + "-";
        if (name != "") {
            _s += name + ":";
        }
        return _s;
    }
    static Dictionary<string, object> Properties = new Dictionary<string, object>();
    public static void SetKey(string folder, string name, object value)
    {
        string path = GetKeyPath(folder, name);
        if (Properties.ContainsKey(path)) {
            Properties[path] = value;
        }
        else {
            Properties.Add(path, value);
        }
    }

    public static T GetKey<T>(string folder, string name, T defVal)
    {
        string path = GetKeyPath(folder, name);
        return GetKey<T>(path, defVal);
    }

    public static void RemoveFolder(string folder)
    {
        List<string> keys = App.GetKeysPath(folder);
        for (int i = 0; i < keys.Count; i++) {
            RemoveKey(keys[i]);
        }
    }

    public static T GetKey<T>(string path, T defVal)
    {
        if (Properties.ContainsKey(path)) {
            return (T)Properties[path];
        }
        else {
            return defVal;
        }
    }

    public static List<T> GetKeys<T>(string folder)
    {
        List<string> keyNames = GetKeysPath(folder);

        List<T> allKeys = new List<T>();
        foreach (var key in keyNames) {
            allKeys.Add((T)Properties[key]);
        }

        return allKeys;
    }

    public static int GetKeyCount(string folder)
    {
        return GetKeysPath(folder).Count;
    }
    public static List<string> GetKeysPath(string folder)
    {
        List<string> keyNames = Properties.Keys.Where(t => t.StartsWith(GetKeyPath(folder))).ToList();
        return keyNames;
    }

    public static bool KeyExists(string folder, string name)
    {
        string path = GetKeyPath(folder, name);
        return KeyExists(path);
    }
    public static bool KeyExists(string path)
    {
        return (Properties.ContainsKey(path));
    }
    public static void RemoveKey(string folder, string name)
    {
        string path = GetKeyPath(folder, name);
        RemoveKey(path);
    }
    public static void RemoveKey(string path)
    {
        if (Properties.ContainsKey(path)) {
            Properties.Remove(path);
        }
    }
}

public static class Settings
{
    public static string NativeSubShortName = "Eng";
    public static bool UseAniList = true;
    public static bool SubtitlesEnabled = false;
    public static bool DefaultDub = true;
    public static bool CacheImdb = true;
    public static bool CacheMAL = true;
    public static bool IgnoreSSLCert = true;
    public static bool PremitM3u8Download = false;
    public static bool SubtitlesClosedCaptioning = false;
    public static bool _IgnoreSSLCert = true;
    public static int malTimeout = 4000;

    public static bool IsProviderActive(string name)
    {
        return true;
    }
}

public static class MovieHelper
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static bool IsMovie(this MovieType mtype)
    {
        return mtype == MovieType.AnimeMovie || mtype == MovieType.Movie;
    }

    /// <summary>
    /// If is not null and is not ""
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsClean(this string s)
    {
        return s != null && s != "";
    }


    static readonly List<Type> types = new List<Type>() { typeof(decimal), typeof(int), typeof(string), typeof(bool), typeof(double), typeof(ushort), typeof(ulong), typeof(uint), typeof(short), typeof(short), typeof(char), typeof(long), typeof(float), };

    public static string FString(this object o, string _s = "")
    {

#if RELEASE
			return "";
#endif
#if DEBUG
        if (o == null) {
            return "Null";
        }
        Type valueType = o.GetType();

        if (o is IList) {
            IList list = (o as IList);
            string s = valueType.Name + " {";
            for (int i = 0; i < list.Count; i++) {
                s += "\n	" + _s + i + ". " + list[i].FString(_s + "	");
            }
            return s + "\n" + _s + "}";
        }


        if (!types.Contains(valueType) && !valueType.IsArray && !valueType.IsEnum) {
            string s = valueType.Name + " {";
            foreach (var field in valueType.GetFields()) {
                s += ("\n	" + _s + field.Name + " => " + field.GetValue(o).FString(_s + "	"));
            }
            return s + "\n" + _s + "}";
        }
        else {
            if (valueType.IsArray) {
                int _count = 0;
                var enu = ((o) as IEnumerable).GetEnumerator();
                string s = valueType.Name + " {";
                while (enu.MoveNext()) {
                    s += "\n	" + _count + ". " + enu.Current.FString(_s + "	");
                    _count++;
                }
                return s + "\n" + _s + "}";
            }
            else if (valueType.IsEnum) {
                return valueType.GetEnumName(o);
            }
            else {
                return o.ToString();
            }
        }
#endif

    }

    public static string RString(this object o)
    {
        string s = "VALUE OF: ";
        foreach (var field in o.GetType().GetFields()) {
            s += ("\n" + field.Name + " => " + field.GetValue(o).ToString());
        }
        return s;
    }
}