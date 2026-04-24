using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Domain.ValueObject;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class SceneTransitionUseCase
    {
        private readonly ISceneTransitionProvider _sceneTransitionProvider;

        public SceneTransitionUseCase(ISceneTransitionProvider sceneTransitionProvider)
        {
            _sceneTransitionProvider = sceneTransitionProvider;
        }

        public void LoadTitleSceneAsync()
            => _sceneTransitionProvider.LoadSceneAsync(SceneLabel.Title);
        
        public void LoadOptionSceneAsync()
            => _sceneTransitionProvider.LoadSceneAsync(SceneLabel.Option);
        
        public void LoadChapterSelectSceneAsync()
            => _sceneTransitionProvider.LoadSceneAsync(SceneLabel.ChapterSelect);
        
        public void LoadNovelSceneAsync()
            => _sceneTransitionProvider.LoadSceneAsync(SceneLabel.Novel);
        
        public void LoadResultSceneAsync()
            => _sceneTransitionProvider.LoadSceneAsync(SceneLabel.Result);
    }
}