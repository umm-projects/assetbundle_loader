using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;

namespace ProjectManagement.AssetBundle {

    public class Loader {

        /// <summary>
        /// Root AssetBundleManifest のキャッシュ保存先パスフォーマット
        /// </summary>
        private const string CACHE_PATH_ROOT_ASSET_BUNDLE_MANIFEST_FORMAT = "AssetBundles/{0}";

        public string ProjectName {
            get;
            set;
        }

        public UnityEngine.AssetBundleManifest RootAssetBundleManifest {
            get;
            set;
        }

        private Dictionary<string, List<string>> assetBundleDependencieListMap;

        public Dictionary<string, List<string>> AssetBundleDependencieListMap {
            get {
                if (this.assetBundleDependencieListMap == default(Dictionary<string, List<string>>)) {
                    this.assetBundleDependencieListMap = new Dictionary<string, List<string>>();
                }
                return this.assetBundleDependencieListMap;
            }
            set {
                this.assetBundleDependencieListMap = value;
            }
        }

        private Dictionary<string, string> sceneAssetBundleNameMap;

        public Dictionary<string, string> SceneAssetBundleNameMap {
            get {
                if (this.sceneAssetBundleNameMap == default(Dictionary<string, string>)) {
                    this.sceneAssetBundleNameMap = new Dictionary<string, string>();
                }
                return this.sceneAssetBundleNameMap;
            }
            set {
                this.sceneAssetBundleNameMap = value;
            }
        }

        private List<UnityEngine.AssetBundle> temporaryLoadedAssetBundleList;

        private List<UnityEngine.AssetBundle> TemporaryLoadedAssetBundleList {
            get {
                if (this.temporaryLoadedAssetBundleList == default(List<UnityEngine.AssetBundle>)) {
                    this.temporaryLoadedAssetBundleList = new List<UnityEngine.AssetBundle>();
                }
                return this.temporaryLoadedAssetBundleList;
            }
        }

        /// <summary>
        /// 全ての AssetAundle をダウンロードする
        /// </summary>
        /// <returns>全てのダウンロードが終わったことを通知するストリーム</returns>
        public IObservable<Unit> FetchAllAsObservable() {
            return this.FetchRootAssetBundleAsObservable()
                .SelectMany(_ => this.FetchAllAssetBundlesAsObservable());
        }

        /// <summary>
        /// 任意の AssetBundle を読み込む
        /// </summary>
        /// <param name="assetBundleName">AssetBundle 名</param>
        /// <returns>読み込んだ AssetBundle のインスタンス</returns>
        public IObservable<UnityEngine.AssetBundle> LoadAsObservable(string assetBundleName) {
            this.TemporaryLoadedAssetBundleList.Clear();
            return this.LoadAssetBundleWithDependenciesAsObservable(assetBundleName)
                .Finally(
                    () => {
                        this.TemporaryLoadedAssetBundleList.FindAll(x => x != default(UnityEngine.AssetBundle)).ForEach(x => x.Unload(false));
                    }
                );
        }

        /// <summary>
        /// プロジェクトに紐付く全ての AssetBundle をダウンロードする
        /// </summary>
        /// <remarks>ダウンロードした AssetBundle に関する情報をインスタンスに保持します</remarks>
        /// <remarks>ダウンロードするだけなので、即 Unload します</remarks>
        /// <returns>ダウンロードが完了したことを通知するストリーム</returns>
        private IObservable<Unit> FetchAllAssetBundlesAsObservable() {
            Manager.Instance.GetProgressNotifier(this.ProjectName).TotalCount = this.RootAssetBundleManifest.GetAllAssetBundles().Count();
            return this.RootAssetBundleManifest
                .GetAllAssetBundles()
                .Select(assetBundleName => this.FetchAsObservable(assetBundleName, true))
                .WhenAll()
                .AsUnitObservable();
        }

        /// <summary>
        /// 依存も含めて AssetBundle を読み込む
        /// </summary>
        /// <param name="assetBundleName">AssetBundle 名</param>
        /// <returns>全てのダウンロードが完了したら値が流れるストリーム</returns>
        private IObservable<UnityEngine.AssetBundle> LoadAssetBundleWithDependenciesAsObservable(string assetBundleName) {
            if (!this.AssetBundleDependencieListMap.ContainsKey(assetBundleName) || this.AssetBundleDependencieListMap[assetBundleName].Count == 0) {
                return this.FetchAsObservable(assetBundleName, false);
            }
            // 再帰的に依存 AssetBundle を読み込み
            return this.AssetBundleDependencieListMap[assetBundleName]
                .Select(this.LoadAssetBundleWithDependenciesAsObservable)
                .WhenAll()
                .SelectMany(_ => this.FetchAsObservable(assetBundleName, false));
        }

