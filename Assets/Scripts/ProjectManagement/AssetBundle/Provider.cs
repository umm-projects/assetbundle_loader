
using UnityEngine;
using UnityModule.Settings;

namespace ProjectManagement.AssetBundle {

    public interface ISourceProvider {

        string DeterminateURL(string assetBundleName);

        string DeterminateURL(string assetBundleName, bool isRoot);

        bool ShouldAppendExtension();

    }

    public class DefaultSourceProvider : ISourceProvider {

        public string DeterminateURL(string assetBundleName) {
            return this.DeterminateURL(assetBundleName, false);
        }

        public string DeterminateURL(string assetBundleName, bool isRoot) {
            return string.Format(
                EnvironmentSetting.Instance.AssetBundleURLFormat,
                string.Format(assetBundleName, this.ShouldAppendExtension() && !isRoot ? EnvironmentSetting.Instance.AssetBundleExtension : string.Empty),
                Application.version
            );
        }

        public bool ShouldAppendExtension() {
            return true;
        }

    }

}
