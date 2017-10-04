using System.Collections.Generic;
using UnityModule;

namespace SimpleLoader.AssetBundle {

    public class Manager : Singleton<Manager> {

        private ISourceProvider sourceProvider;

        public ISourceProvider SourceProvider {
            get {
                if (this.sourceProvider == default(ISourceProvider)) {
                    this.sourceProvider = new DefaultSourceProvider();
                }
                return this.sourceProvider;
            }
            set {
                this.sourceProvider = value;
            }
        }

        private Dictionary<string, Loader> loaderMap;

        private Dictionary<string, Loader> LoaderMap {
            get {
                if (this.loaderMap == default(Dictionary<string, Loader>)) {
                    this.loaderMap = new Dictionary<string, Loader>();
                }
                return this.loaderMap;
            }
        }

        private Dictionary<string, ProgressNotifier> progressNotifierMap;

        private Dictionary<string, ProgressNotifier> ProgressNotifierMap {
            get {
                if (this.progressNotifierMap == default(Dictionary<string, ProgressNotifier>)) {
                    this.progressNotifierMap = new Dictionary<string, ProgressNotifier>();
                }
                return this.progressNotifierMap;
            }
        }

        public Loader GetLoader() {
            return this.GetLoader(string.Empty);
        }

        public Loader GetLoader(string key) {
            if (!this.LoaderMap.ContainsKey(key)) {
                this.LoaderMap[key] = new Loader() {
                    RootAssetBundleName = key,
                    ProgressNotifier = this.GetProgressNotifier(key),
                };
            }
            return this.LoaderMap[key];
        }

        public ProgressNotifier GetProgressNotifier() {
            return this.GetProgressNotifier(string.Empty);
        }

        public ProgressNotifier GetProgressNotifier(string key) {
            if (!this.ProgressNotifierMap.ContainsKey(key)) {
                this.ProgressNotifierMap[key] = new ProgressNotifier();
            }
            return this.ProgressNotifierMap[key];
        }

    }

}