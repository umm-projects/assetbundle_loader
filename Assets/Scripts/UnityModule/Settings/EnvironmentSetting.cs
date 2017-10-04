using UnityEngine;

namespace UnityModule.Settings {

    public partial class EnvironmentSetting {

        private const string DEFAULT_ASSET_BUNDLE_CACHE_PATH_FORMAT = "AssetBundles/{0}";

        private const string DEFAULT_ASSET_BUNDLE_EXTENSION = ".unity3d";

        [SerializeField][Tooltip("string.Format による置換が実行されます。\n{0}: AssetBundle 名\n{1}: アプリバージョン")]
        private string assetBundleURLFormat;

        public string AssetBundleURLFormat {
            get {
                return this.assetBundleURLFormat;
            }
            set {
                this.assetBundleURLFormat = value;
            }
        }

        [SerializeField][Tooltip("string.Format による置換が実行されます。\n{0}: AssetBundle 名\n{1}: アプリバージョン")]
        private string assetBundleCachePathFormat = DEFAULT_ASSET_BUNDLE_CACHE_PATH_FORMAT;

        public string AssetBundleCachePathFormat {
            get {
                return this.assetBundleCachePathFormat;
            }
            set {
                this.assetBundleCachePathFormat = value;
            }
        }

        [SerializeField]
        private string assetBundleExtension = DEFAULT_ASSET_BUNDLE_EXTENSION;

        public string AssetBundleExtension {
            get {
                return this.assetBundleExtension;
            }
            set {
                this.assetBundleExtension = value;
            }
        }

    }

}

