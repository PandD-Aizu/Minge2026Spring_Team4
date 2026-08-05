using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelAchievementPresenter : IInitializable
    {
        private readonly AchievementUseCase _achievementUseCase;
        private readonly NovelAchievementUIView _view;

        public NovelAchievementPresenter(AchievementUseCase achievementUseCase, NovelAchievementUIView view)
        {
            _achievementUseCase = achievementUseCase;
            _view = view;
        }

        public void Initialize()
        {
            _view.SetAchievements(
                _achievementUseCase.GetReachedEndingIds(),
                _achievementUseCase.GetEndingClearCount() > 0);
        }
    }
}
