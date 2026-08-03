namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface IEndingValueProvider
    {
        /// <summary>
        /// エンディング値を読み込む
        /// </summary>
        /// <param name="endingValue">読み込んだエンディング値</param>
        /// <returns>読み込みに成功した場合はtrue</returns>
        bool TryGetEndingValue(out int endingValue);
    }
}
