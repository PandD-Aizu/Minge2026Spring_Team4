using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelChatPresenter : IInitializable, IDisposable
    {
        // TODO: リファクタする
        private enum SkipMode
        {
            None,
            ToNextInputRequired
        }

        private readonly ChatUseCase _chatUseCase;
        private readonly FreeChatUseCase _freeChatUseCase;
        private readonly MoraleUseCase _moraleUseCase;
        private readonly ITmpMoraleJsonExporter _tmpMoraleJsonExporter;
        private readonly ProcessUseCase _processUseCase;
        private readonly ChatWindowView _chatWindowView;
        private readonly IJsonUtilityProvider _jsonUtilityProvider;
        private readonly NovelDmButtonView _dmButtonView;
        private SkipButtonHoldNotifier _skipButtonHoldNotifier;
        private CancellationTokenSource _dmCancellationSource = new();
        private bool _isDmMode;
        private bool _isRestoringMainScenario;
        private bool _hasStartedExternalGame;
        private CompositeDisposable _disposables = new();
        private SkipMode _skipMode = SkipMode.None; // TODO: リファクタする

        public NovelChatPresenter(
            ChatUseCase chatUseCase,
            FreeChatUseCase freeChatUseCase,
            MoraleUseCase moraleUseCase,
            ITmpMoraleJsonExporter tmpMoraleJsonExporter,
            ProcessUseCase processUseCase,
            ChatWindowView chatWindowView,
            IJsonUtilityProvider jsonUtilityProvider,
            NovelDmButtonView dmButtonView)
        {
            _chatUseCase = chatUseCase;
            _freeChatUseCase = freeChatUseCase;
            _moraleUseCase = moraleUseCase;
            _tmpMoraleJsonExporter = tmpMoraleJsonExporter;
            _processUseCase = processUseCase;
            _chatWindowView = chatWindowView;
            _jsonUtilityProvider = jsonUtilityProvider;
            _dmButtonView = dmButtonView;
        }
        
        public void Initialize()
        {
            _moraleUseCase.OnGameStart();

            BindSkipButton();
            _dmButtonView.DmClicked += EnterDmMode;
            _dmButtonView.CharacterDmClicked += StartCharacterDm;
            _dmButtonView.DmBackClicked += ExitDmMode;

            // 章の会話ブロックの変化を監視して、チャットウィンドウに反映させる
            _chatUseCase.CurrentChapterBlock
                .Where(block => block is not null)
                .Where(_ => !_isRestoringMainScenario)
                .SubscribeAwait(async (block, token) => await HandleChapterBlockAsync(block, token))
                .AddTo(_disposables);
            
            // 章の終了を監視して、アイワナを起動する
            _chatUseCase.IsChapterEnded
                .Skip(1)
                .Where(isEnded => isEnded && !_hasStartedExternalGame)
                .Subscribe(isEnded =>
                {
                    if (isEnded)
                    {
                        // エンディング会話終了時の再起動を防ぐ
                        _hasStartedExternalGame = true;
                        SaveMoraleToTmpJson();
                        
                        var path = Path.Combine(
                            "I_gonna_be_the_tresure_hunter",
                            "I_wanna_Siv3D.exe"
                        );
                        _processUseCase.StartProcess(path);
                    }
                })
                .AddTo(_disposables);
            
            // 章の会話データをロードする
            LoadMainScenarioAsync().Forget();
        }

        public void Dispose()
        {
            if (_skipButtonHoldNotifier is not null)
            {
                _skipButtonHoldNotifier.Pressed -= StartSkipToInput;
                _skipButtonHoldNotifier.Released -= StopSkipToInput;
            }

            if (_dmButtonView is not null)
            {
                _dmButtonView.DmClicked -= EnterDmMode;
                _dmButtonView.CharacterDmClicked -= StartCharacterDm;
                _dmButtonView.DmBackClicked -= ExitDmMode;
            }

            StopSkipToInput();
            _dmCancellationSource.Cancel();
            _dmCancellationSource.Dispose();
            _disposables.Dispose();
        }

        /// <summary>
        /// 右側のシナリオ表示をDM表示へ切り替える
        /// </summary>
        private void EnterDmMode()
        {
            if (_isDmMode)
                return;

            _isDmMode = true;
            _dmButtonView.Disable();
            StopSkipToInput();
            _chatWindowView.StopVoice();
            _dmCancellationSource.Cancel();
            _dmCancellationSource.Dispose();
            _dmCancellationSource = new CancellationTokenSource();
            _chatWindowView.ClearChatObjects();
        }

        /// <summary>
        /// DM表示を終了して通常チャットを再開する
        /// </summary>
        private void ExitDmMode()
        {
            if (!_isDmMode)
                return;

            _isDmMode = false;
            _dmCancellationSource.Cancel();
            _dmCancellationSource.Dispose();
            _dmCancellationSource = new CancellationTokenSource();
            _chatWindowView.StopVoice();
            _chatWindowView.ClearChatObjects();

            RestoreMainScenarioAsync(_dmCancellationSource.Token).Forget();
        }

        /// <summary>
        /// 選択された人物のDMシナリオを開始する
        /// </summary>
        private void StartCharacterDm(string assetKey)
        {
            if (!_isDmMode)
                return;

            _dmCancellationSource.Cancel();
            _dmCancellationSource.Dispose();
            _dmCancellationSource = new CancellationTokenSource();
            _chatWindowView.ClearChatObjects();
            ShowCharacterDmAsync(assetKey, _dmCancellationSource.Token).Forget();
        }

        /// <summary>
        /// DMシナリオを読み込み、チャットウィンドウへ表示する
        /// </summary>
        private async UniTaskVoid ShowCharacterDmAsync(string assetKey, CancellationToken token)
        {
            var chapter = await _jsonUtilityProvider.ConvertJsonToAnyObjectAsync<Chapter>(assetKey);
            if (chapter?.blocks is null)
                return;

            foreach (var block in chapter.blocks)
                await _chatWindowView.AddNewChatObject(block, token);

            Debug.Log($"[NovelChatPresenter] Started DM scenario: {assetKey}");
        }

        private void BindSkipButton()
        {
            if (_chatWindowView.skipButton is null)
            {
                Debug.LogWarning("[NovelChatPresenter] skipButton is not assigned.");
                return;
            }

            _skipButtonHoldNotifier = _chatWindowView.skipButton.GetComponent<SkipButtonHoldNotifier>();
            if (_skipButtonHoldNotifier is null)
                _skipButtonHoldNotifier = _chatWindowView.skipButton.gameObject.AddComponent<SkipButtonHoldNotifier>();

            _skipButtonHoldNotifier.Pressed += StartSkipToInput;
            _skipButtonHoldNotifier.Released += StopSkipToInput;
        }

        /// <summary>
        /// 選択肢やLLM入力など、次の入力待ちイベント到達までスキップする。
        /// </summary>
        public void StartSkipToInput()
        {
            if (_skipMode == SkipMode.ToNextInputRequired)
                return;

            _skipMode = SkipMode.ToNextInputRequired;
            _chatWindowView.StopVoice();
            Debug.Log("[NovelChatPresenter] Skip-to-input mode enabled.");
        }

        /// <summary>
        /// スキップを停止する。
        /// </summary>
        public void StopSkipToInput()
        {
            _skipMode = SkipMode.None;
        }

        private async UniTask HandleChapterBlockAsync(ChapterBlock block, CancellationToken token)
        {
            if (_isDmMode)
                return;

            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token, _dmCancellationSource.Token);
            try
            {
                await _chatWindowView.AddNewChatObject(
                    block,
                    linkedCancellationSource.Token,
                    choiceIndex =>
                    {
                        // 手動選択が発生した時点でスキップは不要になるため解除する
                        StopSkipToInput();
                        ApplyChoiceMorale(block, choiceIndex);
                        _moraleUseCase.OnSave();
                        _chatUseCase.MoveToNextBlock(choiceIndex);
                    },
                    shouldSkipDelays: IsSkipToInputActive);

                if (_isDmMode)
                    return;

                if (IsSkipToInputActive() && IsUserInputRequiredBlock(block))
                {
                    StopSkipToInput();
                    return;
                }

                if (!ShouldAutoAdvance(block))
                    return;

                if (block.waitingTime > 0)
                    await WaitWithSkipAsync(block.waitingTime, linkedCancellationSource.Token);

                // DM表示中にメインシナリオを進行させない
                linkedCancellationSource.Token.ThrowIfCancellationRequested();
                if (_isDmMode)
                    return;

                _chatUseCase.MoveToNextBlock();
            }
            catch (OperationCanceledException) when (linkedCancellationSource.IsCancellationRequested)
            {
                // DM切替やシーン終了による表示中断は正常終了として扱う
            }
        }

        /// <summary>
        /// メインシナリオと保存済みの会話履歴をロードする
        /// </summary>
        private async UniTaskVoid LoadMainScenarioAsync()
        {
            _isRestoringMainScenario = true;
            try
            {
                await _chatUseCase.LoadChapter("Chapter");
                await RestoreMainScenarioContentsAsync(_dmCancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                // シーン終了やDM切替によるキャンセルは正常終了として扱う
            }
            finally
            {
                _isRestoringMainScenario = false;
            }
        }

        /// <summary>
        /// DMから戻る際に保存済みのメインシナリオを復元する
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        private async UniTaskVoid RestoreMainScenarioAsync(CancellationToken token)
        {
            _isRestoringMainScenario = true;
            try
            {
                await RestoreMainScenarioContentsAsync(token);
            }
            catch (OperationCanceledException)
            {
                // 別のDM選択などによる復元中断は正常終了として扱う
            }
            finally
            {
                _isRestoringMainScenario = false;
            }
        }

        /// <summary>
        /// 到達済みブロックを再描画して現在位置の入力UIを復元する
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        private async UniTask RestoreMainScenarioContentsAsync(CancellationToken token)
        {
            _chatWindowView.ClearChatObjects();
            await _chatWindowView.AddChatHeader(token);
            var reachedBlocks = _chatUseCase.ReachedChapterBlocks;
            if (reachedBlocks.Count == 0)
                return;

            // 過去ブロックは会話だけを即時復元する
            for (var index = 0; index < reachedBlocks.Count - 1; index++)
                await _chatWindowView.AddHistoricalChatObject(reachedBlocks[index], token);

            // 現在ブロックは分岐を含む通常処理で復元する
            _isRestoringMainScenario = false;
            await HandleChapterBlockAsync(reachedBlocks[^1], token);
        }

        private bool IsSkipToInputActive()
        {
            return _skipMode == SkipMode.ToNextInputRequired;
        }

        private async UniTask WaitWithSkipAsync(float waitingTime, CancellationToken token)
        {
            if (IsSkipToInputActive())
                return;

            const float stepSeconds = 0.1f;
            var remaining = waitingTime;
            while (remaining > 0f)
            {
                if (IsSkipToInputActive())
                    return;

                var waitSeconds = Mathf.Min(stepSeconds, remaining);
                await UniTask.WaitForSeconds(waitSeconds, cancellationToken: token);
                remaining -= waitSeconds;
            }
        }

        private static bool ShouldAutoAdvance(ChapterBlock block)
        {
            return !IsUserInputRequiredBlock(block);
        }

        private static bool IsUserInputRequiredBlock(ChapterBlock block)
        {
            var hasChoices = block.choices is { Length: > 0 };
            var hasFreeChats = block.freeChats is { Length: > 0 };
            var isLlmNode = block.nodeType == ChapterNodeType.LLM;

            return hasChoices || hasFreeChats || isLlmNode;
        }

        private void ApplyChoiceMorale(ChapterBlock block, int choiceIndex)
        {
            if (block.choices is null || choiceIndex < 0 || choiceIndex >= block.choices.Length)
                return;

            var choice = block.choices[choiceIndex];
            foreach (var moraleDelta in choice.GetMoraleDeltas())
            {
                if (moraleDelta.Delta == 0)
                    continue;

                _moraleUseCase.AddMorale(moraleDelta.CharacterId, moraleDelta.Delta);
            }

            Debug.Log($"[NovelChatPresenter] Applied morale changes from choice index {choiceIndex}");
        }

        // TODO: TMP
        private void SaveMoraleToTmpJson()
        {
            var moraleDto = _moraleUseCase.GetMoraleDto();
            _tmpMoraleJsonExporter.Export(moraleDto);
            Debug.Log("[NovelChatPresenter] Exported morale JSON at chapter end.");
        }
    }
}