        /// <summary>
        /// AssetBundle をダウンロードする
        /// </summary>
        /// <param name="assetBundleName">AssetBundle 名</param>
        /// <param name="shouldUnloadImmediately">自動 Unload するかどうか</param>
        /// <returns>ダウンロードが完了した AssetBundle のインスタンスが流れるストリーム</returns>
        private IObservable<UnityEngine.AssetBundle> FetchAsObservable(string assetBundleName, bool shouldUnloadImmediately) {
            ScheduledNotifier<float> scheduledNotifier = new ScheduledNotifier<float>();
            scheduledNotifier.Subscribe(x => Manager.Instance.GetProgressNotifier(this.ProjectName).Report(assetBundleName, x));
            return ObservableUnityWebRequest
                .GetAssetBundle(
                    Manager.Instance.SourceProvider.DeterminateURL(assetBundleName),
                    this.RootAssetBundleManifest.GetAssetBundleHash(assetBundleName),
                    0,
                    null,
                    scheduledNotifier
                )
                .Select(
                    (assetBundle) => {
                        // 保持している他のバージョンキャッシュを削除する
                        this.ClearOtherCachedVersions(assetBundle.name);

                        // AssetBundle 単位の依存 AssetBundle 名マップに依存 AssetBundle を保存
                        this.AssetBundleDependencieListMap[assetBundle.name] = this.RootAssetBundleManifest.GetAllDependencies(assetBundle.name).ToList();
                        if (assetBundle.isStreamedSceneAssetBundle) {
                            /* StreamedSceneAssetBundle の場合は AssetBundle 名を保持しておく */
                            string sceneName = Path.GetFileNameWithoutExtension(assetBundle.GetAllScenePaths().ToList().First());
                            if (!string.IsNullOrEmpty(sceneName)) {
                                this.SceneAssetBundleNameMap[sceneName] = assetBundle.name;
                            }
                        }
                        if (shouldUnloadImmediately) {
                            assetBundle.Unload(true);
                            return null;
                        }
                        this.TemporaryLoadedAssetBundleList.Add(assetBundle);
                        return assetBundle;
                    }
                );
        }

        /// <summary>
        /// Root AssetBundle をキャッシュしつつダウンロードする
        /// </summary>
        /// <returns>ダウンロードが完了した AssetBundle のインスタンスが流れるストリーム</returns>
        private IObservable<Unit> FetchRootAssetBundleAsObservable() {
            string rootAssetBundleManifestPath = Path.Combine(UnityEngine.Application.persistentDataPath, string.Format(CACHE_PATH_ROOT_ASSET_BUNDLE_MANIFEST_FORMAT, this.ProjectName));
            IObservable<UnityEngine.AssetBundle> stream;
            if (UnityEngine.Application.internetReachability == UnityEngine.NetworkReachability.NotReachable && File.Exists(rootAssetBundleManifestPath)) {
                stream = Observable
                    .Return(UnityEngine.AssetBundle.LoadFromFile(rootAssetBundleManifestPath));
            } else {
                stream = ObservableUnityWebRequest
                    .GetUnityWebRequest(Manager.Instance.SourceProvider.DeterminateURL(this.ProjectName, true))
                    .Select(
                        (unityWebRequest) => {
                            if (!Directory.Exists(Path.GetDirectoryName(rootAssetBundleManifestPath)) && !string.IsNullOrEmpty(Path.GetDirectoryName(rootAssetBundleManifestPath))) {
                                // ReSharper disable once AssignNullToNotNullAttribute
                                Directory.CreateDirectory(Path.GetDirectoryName(rootAssetBundleManifestPath));
                            }
                            File.WriteAllBytes(rootAssetBundleManifestPath, unityWebRequest.downloadHandler.data);
#if UNITY_IOS && !UNITY_EDITOR
                            // iOS 実機の場合、非ユーザデータを iCloud バックアップ対象から除外しないとリジェクトされます
                            UnityEngine.iOS.Device.SetNoBackupFlag(rootAssetBundleManifestPath);
#endif
                            return UnityEngine.AssetBundle.LoadFromFile(rootAssetBundleManifestPath);
                        }
                    );
            }
            return stream.Select(
                (assetBundle) => {
                    this.RootAssetBundleManifest = assetBundle.LoadAsset<UnityEngine.AssetBundleManifest>("AssetBundleManifest");
                    assetBundle.Unload(false);
                    return Unit.Default;
                }
            );
        }

        /// <summary>
        /// 最新バージョン以外の AssetBundle キャッシュをクリアする
        /// </summary>
        /// <param name="assetBundleName"></param>
        private void ClearOtherCachedVersions(string assetBundleName) {
            UnityEngine.Hash128 hash = this.RootAssetBundleManifest.GetAssetBundleHash(assetBundleName);
            UnityEngine.Caching.ClearOtherCachedVersions(assetBundleName, hash);
        }

    }

}