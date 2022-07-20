using MelonLoader;
using PersonAvoider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.Management;
using System.Reflection;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using UnhollowerRuntimeLib;

[assembly: MelonInfo(typeof(PersonAvoiderMod), "Person Avoider", "1.0.0", "ITR", "https://github.com/ITR13/VrcPersonAvoiderMod")]
[assembly: MelonGame("VRChat", "VRChat")]
namespace PersonAvoider
{
    public class PersonAvoiderMod : MelonMod
    {
        private const string CustomJoinSoundFileName = "UserData/PA-Join.ogg";
        private const string AlertListFileName = "UserData/AlertList.txt";
        private string AlertListPath;
        private readonly List<string> CurrentNames = new List<string>();

        private Image _joinedImage;
        private AudioSource _joinedAudioSource;
        private Text _joinedText;

        private AssetBundle _assetBundle;
        private Sprite _joinSprite;
        private AudioClip _JoinClip;

        private AudioMixerGroup _uiAudioMixerGroup;
        private static Func<VRCUiManager> _getUiManager;

        private DateTime _alertListLastUpdate = DateTime.MinValue;
        private HashSet<string> _alertOn = new HashSet<string>();

        public override void OnApplicationStart()
        {
            AlertListPath = Path.Combine(Environment.CurrentDirectory, AlertListFileName);
            if (!File.Exists(AlertListPath))
            {
                File.Create(AlertListPath);
            }

            _getUiManager = (Func<VRCUiManager>)Delegate.CreateDelegate(
                typeof(Func<VRCUiManager>),
                typeof(VRCUiManager)
                    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .First(it => it.PropertyType == typeof(VRCUiManager))
                    .GetMethod
                );

            PersonAvoiderSettings.RegisterSettings();

            MelonCoroutines.Start(InitThings());
        }

        private T LoadAsset<T>(string name) where T : UnhollowerBaseLib.Il2CppObjectBase
        {
            var path = $"Assets/{name}";
            return _assetBundle.LoadAsset_Internal(path, Il2CppType.Of<T>()).Cast<T>();
        }


