using System.Linq;
using UniRx;

namespace ProjectManagement.AssetBundle {

    public class ProgressNotifier {

        public int TotalCount {
            private get;
            set;
        }

        private readonly ReactiveDictionary<string, float> rdProgressMap = new ReactiveDictionary<string, float>();

        private readonly ReactiveProperty<float> rpProgress = new ReactiveProperty<float>();

        public ProgressNotifier() {
            this.rdProgressMap.ObserveAdd().Subscribe(_ => this.Summary());
            this.rdProgressMap.ObserveReplace().Subscribe(_ => this.Summary());
        }

        public void Report(string assetBundleName, float progress) {
            this.rdProgressMap[assetBundleName] = progress;
        }

        public IObservable<float> AsObservable() {
            return this.rpProgress.Select(x => x / this.TotalCount);
        }

        private void Summary() {
            this.rpProgress.Value = this.rdProgressMap.Values.Sum();
        }

    }

}

