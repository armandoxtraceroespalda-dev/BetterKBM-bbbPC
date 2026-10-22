using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// Control profiles: named sets of keyboard and mouse bindings, kept as small JSON files in
    /// %APPDATA%\Lantern Engine\profiles and shared as text: export a file or copy the JSON, paste
    /// it on any paste site, and someone else imports it from the file or the link.
    ///
    /// {
    ///   "format": "lantern-controls", "version": 3, "input_device": "keyboard_mouse",
    ///   "name": "Mouse combat", "author": "someone", "description": "…",
    ///   "bindings": { "attack": "Mouse1", "roll": "Space", "firearm": "X, Mouse2", … }
    /// }
    ///
    /// Shared files are untrusted: only the bindings are used, every input and key name is checked
    /// against the ones the launcher knows, text fields are length-limited, and anything else is
    /// dropped with a note.
    /// </summary>
    public static class ControlProfiles
    {
        public const string Format = "lantern-controls";
        /// <summary>The format name of profiles exported before 0.9.37 beta 2 (still read).</summary>
        private const string OldFormat = "bbport-controls";
        public const int Version = 3;
        public const string DarkSouls3Name = "Dark Souls 3 PC";
        public const string DarkSouls3Help = "Dark Souls 3 PC layout: left click attacks; Shift + left click performs a strong attack; right click or Ctrl fires; E interacts; R uses an item; C uses a Blood Vial; Space dodges or sprints. Scroll to cycle items and Shift + scroll to switch weapons. Keeps your controller bindings.";
        private const int MaxBytes = 64 * 1024;

        /// <summary>Pad: controller assignments for controller profiles. Keyboard profiles never apply the stored pad map.</summary>
        public sealed record Profile(string Name, string Author, string Description, Dictionary<string, string> Bindings, bool BuiltIn = false, string File = "",
            Dictionary<string, string>? Pad = null, string InputDevice = "keyboard_mouse");

        public sealed record Parsed(Profile Profile, List<string> Notes, bool DeviceExplicit = false);

        public static string Folder => Path.Combine(Paths.ConfigDir, "profiles");

        // ---------------------------------------------------------------- built-ins

        public static Dictionary<string, string> DefaultBindings() =>
            InputMap.Controls.ToDictionary(c => c.Name, c => c.Default);

        public static readonly Profile[] BuiltIns =
        {
            new(DarkSouls3Name, "Bloodborne PC", "Dark Souls III PC layout adapted to Bloodborne: left click attacks, Shift + left click strong attack, " +
                "right click or Ctrl firearm, E interact, R quick item, C Blood Vial, F transform, Space dodge/sprint, Q or middle click lock on. " +
                "Enables PC controls, mouse camera and mouse menus; keeps your controller bindings.",
                new Dictionary<string, string>(DefaultBindings())
                {
                    ["attack"] = "Mouse1",
                    ["strong_modifier"] = "none",
                    ["strong_attack"] = "Left Shift + Mouse1",
                    ["firearm"] = "Left Ctrl, Mouse3",
                    ["transform"] = "F",
                    ["lock_on"] = "Q, Mouse2",
                    ["roll"] = "Space",
                    ["blood_vial"] = "C",
                    ["use_item"] = "R",
                    ["interact"] = "E",
                    ["switch_item"] = "WheelDown",
                    ["previous_item"] = "WheelUp",
                    ["switch_right"] = "Left Shift + WheelUp",
                    ["switch_left"] = "Left Shift + WheelDown",
                    ["dpad_up"] = "T",
                    ["walk"] = "Left Alt",
                    ["l3"] = "Z",
                    ["gestures"] = "G",
                    ["personal_effects"] = "Tab",
                    ["menu"] = "Escape",
                    ["confirm"] = "Return",
                    ["back"] = "Backspace",
                }, BuiltIn: true),
            new("Controller", "Bloodborne PC", "", new Dictionary<string,string>(),
                BuiltIn: true, Pad: new Dictionary<string,string>(), InputDevice: "controller"),
        };
        // ---------------------------------------------------------------- saved profiles

        public static List<Profile> All(string? inputDevice = null)
        {
            var list = new List<Profile>(BuiltIns);
            try
            {
                if (Directory.Exists(Folder))
                    foreach (var file in Directory.GetFiles(Folder, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var parsed = Parse(File.ReadAllText(file));
                            list.Add(parsed.Profile with { File = file });
                        }
                        catch
                        {
                            // A broken file is skipped; it stays on disk for the user to look at.
                        }
                    }
            }
            catch
            {
                // Unreadable folder: the built-ins only.
            }
            return inputDevice == null ? list : list.Where(p => p.InputDevice == inputDevice).ToList();
        }

        /// <summary>Saves a profile under its name (replacing one of the same name); returns the file.</summary>
        public static string Save(Profile profile)
        {
            Directory.CreateDirectory(Folder);
            var file = Path.Combine(Folder, profile.InputDevice + "-" + SafeFileName(profile.Name) + ".json");
            // Only a profile loaded from this folder may overwrite its original legacy filename.
            if (!string.IsNullOrEmpty(profile.File) &&
                string.Equals(Path.GetDirectoryName(Path.GetFullPath(profile.File)), Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase))
                file = profile.File;
            File.WriteAllText(file, ToJson(profile), new UTF8Encoding(false));
            return file;
        }

        public static void Delete(Profile profile)
        {
            if (!profile.BuiltIn && File.Exists(profile.File))
                File.Delete(profile.File);
        }

        private static string SafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
            return clean.Length == 0 ? "profile" : clean[..Math.Min(clean.Length, 60)];
        }

        // ---------------------------------------------------------------- the current bindings

        public static Dictionary<string, string> Current(IniFile ini) => Bindings.Current(ini);

        /// <summary>Makes a profile's bindings the current ones (inputs it leaves out get the game's key).</summary>
        public static void Apply(LauncherSettings s, IniFile ini, Profile profile)
        {
            bool keyboard = profile.InputDevice == "keyboard_mouse";
            if (keyboard)
            {
                foreach (var control in InputMap.Controls)
                {
                    var value = profile.Bindings.TryGetValue(control.Name, out var v) ? v : control.Default;
                    Bindings.Set(ini, control, InputMap.Split(value));
                }
            }
            else PadMap.Apply(ini, profile.Pad);
            ini.Set("input_mode", profile.InputDevice);
            ini.Set("pc_controls", "1");
            ini.Set("mouse_camera", keyboard ? "1" : "0");
            ini.Set("mouse_menu", "1");
            ini.Set("button_prompts", keyboard ? "keyboard" : "controller");
            if (profile.BuiltIn && profile.Name == DarkSouls3Name) ini.Set("ds3_profile", "1");
            s.Set("controls_profile." + profile.InputDevice, profile.Name);
            s.Set("controls_profile", profile.Name);
        }

        // ---------------------------------------------------------------- JSON

        public static string ToJson(Profile profile)
        {
            var bindings = new JsonObject();
            foreach (var control in InputMap.Controls.Where(_ => profile.InputDevice == "keyboard_mouse"))
                bindings[control.Name] = profile.Bindings.TryGetValue(control.Name, out var v) ? v : control.Default;
            var json = new JsonObject
            {
                ["format"] = Format,
                ["version"] = Version,
                ["input_device"] = profile.InputDevice,
                ["name"] = profile.Name,
                ["author"] = profile.Author,
                ["description"] = profile.Description,
                ["bindings"] = bindings,
            };
            if (profile.Pad != null)
            {
                var pad = new JsonObject();
                foreach (var (input, buttons) in profile.Pad)
                    pad[input] = buttons;
                json["pad"] = pad;
            }
            return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
        }

        /// <summary>Reads and checks a profile; throws FormatException with a readable reason.</summary>
        public static Parsed Parse(string text)
        {
            if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
                throw new FormatException(BloodborneLauncher.Core.UiText.T("The file is too large to be a control profile."));

            JsonObject root;
            try
            {
                root = JsonNode.Parse(text.Trim().TrimStart('﻿'), documentOptions: new JsonDocumentOptions
                    { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject
                    ?? throw new FormatException(BloodborneLauncher.Core.UiText.T("This is not a control profile (expected a JSON object)."));
            }
            catch (JsonException e)
            {
                throw new FormatException(BloodborneLauncher.Core.UiText.Format("This is not valid JSON: {0}",e.Message));
            }

            if ((string?)Str(root, "format") is { } format && format != Format && format != OldFormat)
                throw new FormatException(BloodborneLauncher.Core.UiText.F($"This is a \"{format}\" file, not a control profile."));
            if (root["bindings"] is not JsonObject bindings)
                throw new FormatException(BloodborneLauncher.Core.UiText.T("This file has no \"bindings\" section."));

            var device = Str(root, "input_device") ?? "keyboard_mouse";
            if (root.ContainsKey("input_device") && Str(root, "input_device") == null || device is not ("keyboard_mouse" or "controller"))
                throw new FormatException(UiText.T("Unknown input device in the control profile."));
            var notes = new List<string>();
            var known = InputMap.Controls.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, string>();
            foreach (var (input, node) in bindings)
            {
                if (!known.TryGetValue(input, out var control))
                {
                    notes.Add(BloodborneLauncher.Core.UiText.F($"Skipped unknown input \"{Clip(input, 30)}\"."));
                    continue;
                }
                var value = node is JsonValue jv && jv.TryGetValue<string>(out var s) ? s
                    : node is JsonArray array ? string.Join(", ", array.Select(n => n?.ToString() ?? ""))
                    : null;
                if (value == null)
                {
                    notes.Add(BloodborneLauncher.Core.UiText.F($"Skipped {control.Label}: its binding is not text."));
                    continue;
                }

                var names = new List<string>();
                foreach (var name in InputMap.Split(value))
                {
                    var canonical = InputMap.Canonical(name);
                    if (canonical == null)
                        notes.Add(BloodborneLauncher.Core.UiText.F($"{control.Label}: skipped unknown key \"{Clip(name, 30)}\"."));
                    else if (!names.Contains(canonical, StringComparer.OrdinalIgnoreCase))
                        names.Add(canonical);
                }
                if(names.FirstOrDefault(BindingExpressions.IsChord) is {} chord){result[control.Name]=chord;continue;}
                // Legacy alternatives retain one key and one mouse input.
                var key = names.FirstOrDefault(n => !InputMap.IsMouse(n));
                var mouse = names.FirstOrDefault(InputMap.IsMouse);
                if (names.Count > (key != null ? 1 : 0) + (mouse != null ? 1 : 0))
                    notes.Add(BloodborneLauncher.Core.UiText.F($"{control.Label}: kept one key and one mouse input."));
                names = new[] { key, mouse }.Where(n => n != null).Select(n => n!).ToList();
                result[control.Name] = InputMap.Join(names);
            }

            // The controller layout: PS4 inputs and SDL button names the launcher offers.
            Dictionary<string, string>? padMap = null;
            if (root["pad"] is JsonObject pad)
            {
                padMap = new Dictionary<string, string>();
                var inputs = PadMap.Inputs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var buttons = PadMap.Buttons.Where(b => b.Sdl.Length > 0).Select(b => b.Sdl).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var (input, node) in pad)
                {
                    if (!inputs.Contains(input))
                    {
                        notes.Add(BloodborneLauncher.Core.UiText.F($"Controller: skipped unknown input \"{Clip(input, 30)}\"."));
                        continue;
                    }
                    var value = node is JsonValue jv && jv.TryGetValue<string>(out var s) ? s : "";
                    var names = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(n => buttons.Contains(n)).Select(n => n.ToLowerInvariant()).Distinct().Take(4).ToList();
                    padMap[input.ToLowerInvariant()] = string.Join(", ", names);
                }
            }

            if (device == "keyboard_mouse" && result.Count == 0 || device == "controller" && padMap == null)
                throw new FormatException(UiText.T("This profile binds nothing the launcher knows."));
            var nameText = Clip(Str(root, "name") ?? "", 60).Trim();
            return new Parsed(new Profile(
                nameText.Length > 0 ? nameText : "Imported profile",
                Clip(Str(root, "author") ?? "", 60).Trim(),
                Clip(Str(root, "description") ?? "", 400).Trim(),
                result, Pad: padMap, InputDevice: device), notes, root.ContainsKey("input_device"));
        }

        private static string? Str(JsonObject root, string key) =>
            root[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

        /// <summary>At most n characters, with control characters removed.</summary>
        private static string Clip(string text, int n)
        {
            var clean = new string(text.Where(c => !char.IsControl(c)).ToArray());
            return clean.Length <= n ? clean : clean[..n] + "…";
        }

        // ---------------------------------------------------------------- links

        /// <summary>The raw-text address of a paste or file link, for the sites people share on.</summary>
        public static Uri RawUrl(string link)
        {
            link = link.Trim();
            if (!link.Contains("://")) link = "https://" + link;
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new FormatException(BloodborneLauncher.Core.UiText.T("That is not a web link."));

            var host = uri.Host.ToLowerInvariant();
            var path = uri.AbsolutePath.TrimEnd('/');
            Match m;
            string? raw = null;

            if (host is "pastebin.com" && (m = Regex.Match(path, @"^/(?:raw/)?([A-Za-z0-9]+)$")).Success)
                raw = $"https://pastebin.com/raw/{m.Groups[1].Value}";
            else if (host is "gist.github.com" && !path.Contains("/raw"))
                raw = $"https://gist.github.com{path}/raw";
            else if (host is "github.com" && (m = Regex.Match(path, @"^/([^/]+)/([^/]+)/blob/(.+)$")).Success)
                raw = $"https://raw.githubusercontent.com/{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}";
            else if (host is "rentry.co" or "rentry.org" && !path.EndsWith("/raw"))
                raw = $"https://{host}{path}/raw";
            else if (host is "hastebin.com" or "hastebin.skyra.pw" or "toptal.com" && (m = Regex.Match(path, @"/(?:raw/)?([A-Za-z0-9]+)(?:\.\w+)?$")).Success)
                raw = host == "toptal.com"
                    ? $"https://www.toptal.com/developers/hastebin/raw/{m.Groups[1].Value}"
                    : $"https://{host}/raw/{m.Groups[1].Value}";
            else if (host is "pastes.dev" && (m = Regex.Match(path, @"^/([A-Za-z0-9]+)$")).Success)
                raw = $"https://api.pastes.dev/{m.Groups[1].Value}";
            else if (host is "dpaste.org" && !path.EndsWith("/raw"))
                raw = $"https://dpaste.org{path}/raw";
            else if (host is "dpaste.com" && !path.EndsWith(".txt"))
                raw = $"https://dpaste.com{path}.txt";

            return raw != null ? new Uri(raw) : uri;
        }

        /// <summary>Downloads a shared profile (text only, at most 64 KB) and checks it.</summary>
        public static async Task<Parsed> DownloadAsync(string link)
        {
            var uri = RawUrl(link);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BloodborneLauncher/1.0");
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                throw new FormatException(BloodborneLauncher.Core.UiText.F($"The link answered {(int)response.StatusCode} {response.ReasonPhrase}."));
            if (response.Content.Headers.ContentLength > MaxBytes)
                throw new FormatException(BloodborneLauncher.Core.UiText.T("That link is too large to be a control profile."));

            await using var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[MaxBytes + 1];
            var read = 0;
            int n;
            while (read < buffer.Length && (n = await stream.ReadAsync(buffer.AsMemory(read))) > 0)
                read += n;
            if (read > MaxBytes)
                throw new FormatException(BloodborneLauncher.Core.UiText.T("That link is too large to be a control profile."));

            var text = Encoding.UTF8.GetString(buffer, 0, read);
            if (text.TrimStart().StartsWith('<'))
                throw new FormatException(BloodborneLauncher.Core.UiText.T("That link shows a web page, not the raw profile. Use the paste's \"raw\" link."));
            return Parse(text);
        }
    }
}
