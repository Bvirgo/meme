namespace NendUnityPlugin
{
	using UnityEngine;
	using UnityEditor;
	using System.IO;
	using System.Xml;
	using System.Linq;

	public class NendAndroidSetup : EditorWindow
	{
		private static bool isImportGooglePlayServices = false;
		private static bool isOutputDebugLog = false;

		private const string AndroidSDKRoot = "AndroidSdkRoot";
		private const string GmsDirectoryPath = "extras/google/m2repository/com/google/android/gms";
		private const string GmsArtifactName = "play-services-basement";
		private const string AndroidLibraryDirectoryPath = "NendAd/Plugins/Android";

		private bool m_ShowGooglePlayServiceMenu = true;
		private bool m_ShowDebugMenu = true;
		private Vector2 m_ScrollPosition = Vector2.zero;

		private static class Ja
		{
			internal const string ImportGooglePlayServices = "ダウンロード済みのGoogle Play Servicesをプロジェクトに追加します。\n既にGoogle Play Servicesがプロジェクトに追加されている場合はチェックを外してください。";
			internal const string WarningAndroidSDKPath = "AndroidSDKのパスが設定されていません。\nUnityのPreferences... > External Toolsより設定を行ってください。";
			internal const string WarningGooglePlayServices = "Google Play Servicesがダウンロードされていません。\nAndroid SDK ManagerでGoogle Repositoryをダウンロードしてください。";
			internal const string AboutUnityPreferences = "Preferences設定について";
			internal const string AboutAndroidSDKManager = "Android SDK Managerについて";
			internal const string AboutGoogleRepository = "Google Repositoryについて";
			internal const string OutputDebugLog = "nendSDKのデバッグログを出力するかどうかを設定します。";
			internal const string UnityPreferencesURL = "https://docs.unity3d.com/ja/current/Manual/Preferences.html";
			internal const string AndroidSDKManagerURL = "https://developer.android.com/studio/intro/update.html?hl=ja#sdk-manager";
			internal const string GoogleRepositoryURL = "https://developer.android.com/studio/intro/update.html?hl=ja#recommended";
		}

		private static class En
		{
			internal const string ImportGooglePlayServices = "Add the downloaded Google Play Services to your project.\nUncheck this if Google Play Services has already been added to the project.";
			internal const string WarningAndroidSDKPath = "The Android SDK path is not set.\nPlease make settings from Unity's \"Preferences ...> External Tools\".";
			internal const string WarningGooglePlayServices = "Google Play Services has not been downloaded.\nDownload Google Repository with Android SDK Manager.";
			internal const string AboutUnityPreferences = "About Preferences";
			internal const string AboutAndroidSDKManager = "About Android SDK Manager";
			internal const string AboutGoogleRepository = "About Google Repository";
			internal const string OutputDebugLog = "Sets whether to output debug log of nendSDK or not.";
			internal const string UnityPreferencesURL = "https://docs.unity3d.com/Manual/Preferences.html";
			internal const string AndroidSDKManagerURL = "https://developer.android.com/studio/intro/update.html#sdk-manager";
			internal const string GoogleRepositoryURL = "https://developer.android.com/studio/intro/update.html#recommended";
		}
			
		[MenuItem ("NendSDK/Android Setup", false, 1)]
		public static void MenuItemAndroidSetup ()
		{
			NendAndroidSetup window = (NendAndroidSetup)EditorWindow.GetWindow (typeof(NendAndroidSetup));
			var titleContent = new GUIContent ();
			titleContent.text = "Android Setup";
			window.titleContent = titleContent;

			var vec2 = new Vector2 (460, 210);
			window.minSize = vec2;
			window.Show ();
		}

		void OnGUI ()
		{
			GUIStyle buttonStyle;

			var isJapanese = IsJapanese ();

			m_ScrollPosition = EditorGUILayout.BeginScrollView (m_ScrollPosition);
			{
				m_ShowGooglePlayServiceMenu = EditorGUILayout.Foldout (m_ShowGooglePlayServiceMenu, "Google Play Services");
				if (m_ShowGooglePlayServiceMenu) {
					EditorGUI.indentLevel = 1;
					EditorGUILayout.HelpBox (isJapanese ? Ja.ImportGooglePlayServices : En.ImportGooglePlayServices, MessageType.Info, true);
					isImportGooglePlayServices = EditorGUILayout.ToggleLeft ("Import Google Play Services", isImportGooglePlayServices);
					if (isImportGooglePlayServices) {
						if (!CheckAndroidSDKPath ()) {
							EditorGUILayout.HelpBox (isJapanese ? Ja.WarningAndroidSDKPath : En.WarningAndroidSDKPath, MessageType.Warning, true);
							buttonStyle = new GUIStyle (GUI.skin.button);
							buttonStyle.margin = new RectOffset (20, 0, 0, 0);
							if (GUILayout.Button (isJapanese ? Ja.AboutUnityPreferences : En.AboutUnityPreferences, buttonStyle, GUILayout.ExpandWidth (false))) {
								Application.OpenURL (isJapanese ? Ja.UnityPreferencesURL : En.UnityPreferencesURL);
							}
						} else if (!CheckGooglePlayService ()) {
							EditorGUILayout.HelpBox (isJapanese ? Ja.WarningGooglePlayServices : En.WarningGooglePlayServices, MessageType.Warning, true);
							EditorGUILayout.BeginHorizontal ();
							{
								buttonStyle = new GUIStyle (GUI.skin.button);
								buttonStyle.margin = new RectOffset (20, 0, 0, 0);
								if (GUILayout.Button (isJapanese ? Ja.AboutAndroidSDKManager : En.AboutAndroidSDKManager, buttonStyle, GUILayout.ExpandWidth (false))) {
									Application.OpenURL (isJapanese ? Ja.AndroidSDKManagerURL : En.AndroidSDKManagerURL);
								}
								buttonStyle = new GUIStyle (GUI.skin.button);
								buttonStyle.margin = new RectOffset (10, 0, 0, 0);
								if (GUILayout.Button (isJapanese ? Ja.AboutGoogleRepository : En.AboutGoogleRepository, buttonStyle, GUILayout.ExpandWidth (false))) {
									Application.OpenURL (isJapanese ? Ja.GoogleRepositoryURL : En.GoogleRepositoryURL);
								}
							}
							EditorGUILayout.EndHorizontal ();
						}
					}
				}

				EditorGUI.indentLevel = 0;

				m_ShowDebugMenu = EditorGUILayout.Foldout (m_ShowDebugMenu, "Debug");
				if (m_ShowDebugMenu) {
					EditorGUI.indentLevel = 1;
					EditorGUILayout.HelpBox (IsJapanese () ? Ja.OutputDebugLog : En.OutputDebugLog, MessageType.Info, true);
					isOutputDebugLog = EditorGUILayout.ToggleLeft ("Output Debug Log", isOutputDebugLog);
				}
			}
			EditorGUILayout.EndScrollView ();

			buttonStyle = new GUIStyle (GUI.skin.button);
			buttonStyle.margin = new RectOffset (20, 20, 10, 10);
			if (GUILayout.Button ("Configure", buttonStyle, GUILayout.Height (24))) {
				Configure ();
			}
		}

		public void Configure ()
		{
			Debug.Log ("Processing...");
			if (isImportGooglePlayServices) {
				AddGooglePlayServicesLibrary ();
			}
			ConfigureAndroidManifest ();
			AssetDatabase.Refresh ();
			Debug.Log ("Done!");
			Close ();
		}

		private static bool IsJapanese ()
		{
			return Application.systemLanguage == SystemLanguage.Japanese; 
		}

		private static bool CheckAndroidSDKPath ()
		{
			string androidSDKPath = EditorPrefs.GetString (AndroidSDKRoot, null);
			return !string.IsNullOrEmpty (androidSDKPath);
		}

		private static bool CheckGooglePlayService ()
		{
			string androidSDKPath = EditorPrefs.GetString (AndroidSDKRoot, null);
			string gmsPath = Path.Combine (androidSDKPath, ToPlatformDirectorySeparator (GmsDirectoryPath));
			return Directory.Exists (gmsPath);
		}

		private static string ToPlatformDirectorySeparator (string path)
		{
			return path.Replace ("/", Path.DirectorySeparatorChar.ToString ());
		}

		private static void AddGooglePlayServicesLibrary ()
		{
			string androidSDKPath = EditorPrefs.GetString (AndroidSDKRoot, null);
			if (string.IsNullOrEmpty (androidSDKPath)) {
				Debug.LogWarning ("AndroidSdkRoot is not setup.");
				return;
			}
			Debug.Log ("AndroidSDK path: " + androidSDKPath);
			if (!Directory.Exists (androidSDKPath)) {
				Debug.LogWarning ("AndroidSDK is not installed.");
				return;
			}
			string gmsPath = Path.Combine (androidSDKPath, ToPlatformDirectorySeparator (GmsDirectoryPath));
			if (!Directory.Exists (gmsPath)) {
				Debug.LogWarning ("The Google Play services library is not installed.");
				return;
			}
			string libraryDirectoryPath = Path.Combine (Application.dataPath, ToPlatformDirectorySeparator (AndroidLibraryDirectoryPath));
			string[] archives = Directory.GetFiles (libraryDirectoryPath, "play-services-basement-*.?.?.aar");
			if (null != archives && 0 < archives.Length) {
				Debug.Log ("The `play-services-basement` is already exist.");
				return;
			}
			string artifactPath = Path.Combine (androidSDKPath, Path.Combine (GmsDirectoryPath, GmsArtifactName));
			var directoryInfo = new DirectoryInfo (artifactPath);
			if (directoryInfo.Exists) {
				DirectoryInfo[] infos = directoryInfo.GetDirectories ("*.?.?");
				if (null == infos || 0 == infos.Length) {
					Debug.LogWarning ("The Google Play services library is not installed.");
					return;
				}
				var max = infos
					.Select (di => di.Name)
					.Aggregate ((current, next) => {
					int currentVersion = int.Parse (current.Replace (".", ""));
					int nextVersion = int.Parse (next.Replace (".", ""));
					return nextVersion > currentVersion ? next : current;
				});
				string archiveName = string.Format ("play-services-basement-{0}.aar", max);
				string aarPathFrom = Path.Combine (artifactPath, Path.Combine (max, archiveName));
				string aarPathTo = Path.Combine (libraryDirectoryPath, archiveName);
				FileUtil.CopyFileOrDirectory (aarPathFrom, aarPathTo);
				Debug.Log ("Added: " + aarPathTo);
			} else {
				Debug.LogWarning ("The Google Play services library is not installed.");
			}
		}

		private static void ConfigureAndroidManifest ()
		{
			string manifestPathDest = Path.Combine (Application.dataPath, ToPlatformDirectorySeparator (AndroidLibraryDirectoryPath + "/AndroidManifest.xml"));
			if (!File.Exists (manifestPathDest)) {
				if (!isOutputDebugLog) {
					Debug.Log ("There is no need to change the AndroidManifest.");
					return;
				}

				string[] UnityAndroidManifestPathList = {
					Path.Combine (EditorApplication.applicationPath, ToPlatformDirectorySeparator ("../PlaybackEngines/AndroidPlayer/Apk/AndroidManifest.xml")),
					Path.Combine (EditorApplication.applicationContentsPath, ToPlatformDirectorySeparator ("PlaybackEngines/AndroidPlayer/Apk/AndroidManifest.xml")),
					Path.Combine (EditorApplication.applicationContentsPath, ToPlatformDirectorySeparator ("PlaybackEngines/AndroidPlayer/AndroidManifest.xml"))
				};
					
				string defaultManifestPath = null;
				foreach (string path in UnityAndroidManifestPathList) {
					if (File.Exists (path)) {
						defaultManifestPath = path;
						Debug.Log ("Found AndroidManifest at " + path);
						break;
					}
				}
				if (null == defaultManifestPath) {
					Debug.LogWarning ("Couldn't find default AndroidManifest.");
					return;
				}
				FileUtil.CopyFileOrDirectory (defaultManifestPath, manifestPathDest);
			} else {
				Debug.Log ("The AndroidManifest is already exist.");
			}
		
			var doc = new XmlDocument ();
			doc.Load (manifestPathDest);
		
			XmlNode applicationNode = doc.SelectSingleNode ("manifest/application");
			if (null == applicationNode) {
				Debug.LogWarning ("The application tag is not found.");
				return;
			}
		
			string ns = applicationNode.GetNamespaceOfPrefix ("android");
			var nsManager = new XmlNamespaceManager (doc.NameTable);
			nsManager.AddNamespace ("android", ns);
				
			XmlNode nendDebuggableNode = applicationNode.SelectSingleNode (@"//meta-data[@android:name='NendDebuggable']", nsManager);
			if (null != nendDebuggableNode) {
				XmlElement element = (XmlElement)nendDebuggableNode;
				element.SetAttribute ("value", ns, isOutputDebugLog.ToString ().ToLower ());
				Debug.Log ("Modified: " + element.OuterXml);
			} else if (isOutputDebugLog) { 
				XmlElement element = CreateNendDebuggableElement (doc, ns);
				applicationNode.AppendChild (element);
				Debug.Log ("Added: " + element.OuterXml);
			} else {
				Debug.Log ("There is no need to create a NendDebuggable element.");
			}
			doc.Save (manifestPathDest);
		}

		private static XmlElement CreateNendDebuggableElement (XmlDocument doc, string ns)
		{
			XmlElement element = doc.CreateElement ("meta-data");
			element.SetAttribute ("name", ns, "NendDebuggable");
			element.SetAttribute ("value", ns, "true");
			return element;
		}
	}
}