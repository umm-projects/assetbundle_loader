using System.Collections.Generic;
using UnityModule;

namespace ProjectManagement.AssetBundle {

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

        public Loader GetLoader(string projectName) {
            if (!this.LoaderMap.ContainsKey(projectName)) {
                this.LoaderMap[projectName] = new Loader() {
                    ProjectName = projectName,
                };
            }
            return this.LoaderMap[projectName];
        }

        public ProgressNotifier GetProgressNotifier(string projectName) {
            if (!this.ProgressNotifierMap.ContainsKey(projectName)) {
                this.ProgressNotifierMap[projectName] = new ProgressNotifier();
            }
            return this.ProgressNotifierMap[projectName];
        }

    }

}