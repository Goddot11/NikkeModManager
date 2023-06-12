using Microsoft.Xna.Framework;
using MonoGame.Forms.Controls;
using NikkeModManagerCore;

namespace NikkeModManager.PreviewEngine
{
    public class PreviewEngine : MonoGameControl {

        private PreviewScreen4_0_64 _screen4064;
        private PreviewScreen4_1_0 _screen410;
        private PreviewScreen? _currentScreen;

        private readonly Dictionary<NikkeBundle, PreviewScreen> _bundleScreens = new Dictionary<NikkeBundle, PreviewScreen>();

        private NikkeBundle _previewBundle;
        private string _previewAnim;

        protected override void Initialize()
        {
            base.Initialize();
            _screen4064 = new PreviewScreen4_0_64(this);
            _screen410 = new PreviewScreen4_1_0(this);
        }

        public void PreviewBundle(NikkeBundle bundle, string anim = "") {
            _previewBundle = bundle;
            _previewAnim = anim != "" ? anim : bundle.DefaultAnimation;
        }

        private void SetBundle(NikkeBundle bundle) {
            if (!_bundleScreens.ContainsKey(bundle)) {
                Console.WriteLine($"Building skeleton for {bundle.FileName}");
                try {
                    _screen410.BuildSkeleton(bundle);
                    _bundleScreens[bundle] = _screen410;
                } catch (Exception e) {
                    try {
                        _screen4064.BuildSkeleton(bundle);
                        _bundleScreens[bundle] = _screen4064;
                    } catch (Exception e2) {
                        Console.WriteLine($"Unable to build skeleton for bundle {bundle.FileName}:\n4.1.0 Engine:{e}\n\n:4.0.64 Engine:\n{e2}");
                        return;
                    }
                }
            }

            _currentScreen = _bundleScreens[bundle];
            Console.WriteLine($"Loading skeleton for {bundle.CharacterId} with anim {_previewAnim}");
            try {
                _currentScreen!.LoadSkeleton(bundle, _previewAnim);
                Console.WriteLine($"Successfully loaded skeleton for {bundle.CharacterId} with anim {_previewAnim}");
            } catch (Exception e) {
                Console.WriteLine($"Failed to load skeleton:\n{e}");
            }
        }

        protected override void Update(GameTime gameTime) {
            if (_previewBundle != null) {
                lock (_previewBundle) {
                    SetBundle(_previewBundle);
                    _previewBundle = null;
                }
            }

            if (_currentScreen != null) {
                _currentScreen.Update((float)(gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0));
            }
        }

        protected override void Draw() {
            if (_currentScreen == null) return;
            lock (_currentScreen) {
                try {
                    _currentScreen!.Render();
                } catch (Exception e) {
                    Console.WriteLine($"Failed to draw preview: \n{e}");
                }
            }
        }
    }
}