        public IEnumerator InitThings()
        {
            MelonDebug.Msg("Waiting for init");

            while (ReferenceEquals(NetworkManager.field_Internal_Static_NetworkManager_0, null)) yield return null;
            while (ReferenceEquals(VRCAudioManager.field_Private_Static_VRCAudioManager_0, null)) yield return null;
            while (ReferenceEquals(_getUiManager(), null)) yield return null;

            var audioManager = VRCAudioManager.field_Private_Static_VRCAudioManager_0;

            _uiAudioMixerGroup = new[]
            {
                audioManager.field_Public_AudioMixerGroup_0, audioManager.field_Public_AudioMixerGroup_1,
                audioManager.field_Public_AudioMixerGroup_2
            }.FirstOrDefault(it => it.name == "UI");

            if (_uiAudioMixerGroup == null)
            {
                MelonDebug.Error("Failed to find ui audio mixer");
                yield break;
            }

            MelonDebug.Msg("Start init");

            NetworkManagerHooks.Initialize();

            var pam = this;

            var executingAssembly = Assembly.GetExecutingAssembly();
            var resourcePaths = executingAssembly.GetManifestResourceNames();

            using (var stream = executingAssembly.GetManifestResourceStream(resourcePaths[0]))
            {
                using (var tempStream = new MemoryStream((int)stream.Length))
                {
                    stream.CopyTo(tempStream);

                    _assetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                    _assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }

            _joinSprite = LoadAsset<Sprite>("JoinIcon.png");
            _joinSprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            if (File.Exists(CustomJoinSoundFileName))
            {
                MelonLogger.Msg("Loading custom join sound");
                var uwr = UnityWebRequest.Get($"file://{Path.Combine(Environment.CurrentDirectory, CustomJoinSoundFileName)}");
                uwr.SendWebRequest();

                while (!uwr.isDone) yield return null;

                _JoinClip = WebRequestWWW.InternalCreateAudioClipUsingDH(uwr.downloadHandler, uwr.url, false, false, AudioType.UNKNOWN);
            }

            if (_JoinClip == null)
                _JoinClip = LoadAsset<AudioClip>("Chime.ogg");

            _JoinClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            CreateGameObjects();

            NetworkManagerHooks.OnJoin += OnPlayerJoined;

            PersonAvoiderSettings.SoundVolume.OnValueChanged += (_, __) => ApplySoundSettings();
            PersonAvoiderSettings.UseUiMixer.OnValueChanged += (_, __) => ApplySoundSettings();
            PersonAvoiderSettings.TextSize.OnValueChanged += (_, __) => ApplyFontSize();
            PersonAvoiderSettings.JoinIconColor.OnValueChanged += (_, __) =>
            {
                if (_joinedImage != null) _joinedImage.color = PersonAvoiderSettings.GetJoinIconColor();
            };
        }

        private void ApplySoundSettings()
        {
            if (_joinedAudioSource != null)
            {
                _joinedAudioSource.volume = PersonAvoiderSettings.SoundVolume.Value;
                _joinedAudioSource.outputAudioMixerGroup = PersonAvoiderSettings.UseUiMixer.Value ? _uiAudioMixerGroup : null;
            }
        }

        private void ApplyFontSize()
        {
            if (_joinedText != null) _joinedText.fontSize = PersonAvoiderSettings.TextSize.Value;
        }

        private Image CreateNotifierImage(string name, float offset, Color colorTint)
        {
            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent_Old/Hud");
            var requestedParent = hudRoot.transform.Find("NotificationDotParent");
            var indicator = Object.Instantiate(
                hudRoot.transform.Find("NotificationDotParent/NotificationDot").gameObject,
                requestedParent,
                false
            ).Cast<GameObject>();

            indicator.name = "NotifyDot-" + name;
            indicator.SetActive(true);
            indicator.transform.localPosition += Vector3.right * offset;
            var image = indicator.GetComponent<Image>();
            image.sprite = _joinSprite;

            image.enabled = false;
            image.color = colorTint;

            return image;
        }

        private Text CreateTextNear(Image image, float offset, TextAnchor alignment)
        {
            var gameObject = new GameObject(image.gameObject.name + "-text");
            gameObject.AddComponent<Text>();
            gameObject.transform.SetParent(image.transform, false);
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.localPosition = Vector3.up * offset;
            var text = gameObject.GetComponent<Text>();
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = alignment;
            text.fontSize = PersonAvoiderSettings.TextSize.Value;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.supportRichText = true;

            gameObject.SetActive(true);
            return text;
        }

        private AudioSource CreateAudioSource(AudioClip clip, GameObject parent)
        {
            var source = parent.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialize = false;
            source.volume = PersonAvoiderSettings.SoundVolume.Value;
            source.loop = false;
            source.playOnAwake = false;
            if (PersonAvoiderSettings.UseUiMixer.Value)
                source.outputAudioMixerGroup = _uiAudioMixerGroup;
            return source;
        }

        private void CreateGameObjects()
        {
            if (_joinedImage != null) return;

            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent_Old/Hud");
            if (hudRoot == null)
            {
                MelonLogger.Msg("Not creating gameobjects - no hud root");
                return;
            }

            MelonDebug.Msg("Creating gameobjects");
            //            var pathToThing = "UserInterface/UnscaledUI/HudContent_Old/Hud/NotificationDotParent/NotificationDot";
            _joinedImage = CreateNotifierImage("join", 50f, PersonAvoiderSettings.GetJoinIconColor());
            _joinedAudioSource = CreateAudioSource(_JoinClip, _joinedImage.gameObject);
            _joinedText = CreateTextNear(_joinedImage, 110f, TextAnchor.MiddleCenter);
        }


        public void OnPlayerJoined(Player player)
        {
            var apiUser = player.prop_APIUser_0;
            if (apiUser == null) return;


            var playerName = apiUser.displayName ?? "!null!";
            var username = apiUser.username ?? "!null!";
            var playerId = apiUser.id ?? "!null!";

            MaybeUpdateAlertOn();

            var matchesPlayer = _alertOn.Contains(playerName.Trim().ToLower());
            var matchesUser = _alertOn.Contains(username.Trim().ToLower());
            var matchesId = _alertOn.Contains(playerId.Trim().ToLower());

            if (!matchesPlayer && !matchesUser && !matchesId)
            {
                return;
            }

            var isBlocked = IsBlocked(apiUser.id);

            if (PersonAvoiderSettings.ShouldJoinBlink.Value)
                MelonCoroutines.Start(BlinkIconCoroutine(_joinedImage));

            if (PersonAvoiderSettings.ShouldPlayJoinSound.Value)
                _joinedAudioSource.Play();

            if (PersonAvoiderSettings.JoinShowName.Value)
                MelonCoroutines.Start(ShowName(_joinedText, CurrentNames, playerName, isBlocked));

            if (PersonAvoiderSettings.LogToConsole.Value)
            {
                var detectedBy = "";
                if (matchesUser)
                {
                    detectedBy += $" (username is {username})";
                }
                if (matchesUser)
                {
                    detectedBy += $" (userid is {playerId})";
                }

                MelonLogger.Msg(isBlocked ? ConsoleColor.Red : ConsoleColor.DarkYellow, $"'{playerName}' joined{detectedBy}");
            }
        }

        private void MaybeUpdateAlertOn()
        {
            var date = File.GetLastWriteTime(AlertListFileName);
            if (date <= _alertListLastUpdate) return;
            _alertListLastUpdate = date;

            MelonLogger.Msg("Loading UserData/AlertList.txt");

            _alertOn.Clear();
            var lines = File.ReadAllLines(AlertListFileName);

            for (var i = 0; i < lines.Length; i++)
            {
                _alertOn.Add(lines[i].Trim().ToLower());
            }
        }

        public IEnumerator ShowName(Text text, List<string> namesList, string name, bool isBlocked)
        {
            var color = isBlocked ? Color.red : Color.yellow;
            var playerLine = $"<color={RenderHex(color)}>{name}</color>";

            namesList.Add(playerLine);

            text.text = string.Join("\n", namesList);
            yield return new WaitForSeconds(3);
            namesList.Remove(playerLine);
            text.text = string.Join("\n", namesList);
        }

        private static string RenderHex(Color color)
        {
            return $"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}{(int)(color.a * 255):X2}";
        }

        public IEnumerator BlinkIconCoroutine(Image imageToBlink)
        {
            for (var i = 0; i < 3; i++)
            {
                imageToBlink.enabled = true;
                yield return new WaitForSeconds(.5f);
                imageToBlink.enabled = false;
                yield return new WaitForSeconds(.5f);
            }
        }

        private static bool IsBlocked(string userId)
        {
            if (userId == null) return false;

            var moderationManager = ModerationManager.prop_ModerationManager_0;
            if (moderationManager == null) return false;
            if (APIUser.CurrentUser?.id == userId)
                return false;

            var moderationsDict = ModerationManager.prop_ModerationManager_0.field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0;
            if (!moderationsDict.ContainsKey(userId)) return false;

            foreach (var playerModeration in moderationsDict[userId])
            {
                if (playerModeration != null && playerModeration.moderationType == ApiPlayerModeration.ModerationType.Block)
                    return true;
            }

            return false;
        }
    }
}
